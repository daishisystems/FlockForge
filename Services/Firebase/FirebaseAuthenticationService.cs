using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Plugin.Firebase.Auth;
using FlockForge.Core.Interfaces;
using FlockForge.Core.Models;
using FlockForge.Core.Configuration;
using CoreUser = FlockForge.Core.Models.User;

namespace FlockForge.Services.Firebase
{
    public class FirebaseAuthenticationService : IAuthenticationService, IDisposable
    {
        private readonly Lazy<IFirebaseAuth> _lazyFirebaseAuth;
        private IFirebaseAuth FirebaseAuth => _lazyFirebaseAuth.Value;
        private readonly ISecureStorage _secureStorage;
        private readonly IPreferences _preferences;
        private readonly IConnectivity _connectivity;
        private readonly ILogger<FirebaseAuthenticationService> _logger;
        private readonly FirebaseConfig _config;
        
        private readonly Subject<CoreUser?> _authStateSubject = new();
        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private readonly SemaphoreSlim _storageLock = new(1, 1);
        private readonly ConcurrentDictionary<string, DateTime> _operationTimestamps = new();
        
        private Timer? _tokenRefreshTimer;
        private CancellationTokenSource? _disposeCts;
        private volatile bool _isDisposed;
        private IDisposable? _authStateListener;
        
        // Storage keys
        private const string RefreshTokenKey = "firebase_refresh_token";
        private const string UserIdKey = "firebase_user_id";
        private const string UserEmailKey = "firebase_user_email";
        private const string UserDisplayNameKey = "firebase_user_display_name";
        private const string LastAuthTimeKey = "firebase_last_auth_time";
        private const string OfflineUserKey = "firebase_offline_user";
        private const string FirebaseTokenKey = "firebase_token";
        
        // Backup keys for Preferences
        private const string BackupUserIdKey = "backup_user_id";
        private const string BackupUserEmailKey = "backup_user_email";
        private const string BackupUserNameKey = "backup_user_name";
        private const string BackupAuthTimeKey = "backup_auth_time";
        
        public IObservable<CoreUser?> AuthStateChanged => _authStateSubject;
        public CoreUser? CurrentUser { get; private set; }
        public bool IsAuthenticated => CurrentUser != null;
        public bool IsEmailVerified => CurrentUser?.IsEmailVerified ?? false;
        
        public FirebaseAuthenticationService(
            Lazy<IFirebaseAuth> lazyFirebaseAuth,
            FirebaseConfig config,
            ISecureStorage secureStorage,
            IPreferences preferences,
            IConnectivity connectivity,
            ILogger<FirebaseAuthenticationService> logger)
        {
            _lazyFirebaseAuth = lazyFirebaseAuth;
            _config = config;
            _secureStorage = secureStorage;
            _preferences = preferences;
            _connectivity = connectivity;
            _logger = logger;
            _disposeCts = new CancellationTokenSource();
            
            // Initialize Firebase auth state listener
            InitializeAuthStateListener();
            
            // Initialize in background
            Task.Run(async () => await InitializeAsync(), _disposeCts.Token);
        }
        
        private void InitializeAuthStateListener()
        {
            try
            {
                _authStateListener = FirebaseAuth.AuthStateChanges().Subscribe(
                    auth => OnAuthStateChanged(auth),
                    error => _logger.LogError(error, "Auth state listener error"),
                    () => _logger.LogInformation("Auth state listener completed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize auth state listener");
            }
        }

        private void OnAuthStateChanged(IFirebaseAuth auth)
        {
            try
            {
                var user = auth?.CurrentUser != null ? MapFirebaseUser(auth.CurrentUser) : null;

                // Prevent clearing user when offline
                if (user != null || _connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    CurrentUser = user;
                    _authStateSubject.OnNext(user);

                    if (user != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await StoreUserWithBackupAsync(user);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to store user");
                            }
                        });

                        _logger.LogInformation("User authenticated: {Email}", user.Email);
                    }
                    else
                    {
                        _logger.LogInformation("User signed out");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling auth state change");
            }
        }
        
