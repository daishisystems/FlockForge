using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FlockForge.Core.Interfaces;
using FlockForge.Core.Models;
using FlockForge.Core.Configuration;
using CoreUser = FlockForge.Core.Models.User;

namespace FlockForge.Services.Firebase
{
    public class FirebaseAuthenticationService : IAuthenticationService, IDisposable
    {
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
            FirebaseConfig config,
            ISecureStorage secureStorage,
            IPreferences preferences,
            IConnectivity connectivity,
            ILogger<FirebaseAuthenticationService> logger)
        {
            _config = config;
            _secureStorage = secureStorage;
            _preferences = preferences;
            _connectivity = connectivity;
            _logger = logger;
            _disposeCts = new CancellationTokenSource();
            
            // Initialize in background
            Task.Run(async () => await InitializeAsync(), _disposeCts.Token);
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
        
        public async Task<AuthResult> SignInWithEmailPasswordAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            if (_isDisposed) return AuthResult.Failure("Service is disposed");
            
            try
            {
                if (!_connectivity.NetworkAccess.HasFlag(NetworkAccess.Internet))
                {
                    // Allow offline sign-in for existing user
                    if (CurrentUser != null && CurrentUser.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Offline sign-in for existing user: {Email}", email);
                        return AuthResult.Success(CurrentUser);
                    }
                    
                    return AuthResult.Failure("Cannot sign in to new account while offline");
                }
                
                // TODO: Implement actual Firebase authentication
                // For now, create a mock user for testing
                var user = new CoreUser
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = email,
                    DisplayName = email,
                    IsEmailVerified = true,
                    LastLoginAt = DateTime.UtcNow
                };
                
                CurrentUser = user;
                _authStateSubject.OnNext(user);
                await StoreUserWithBackupAsync(user);
                
                _logger.LogInformation("User signed in: {Email}", email);
                return AuthResult.Success(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during sign in");
                return AuthResult.Failure("An unexpected error occurred");
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
                
                // TODO: Implement actual Firebase registration
                // For now, create a mock user for testing
                var user = new CoreUser
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = email,
                    DisplayName = email,
                    IsEmailVerified = false,
                    LastLoginAt = DateTime.UtcNow
                };
                
                CurrentUser = user;
                _authStateSubject.OnNext(user);
                await StoreUserWithBackupAsync(user);
                
                _logger.LogInformation("User registered: {Email}", email);
                return AuthResult.Success(user, requiresEmailVerification: true);
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
                // TODO: Implement actual token refresh with Firebase
                // For now, just return current user if available
                if (CurrentUser != null)
                {
                    return AuthResult.Success(CurrentUser);
                }
                
                return AuthResult.Failure("No authenticated user");
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
                
                // TODO: Implement actual email verification with Firebase
                _logger.LogInformation("Email verification sent (mock)");
                return true;
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
                
                // TODO: Implement actual password reset with Firebase
                _logger.LogInformation("Password reset email sent (mock) to {Email}", email);
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
        
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            
            try
            {
                _disposeCts?.Cancel();
                _disposeCts?.Dispose();
                
                _tokenRefreshTimer?.Dispose();
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