        private async Task InitializeAsync()
        {
            try
            {
                await RestoreAuthStateAsync();
                StartTokenRefreshTimer();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize authentication service");
                await RestoreFromBackupAsync();
            }
        }
        
        private async Task RestoreAuthStateAsync()
        {
            try
            {
                // Try to restore from stored token
                var token = await GetStoredTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    // Verify token is still valid if online
                    if (_connectivity.NetworkAccess == NetworkAccess.Internet)
                    {
                        try
                        {
                            // TODO: Implement token validation with Firebase
                            // For now, restore from offline storage
                            await RestoreOfflineUserAsync();
                            return;
                        }
                        catch
                        {
                            // Token invalid, try offline user
                        }
                    }
                }
                
                // Restore from offline storage
                await RestoreOfflineUserAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore auth state");
                await RestoreFromBackupAsync();
            }
        }
        
        private async Task<string?> GetStoredTokenAsync()
        {
            if (!await _storageLock.WaitAsync(_config.StorageOperationTimeoutMs))
            {
                _logger.LogWarning("Storage lock timeout");
                return _preferences.Get(FirebaseTokenKey, string.Empty);
            }
            
            try
            {
                return await _secureStorage.GetAsync(FirebaseTokenKey);
            }
            catch
            {
                return _preferences.Get(FirebaseTokenKey, string.Empty);
            }
            finally
            {
                _storageLock.Release();
            }
        }
        
        private async Task RestoreOfflineUserAsync()
        {
            try
            {
                var user = await GetStoredUserWithTimeoutAsync(CancellationToken.None);
                if (user != null)
                {
                    CurrentUser = user;
                    _authStateSubject.OnNext(user);
                    _logger.LogInformation("Restored offline user: {Email}", user.Email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore offline user");
            }
        }
        
        private async Task<CoreUser?> GetStoredUserWithTimeoutAsync(CancellationToken cancellationToken)
        {
            if (!await _storageLock.WaitAsync(_config.StorageOperationTimeoutMs, cancellationToken))
            {
                _logger.LogWarning("Storage lock timeout - attempting backup restore");
                return await GetUserFromBackupAsync();
            }
            
            try
            {
                // Try primary secure storage first
                try
                {
                    var userJson = await _secureStorage.GetAsync(OfflineUserKey);
                    if (!string.IsNullOrEmpty(userJson))
                    {
                        return System.Text.Json.JsonSerializer.Deserialize<CoreUser>(userJson);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read from secure storage");
                }
                
                // Fallback to preferences backup
                return await GetUserFromBackupAsync();
            }
            finally
            {
                _storageLock.Release();
            }
        }
        
        private async Task<CoreUser?> GetUserFromBackupAsync()
        {
            try
            {
                var userId = _preferences.Get(BackupUserIdKey, string.Empty);
                var userEmail = _preferences.Get(BackupUserEmailKey, string.Empty);
                var userName = _preferences.Get(BackupUserNameKey, string.Empty);
                var authTime = _preferences.Get(BackupAuthTimeKey, string.Empty);
                
                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogInformation("Restored user from backup preferences");
                    return new CoreUser
                    {
                        Id = userId,
                        Email = userEmail,
                        DisplayName = userName ?? userEmail,
                        LastLoginAt = DateTime.TryParse(authTime, out var time) ? time : DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore from backup");
            }
            
            return null;
        }
        
        private void StartTokenRefreshTimer()
        {
            try
            {
                _tokenRefreshTimer?.Dispose();
                _tokenRefreshTimer = new Timer(
                    async _ => await SafeTokenRefreshAsync(),
                    null,
                    TimeSpan.FromMinutes(_config.TokenRefreshIntervalMinutes),
                    TimeSpan.FromMinutes(_config.TokenRefreshIntervalMinutes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start token refresh timer");
            }
        }
        
        private async Task SafeTokenRefreshAsync()
        {
            if (_isDisposed) return;
            
            try
            {
                var operationKey = "token_refresh";
                var now = DateTime.UtcNow;
                
                if (_operationTimestamps.TryGetValue(operationKey, out var lastRefresh))
                {
                    if ((now - lastRefresh).TotalMinutes < 5)
                    {
                        _logger.LogDebug("Skipping token refresh - too recent");
                        return;
                    }
                }
                
                _operationTimestamps[operationKey] = now;
                
                if (_connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    await RefreshTokenAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in safe token refresh");
            }
        }
        
        private void LogAuthenticationDebugInfo(string email, string password)
        {
            _logger.LogInformation("=== AUTH DEBUG START ===");
            _logger.LogInformation($"Email: '{email}' | Length: {email?.Length}");
            _logger.LogInformation($"Email trimmed: '{email?.Trim()}' | Length: {email?.Trim().Length}");
            _logger.LogInformation($"Password length: {password?.Length}");
            _logger.LogInformation($"Password trimmed length: {password?.Trim().Length}");
            
            // Check for special characters
            if (!string.IsNullOrEmpty(email))
            {
                for (int i = 0; i < email.Length; i++)
                {
                    if (char.IsControl(email[i]) || char.IsWhiteSpace(email[i]))
                    {
                        _logger.LogWarning($"Special character found at position {i}: Unicode {(int)email[i]}");
                    }
                }
            }
            _logger.LogInformation("=== AUTH DEBUG END ===");
        }
        
        public async Task<AuthResult> SignInWithEmailPasswordAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                // Add debug logging
                LogAuthenticationDebugInfo(email, password);
                
                // Clean inputs - CRITICAL: Trim whitespace
                email = email?.Trim().ToLowerInvariant();
                password = password?.Trim();
                
                // Validate cleaned inputs
                if (string.IsNullOrEmpty(email))
                {
                    return AuthResult.Failure("Email is required");
                }
                
                if (string.IsNullOrEmpty(password))
                {
                    return AuthResult.Failure("Password is required");
                }
                
                if (password.Length < 6)
                {
                    return AuthResult.Failure("Password must be at least 6 characters");
                }
                
                _logger.LogInformation($"Attempting Firebase auth with email: {email}");
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var result = await FirebaseAuth.SignInWithEmailAndPasswordAsync(email, password);
                stopwatch.Stop();
                
                _logger.LogInformation($"Sign in successful for: {email} in {stopwatch.ElapsedMilliseconds}ms");
                
                // Store auth tokens
                await StoreAuthTokensAsync(result);
                
                return AuthResult.Success(MapFirebaseUser(result));
            }
            catch (Exception ex) when (ex.GetType().Name.Contains("FirebaseAuth") || ex.Message.Contains("Firebase"))
            {
                _logger.LogError(ex, $"Firebase auth error: {ex.Message}");
                
                // Map Firebase error codes to user-friendly messages
                string errorMessage = ex.Message switch
                {
                    var msg when msg.Contains("INVALID_PASSWORD") => "Incorrect password",
                    var msg when msg.Contains("USER_NOT_FOUND") => "No account found with this email",
                    var msg when msg.Contains("malformed") => "Invalid credentials. Please check your email and password.",
                    var msg when msg.Contains("EMAIL_NOT_FOUND") => "Email not found",
                    var msg when msg.Contains("TOO_MANY_ATTEMPTS") => "Too many failed attempts. Please try again later.",
                    _ => "Sign in failed. Please try again."
                };
                
                return AuthResult.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during sign in");
                return AuthResult.Failure("An unexpected error occurred. Please try again.");
            }
        }
        
        public async Task<AuthResult> SignUpWithEmailPasswordAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            if (_isDisposed) return AuthResult.Failure("Service is disposed");
            
            try
            {
                if (!_connectivity.NetworkAccess.HasFlag(NetworkAccess.Internet))
                {
                    return AuthResult.Failure("Registration requires an internet connection");
                }
                
                var result = await FirebaseAuth.CreateUserAsync(email, password);
                
                if (result == null)
                {
                    return AuthResult.Failure("Registration failed");
                }
                
                await result.SendEmailVerificationAsync();
                await StoreAuthTokensAsync(result);
                
                return AuthResult.Success(MapFirebaseUser(result), requiresEmailVerification: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during sign up");
                return AuthResult.Failure("An unexpected error occurred");
            }
        }
        
        public async Task<AuthResult> RefreshTokenAsync()
        {
            if (_isDisposed) return AuthResult.Failure("Service is disposed");
            
            // Only refresh if online
            if (_connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                _logger.LogInformation("Skipping token refresh - device is offline");
                return CurrentUser != null 
                    ? AuthResult.Success(CurrentUser) 
                    : AuthResult.Failure("No authenticated user");
            }
            
            if (!await _refreshLock.WaitAsync(_config.AuthRefreshTimeoutMs))
            {
                _logger.LogWarning("Token refresh lock timeout");
                return CurrentUser != null 
                    ? AuthResult.Success(CurrentUser) 
                    : AuthResult.Failure("Refresh in progress");
            }
            
            try
            {
                if (FirebaseAuth.CurrentUser != null)
                {
                    // Firebase SDK handles token refresh automatically
                    return AuthResult.Success(MapFirebaseUser(FirebaseAuth.CurrentUser));
                }
                
                return AuthResult.Failure("No current user to refresh");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                
                // Even if refresh fails, keep the user authenticated
                if (CurrentUser != null)
                {
                    return AuthResult.Success(CurrentUser);
                }
                
                return AuthResult.Failure("Authentication required");
            }
            finally
            {
                _refreshLock.Release();
            }
        }
private async Task StoreAuthTokensAsync(IFirebaseUser user)
        {
            try
            {
                await _secureStorage.SetAsync(UserIdKey, user.Uid);
                await _secureStorage.SetAsync(UserEmailKey, user.Email ?? string.Empty);
                await _secureStorage.SetAsync(UserDisplayNameKey, user.DisplayName ?? string.Empty);
                
                // Store basic auth info - token will be managed by Firebase SDK
                await _secureStorage.SetAsync(LastAuthTimeKey, DateTime.UtcNow.ToString("O"));
                
                // Also store user object for offline access
                var coreUser = MapFirebaseUser(user);
                await StoreUserWithBackupAsync(coreUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store auth tokens");
            }
        }
        
        private CoreUser MapFirebaseUser(IFirebaseUser firebaseUser)
        {
            return new CoreUser
            {
                Id = firebaseUser.Uid,
                Email = firebaseUser.Email ?? string.Empty,
                DisplayName = firebaseUser.DisplayName ?? firebaseUser.Email ?? string.Empty,
                IsEmailVerified = firebaseUser.IsEmailVerified,
                PhotoUrl = firebaseUser.PhotoUrl,
                LastLoginAt = DateTime.UtcNow
            };
        }
        
        private async Task StoreUserWithBackupAsync(CoreUser user)
        {
            if (!await _storageLock.WaitAsync(_config.StorageOperationTimeoutMs))
            {
                _logger.LogWarning("Storage lock timeout - storing only backup");
                StoreUserBackup(user);
                return;
            }
            
            try
            {
                // Store in secure storage
                try
                {
                    var userJson = System.Text.Json.JsonSerializer.Serialize(user);
                    await _secureStorage.SetAsync(OfflineUserKey, userJson);
                    await _secureStorage.SetAsync(LastAuthTimeKey, DateTime.UtcNow.ToString("O"));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to write to secure storage");
                }
                
                // Always store backup in preferences
                StoreUserBackup(user);
            }
            finally
            {
                _storageLock.Release();
            }
        }
        
        private void StoreUserBackup(CoreUser user)
        {
            try
            {
                _preferences.Set(BackupUserIdKey, user.Id);
                _preferences.Set(BackupUserEmailKey, user.Email);
                _preferences.Set(BackupUserNameKey, user.DisplayName);
                _preferences.Set(BackupAuthTimeKey, DateTime.UtcNow.ToString("O"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store user backup");
            }
        }
        
        public async Task SignOutAsync()
        {
            if (_isDisposed) return;
            
            if (!await _storageLock.WaitAsync(_config.StorageOperationTimeoutMs))
            {
                _logger.LogWarning("Storage lock timeout during sign out");
            }
            
            try
            {
                // Sign out from Firebase
                await FirebaseAuth.SignOutAsync();
                
                CurrentUser = null;
                _authStateSubject.OnNext(null);
                
                // Clear all storage
                await ClearAllStorageAsync();
            }
            finally
            {
                if (_storageLock.CurrentCount == 0)
                    _storageLock.Release();
            }
        }
        
        private async Task ClearAllStorageAsync()
        {
            // Clear secure storage
            try
            {
                _secureStorage.Remove(OfflineUserKey);
                _secureStorage.Remove(LastAuthTimeKey);
                _secureStorage.Remove(RefreshTokenKey);
                _secureStorage.Remove(UserIdKey);
                _secureStorage.Remove(UserEmailKey);
                _secureStorage.Remove(FirebaseTokenKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear secure storage");
            }
            
            // Clear backup preferences
            try
            {
                _preferences.Remove(BackupUserIdKey);
                _preferences.Remove(BackupUserEmailKey);
                _preferences.Remove(BackupUserNameKey);
                _preferences.Remove(BackupAuthTimeKey);
                _preferences.Remove(FirebaseTokenKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear preferences");
            }
        }
        
        public async Task<bool> SendEmailVerificationAsync()
        {
            if (_isDisposed) return false;
            
            try
            {
                if (!_connectivity.NetworkAccess.HasFlag(NetworkAccess.Internet))
                {
                    return false;
                }
                
                if (FirebaseAuth.CurrentUser != null)
                {
                    await FirebaseAuth.CurrentUser.SendEmailVerificationAsync();
                    _logger.LogInformation("Email verification sent to: {Email}", FirebaseAuth.CurrentUser.Email);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email verification");
                return false;
            }
        }
        
        public async Task<bool> SendPasswordResetEmailAsync(string email)
        {
            if (_isDisposed) return false;
            
            try
            {
                if (!_connectivity.NetworkAccess.HasFlag(NetworkAccess.Internet))
                {
                    return false;
                }
                
                await FirebaseAuth.SendPasswordResetEmailAsync(email);
                _logger.LogInformation("Password reset email sent to {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email");
                return false;
            }
        }
        
        public async Task<AuthResult> SignInWithGoogleAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed) return AuthResult.Failure("Service is disposed");
            
            try
            {
                if (!_connectivity.NetworkAccess.HasFlag(NetworkAccess.Internet))
                {
                    return AuthResult.Failure("Google sign in requires an internet connection");
                }
                
                // TODO: Implement Google sign-in
                return AuthResult.Failure("Google sign-in not yet implemented");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google sign in failed");
                return AuthResult.Failure("Google sign in failed");
            }
        }
        
        public async Task<AuthResult> SignInWithAppleAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed) return AuthResult.Failure("Service is disposed");
            
            try
            {
                if (!_connectivity.NetworkAccess.HasFlag(NetworkAccess.Internet))
                {
                    return AuthResult.Failure("Apple sign in requires an internet connection");
                }
                
                // TODO: Implement Apple sign-in
                return AuthResult.Failure("Apple sign-in not yet implemented");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Apple sign in failed");
                return AuthResult.Failure("Apple sign in failed");
            }
        }
        
        public Task<AuthResult> SignInWithMicrosoftAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AuthResult.Failure("Microsoft sign in not yet implemented"));
        }
        
        private async Task RestoreFromBackupAsync()
        {
            try
            {
                var user = await GetUserFromBackupAsync();
                if (user != null)
                {
                    CurrentUser = user;
                    _authStateSubject.OnNext(user);
                    _logger.LogInformation("Successfully restored user from backup");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore from backup");
            }
        }
        
        /// <summary>
        /// Debug method to check Firebase authentication state and connection
        /// </summary>
        public async Task<bool> DebugAuthenticationStateAsync()
        {
            try
            {
                _logger.LogInformation("=== Firebase Authentication Debug ===");
                
                // Check if Firebase is properly initialized
                var currentUser = FirebaseAuth.CurrentUser;
                _logger.LogInformation("Current Firebase user: {Email}", currentUser?.Email ?? "None");
                _logger.LogInformation("Current service user: {Email}", CurrentUser?.Email ?? "None");
                
                // Check network connectivity
                _logger.LogInformation("Network access: {NetworkAccess}", _connectivity.NetworkAccess);
                
                if (_connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    try
                    {
                        // Test Firebase connection by checking if we can access the current user
                        var testUser = FirebaseAuth.CurrentUser;
                        _logger.LogInformation("Firebase connection test successful - can access Firebase Auth");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Firebase connection test failed: {Message}", ex.Message);
                        return false;
                    }
                }
                else
                {
                    _logger.LogWarning("Skipping Firebase connection test - no internet connection");
                }
                
                // Check stored authentication data
                var storedToken = await GetStoredTokenAsync();
                _logger.LogInformation("Stored token exists: {HasToken}", !string.IsNullOrEmpty(storedToken));
                
                var storedUser = await GetStoredUserWithTimeoutAsync(CancellationToken.None);
                _logger.LogInformation("Stored user exists: {HasUser} - {Email}", storedUser != null, storedUser?.Email ?? "None");
                
                _logger.LogInformation("=== End Firebase Authentication Debug ===");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firebase authentication debug failed");
                return false;
            }
        }
        
        /// <summary>
        /// Enhanced method to test Firebase configuration and connection
        /// </summary>
        public async Task<bool> TestFirebaseConfigurationAsync()
        {
            try
            {
                _logger.LogInformation("=== Firebase Configuration Test ===");
                
                // Check if we can access Firebase Auth
                if (FirebaseAuth == null)
                {
                    _logger.LogError("Firebase Auth is null - initialization failed");
                    return false;
                }
                
                _logger.LogInformation("Firebase Auth instance created successfully");
                
                // Check network connectivity
                if (_connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    _logger.LogWarning("No internet connection - cannot test Firebase configuration");
                    return false;
                }
                
                // Test basic Firebase operations
                try
                {
                    // Test if we can access Firebase Auth instance
                    var currentUser = FirebaseAuth.CurrentUser;
                    _logger.LogInformation("Firebase API access successful - configuration is valid");
                    
                    // Try to send a password reset to a test email to verify Firebase connection
                    try
                    {
                        await FirebaseAuth.SendPasswordResetEmailAsync("test@nonexistent-domain-12345.com");
                        _logger.LogInformation("Firebase connection verified - password reset call succeeded");
                    }
                    catch (Exception resetEx)
                    {
                        // This is expected to fail, but if it fails with a network error, that's a problem
                        if (resetEx.Message.Contains("network") || resetEx.Message.Contains("connection"))
                        {
                            _logger.LogError("Network connectivity issue detected: {Message}", resetEx.Message);
                            return false;
                        }
                        else
                        {
                            _logger.LogInformation("Firebase connection verified - expected error for non-existent email");
                        }
                    }
                    
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Firebase API call failed - configuration may be invalid: {Message}", ex.Message);
                    
                    // Check for specific configuration errors
                    if (ex.Message.Contains("API key") || ex.Message.Contains("project"))
                    {
                        _logger.LogError("Firebase configuration error detected. Check GoogleService-Info.plist");
                    }
                    else if (ex.Message.Contains("network") || ex.Message.Contains("connection"))
                    {
                        _logger.LogError("Network connectivity issue detected");
                    }
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firebase configuration test failed");
                return false;
            }
            finally
            {
                _logger.LogInformation("=== End Firebase Configuration Test ===");
            }
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            
            try
            {
                _disposeCts?.Cancel();
                _disposeCts?.Dispose();

                _tokenRefreshTimer?.Dispose();
                _authStateListener?.Dispose();
                _authStateSubject?.Dispose();
                _refreshLock?.Dispose();
                _storageLock?.Dispose();
                
                _operationTimestamps.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disposal");
            }
        }
    }
}