# AI Agent Implementation Guide: Complete Firebase Integration with Persistent Offline Authentication

## Overview

This guide provides comprehensive, step-by-step instructions for implementing a Firebase integration in FlockForge that includes email/password authentication, SSO support, and Firestore with persistent offline capabilities. The implementation ensures users remain authenticated indefinitely while offline.

## Critical Requirements

1. **Authentication**: Email/password and SSO (Google, Apple, Microsoft)
2. **Offline Persistence**: Full Firestore offline support without SQLite
3. **Persistent Auth**: Users must NEVER be logged out while offline
4. **Field-First Design**: Complete functionality without network connectivity

## Implementation Steps

### Step 1: Create Project Structure

Create the following directory structure in the FlockForge project:

```
FlockForge/
├── Core/
│   ├── Interfaces/
│   │   ├── IAuthenticationService.cs
│   │   ├── IDataService.cs
│   │   ├── IFirebaseInitializer.cs
│   │   └── INavigationService.cs
│   └── Models/
│       ├── AuthResult.cs
│       ├── BaseEntity.cs
│       ├── User.cs
│       ├── Farm.cs
│       ├── Farmer.cs
│       ├── LambingSeason.cs
│       ├── Breeding.cs
│       ├── Scanning.cs
│       ├── Lambing.cs
│       └── Weaning.cs
├── Services/
│   ├── Firebase/
│   │   ├── FirebaseAuthenticationService.cs
│   │   └── FirestoreService.cs
│   └── Navigation/
│       └── NavigationService.cs
├── ViewModels/
│   ├── BaseViewModel.cs
│   ├── LoginViewModel.cs
│   ├── RegisterViewModel.cs
│   ├── FarmListViewModel.cs
│   └── FarmDetailViewModel.cs
├── Views/
│   ├── LoginPage.xaml
│   ├── RegisterPage.xaml
│   ├── FarmListPage.xaml
│   └── FarmDetailPage.xaml
└── Platforms/
    ├── Android/
    │   └── Services/
    │       └── AndroidFirebaseInitializer.cs
    └── iOS/
        └── Services/
            └── iOSFirebaseInitializer.cs
```

### Step 2: Install NuGet Packages

Add these exact packages to `FlockForge.csproj`:

```xml
<ItemGroup>
  <!-- Firebase packages -->
  <PackageReference Include="Plugin.Firebase.Auth" Version="2.0.0" />
  <PackageReference Include="Plugin.Firebase.Firestore" Version="2.0.0" />
  <PackageReference Include="Plugin.Firebase.Core" Version="2.0.0" />
  
  <!-- Supporting packages -->
  <PackageReference Include="System.Reactive" Version="6.0.0" />
  <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
  <PackageReference Include="Polly" Version="8.2.0" />
  <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
</ItemGroup>
```

### Step 3: Core Models Implementation

#### File: `Core/Models/BaseEntity.cs`

```csharp
using System;
using Plugin.Firebase.Firestore;

namespace FlockForge.Core.Models
{
    public abstract class BaseEntity
    {
        [FirestoreProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [FirestoreProperty("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [FirestoreProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        [FirestoreProperty("isDeleted")]
        public bool IsDeleted { get; set; } = false;
        
        [FirestoreProperty("userId")]
        public string? UserId { get; set; }
    }
}
```

#### File: `Core/Models/User.cs`

```csharp
using System;

namespace FlockForge.Core.Models
{
    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; }
        public string? PhotoUrl { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
```

#### File: `Core/Models/AuthResult.cs`

```csharp
namespace FlockForge.Core.Models
{
    public class AuthResult
    {
        public bool IsSuccess { get; set; }
        public User? User { get; set; }
        public string? ErrorMessage { get; set; }
        public bool RequiresEmailVerification { get; set; }
        
        public static AuthResult Success(User user, bool requiresEmailVerification = false)
        {
            return new AuthResult 
            { 
                IsSuccess = true, 
                User = user,
                RequiresEmailVerification = requiresEmailVerification
            };
        }
        
        public static AuthResult Failure(string errorMessage)
        {
            return new AuthResult 
            { 
                IsSuccess = false, 
                ErrorMessage = errorMessage 
            };
        }
    }
}
```

#### File: `Core/Models/Farm.cs`

```csharp
using Plugin.Firebase.Firestore;

namespace FlockForge.Core.Models
{
    [FirestoreObject]
    public class Farm : BaseEntity
    {
        [FirestoreProperty("farmerId")]
        public string FarmerId { get; set; } = string.Empty;
        
        [FirestoreProperty("farmName")]
        public string FarmName { get; set; } = string.Empty;
        
        [FirestoreProperty("companyName")]
        public string? CompanyName { get; set; }
        
        [FirestoreProperty("breed")]
        public string Breed { get; set; } = string.Empty;
        
        [FirestoreProperty("totalProductionEwes")]
        public int TotalProductionEwes { get; set; }
        
        [FirestoreProperty("size")]
        public decimal Size { get; set; }
        
        [FirestoreProperty("sizeUnit")]
        public string SizeUnit { get; set; } = "hectares";
        
        [FirestoreProperty("address")]
        public string? Address { get; set; }
        
        [FirestoreProperty("city")]
        public string? City { get; set; }
        
        [FirestoreProperty("province")]
        public string? Province { get; set; }
        
        [FirestoreProperty("gpsLocation")]
        public string? GPSLocation { get; set; }
        
        [FirestoreProperty("productionSystem")]
        public string? ProductionSystem { get; set; }
    }
}
```

### Step 4: Core Interfaces

#### File: `Core/Interfaces/IAuthenticationService.cs`

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using FlockForge.Core.Models;

namespace FlockForge.Core.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AuthResult> SignInWithEmailPasswordAsync(string email, string password, CancellationToken cancellationToken = default);
        Task<AuthResult> SignUpWithEmailPasswordAsync(string email, string password, CancellationToken cancellationToken = default);
        Task<AuthResult> SignInWithGoogleAsync(CancellationToken cancellationToken = default);
        Task<AuthResult> SignInWithAppleAsync(CancellationToken cancellationToken = default);
        Task<AuthResult> SignInWithMicrosoftAsync(CancellationToken cancellationToken = default);
        Task SignOutAsync();
        Task<bool> SendEmailVerificationAsync();
        Task<bool> SendPasswordResetEmailAsync(string email);
        Task<AuthResult> RefreshTokenAsync();
        IObservable<User?> AuthStateChanged { get; }
        User? CurrentUser { get; }
        bool IsAuthenticated { get; }
        bool IsEmailVerified { get; }
    }
}
```

#### File: `Core/Interfaces/IDataService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FlockForge.Core.Models;

namespace FlockForge.Core.Interfaces
{
    public interface IDataService
    {
        Task<T?> GetAsync<T>(string documentId) where T : BaseEntity;
        Task<IReadOnlyList<T>> GetAllAsync<T>() where T : BaseEntity;
        Task<IReadOnlyList<T>> QueryAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity;
        Task<bool> SaveAsync<T>(T entity) where T : BaseEntity;
        Task<bool> DeleteAsync<T>(string documentId) where T : BaseEntity;
        Task<bool> BatchSaveAsync<T>(IEnumerable<T> entities) where T : BaseEntity;
        IObservable<T> DocumentChanged<T>(string documentId) where T : BaseEntity;
        IObservable<IReadOnlyList<T>> CollectionChanged<T>() where T : BaseEntity;
    }
}
```

### Step 5: Firebase Authentication Service with Persistent Offline Support

#### File: `Services/Firebase/FirebaseAuthenticationService.cs`

```csharp
using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Plugin.Firebase.Auth;
using FlockForge.Core.Interfaces;
using FlockForge.Core.Models;

namespace FlockForge.Services.Firebase
{
    public class FirebaseAuthenticationService : IAuthenticationService, IDisposable
    {
        private readonly IFirebaseAuth _firebaseAuth;
        private readonly ISecureStorage _secureStorage;
        private readonly IConnectivity _connectivity;
        private readonly ILogger<FirebaseAuthenticationService> _logger;
        private readonly Subject<User?> _authStateSubject = new();
        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private IDisposable? _authStateListener;
        private Timer? _tokenRefreshTimer;
        
        private const string RefreshTokenKey = "firebase_refresh_token";
        private const string UserIdKey = "firebase_user_id";
        private const string UserEmailKey = "firebase_user_email";
        private const string UserDisplayNameKey = "firebase_user_display_name";
        private const string LastAuthTimeKey = "firebase_last_auth_time";
        private const string OfflineUserKey = "firebase_offline_user";
        
        public IObservable<User?> AuthStateChanged => _authStateSubject;
        public User? CurrentUser { get; private set; }
        public bool IsAuthenticated => CurrentUser != null;
        public bool IsEmailVerified => CurrentUser?.IsEmailVerified ?? false;
        
        public FirebaseAuthenticationService(
            IFirebaseAuth firebaseAuth,
            ISecureStorage secureStorage,
            IConnectivity connectivity,
            ILogger<FirebaseAuthenticationService> logger)
        {
            _firebaseAuth = firebaseAuth;
            _secureStorage = secureStorage;
            _connectivity = connectivity;
            _logger = logger;
            
            InitializeAuthStateListener();
            InitializeOfflineAuth();
            StartTokenRefreshTimer();
        }
        
        private void InitializeAuthStateListener()
        {
            _authStateListener = _firebaseAuth.AuthStateChanged.Subscribe(auth =>
            {
                var user = auth?.CurrentUser != null ? MapFirebaseUser(auth.CurrentUser) : null;
                
                // Only update current user if we have a new valid user OR if we're online
                // This prevents Firebase from clearing our user when offline
                if (user != null || _connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    CurrentUser = user;
                    _authStateSubject.OnNext(user);
                    
                    if (user != null)
                    {
                        Task.Run(async () => await StoreOfflineUserAsync(user));
                    }
                }
            });
        }
        
        private async void InitializeOfflineAuth()
        {
            try
            {
                // First, try to restore from Firebase's own auth state
                if (_firebaseAuth.CurrentUser != null)
                {
                    CurrentUser = MapFirebaseUser(_firebaseAuth.CurrentUser);
                    _authStateSubject.OnNext(CurrentUser);
                    return;
                }
                
                // If Firebase doesn't have a user, restore from secure storage
                await RestoreOfflineUserAsync();
                
                // Only attempt token refresh if we're online
                if (_connectivity.NetworkAccess == NetworkAccess.Internet && CurrentUser != null)
                {
                    await AttemptTokenRefreshAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize offline auth");
            }
        }
        
        private async Task RestoreOfflineUserAsync()
        {
            try
            {
                var offlineUserJson = await _secureStorage.GetAsync(OfflineUserKey);
                if (!string.IsNullOrEmpty(offlineUserJson))
                {
                    var user = System.Text.Json.JsonSerializer.Deserialize<User>(offlineUserJson);
                    if (user != null)
                    {
                        CurrentUser = user;
                        _authStateSubject.OnNext(user);
                        _logger.LogInformation("Restored offline user: {Email}", user.Email);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore offline user");
            }
        }
        
        private async Task StoreOfflineUserAsync(User user)
        {
            try
            {
                var userJson = System.Text.Json.JsonSerializer.Serialize(user);
                await _secureStorage.SetAsync(OfflineUserKey, userJson);
                await _secureStorage.SetAsync(LastAuthTimeKey, DateTime.UtcNow.ToString("O"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store offline user");
            }
        }
        
        private void StartTokenRefreshTimer()
        {
            // Check every 30 minutes if we need to refresh the token
            _tokenRefreshTimer = new Timer(
                async _ => await OnTokenRefreshTimerElapsed(),
                null,
                TimeSpan.FromMinutes(30),
                TimeSpan.FromMinutes(30));
        }
        
        private async Task OnTokenRefreshTimerElapsed()
        {
            // Only refresh if online
            if (_connectivity.NetworkAccess != NetworkAccess.Internet || CurrentUser == null)
            {
                return;
            }
            
            await AttemptTokenRefreshAsync();
        }
        
        private async Task AttemptTokenRefreshAsync()
        {
            await _refreshLock.WaitAsync();
            try
            {
                if (_firebaseAuth.CurrentUser != null)
                {
                    // Force token refresh
                    var token = await _firebaseAuth.CurrentUser.GetIdTokenAsync(true);
                    _logger.LogInformation("Token refreshed successfully");
                    
                    // Update stored user
                    var refreshedUser = MapFirebaseUser(_firebaseAuth.CurrentUser);
                    CurrentUser = refreshedUser;
                    await StoreOfflineUserAsync(refreshedUser);
                }
                else
                {
                    _logger.LogWarning("Cannot refresh token - no Firebase user");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed - user remains authenticated offline");
                // Don't clear the user - they stay authenticated offline
            }
            finally
            {
                _refreshLock.Release();
            }
        }
        
        public async Task<AuthResult> SignInWithEmailPasswordAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_connectivity.NetworkAccess.HasFlag(NetworkAccess.Internet))
                {
                    // Check if this is the same user trying to sign in
                    if (CurrentUser != null && CurrentUser.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Offline sign-in for existing user: {Email}", email);
                        return AuthResult.Success(CurrentUser);
                    }
                    
                    return AuthResult.Failure("Cannot sign in to new account while offline");
                }
                
                var result = await _firebaseAuth.SignInWithEmailAndPasswordAsync(email, password);
                
                if (result?.User == null)
                {
                    return AuthResult.Failure("Sign in failed");
                }
                
                var user = MapFirebaseUser(result.User);
                await StoreOfflineUserAsync(user);
                await StoreAuthTokensAsync(result.User);
                
                if (!result.User.IsEmailVerified)
                {
                    return AuthResult.Success(user, requiresEmailVerification: true);
                }
                
                return AuthResult.Success(user);
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogError(ex, "Firebase auth error during sign in");
                return AuthResult.Failure(GetUserFriendlyErrorMessage(ex));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during sign in");
                return AuthResult.Failure("An unexpected error occurred");
            }
        }
        
        public async Task<AuthResult> SignUpWithEmailPasswordAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_connectivity.NetworkAccess.HasFlag(NetworkAccess.Internet))
                {
                    return AuthResult.Failure("Registration requires an internet connection");
                }
                
                var result = await _firebaseAuth.CreateUserWithEmailAndPasswordAsync(email, password);
                
                if (result?.User == null)
                {
                    return AuthResult.Failure("Registration failed");
                }
                
                await result.User.SendEmailVerificationAsync();
                
                var user = MapFirebaseUser(result.User);
                await StoreOfflineUserAsync(user);
                await StoreAuthTokensAsync(result.User);
                
                return AuthResult.Success(user, requiresEmailVerification: true);
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogError(ex, "Firebase auth error during sign up");
                return AuthResult.Failure(GetUserFriendlyErrorMessage(ex));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during sign up");
                return AuthResult.Failure("An unexpected error occurred");
            }
        }
        
        public async Task<AuthResult> SignInWithGoogleAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_connectivity.NetworkAccess.HasFlag(NetworkAccess.Internet))
                {
                    return AuthResult.Failure("Google sign in requires an internet connection");
                }
                
                var result = await _firebaseAuth.SignInWithGoogleAsync();
                
                if (result?.User == null)
                {
                    return AuthResult.Failure("Google sign in failed");
                }
                
                var user = MapFirebaseUser(result.User);
                await StoreOfflineUserAsync(user);
                await StoreAuthTokensAsync(result.User);
                
                return AuthResult.Success(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google sign in failed");
                return AuthResult.Failure("Google sign in failed");
            }
        }
        
        public async Task<AuthResult> SignInWithAppleAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_connectivity.NetworkAccess.HasFlag(NetworkAccess.Internet))
                {
                    return AuthResult.Failure("Apple sign in requires an internet connection");
                }
                
                var result = await _firebaseAuth.SignInWithAppleAsync();
                
                if (result?.User == null)
                {
                    return AuthResult.Failure("Apple sign in failed");
                }
                
                var user = MapFirebaseUser(result.User);
                await StoreOfflineUserAsync(user);
                await StoreAuthTokensAsync(result.User);
                
                return AuthResult.Success(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Apple sign in failed");
                return AuthResult.Failure("Apple sign in failed");
            }
        }
        
        public async Task<AuthResult> SignInWithMicrosoftAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Microsoft SSO requires custom implementation with MSAL
                // This is a placeholder - implement based on your Microsoft app registration
                throw new NotImplementedException("Microsoft SSO requires MSAL integration");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Microsoft sign in failed");
                return AuthResult.Failure("Microsoft sign in is not yet available");
            }
        }
        
        public async Task<AuthResult> RefreshTokenAsync()
        {
            // Only refresh if online
            if (_connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                _logger.LogInformation("Skipping token refresh - device is offline");
                return CurrentUser != null 
                    ? AuthResult.Success(CurrentUser) 
                    : AuthResult.Failure("No authenticated user");
            }
            
            await _refreshLock.WaitAsync();
            try
            {
                if (_firebaseAuth.CurrentUser != null)
                {
                    var token = await _firebaseAuth.CurrentUser.GetIdTokenAsync(true);
                    var user = MapFirebaseUser(_firebaseAuth.CurrentUser);
                    await StoreOfflineUserAsync(user);
                    return AuthResult.Success(user);
                }
                
                // If no Firebase user but we have offline user, return that
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
        
        public async Task SignOutAsync()
        {
            try
            {
                // Only sign out from Firebase if online
                if (_connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    await _firebaseAuth.SignOutAsync();
                }
                
                // Always clear local storage
                await ClearStoredTokensAsync();
                await _secureStorage.Remove(OfflineUserKey);
                await _secureStorage.Remove(LastAuthTimeKey);
                
                CurrentUser = null;
                _authStateSubject.OnNext(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sign out failed");
                throw;
            }
        }
        
        public async Task<bool> SendEmailVerificationAsync()
        {
            try
            {
                if (!_connectivity.NetworkAccess.HasFlag(NetworkAccess.Internet))
                {
                    return false;
                }
                
                if (_firebaseAuth.CurrentUser == null) return false;
                
                await _firebaseAuth.CurrentUser.SendEmailVerificationAsync();
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
            try
            {
                if (!_connectivity.NetworkAccess.HasFlag(NetworkAccess.Internet))
                {
                    return false;
                }
                
                await _firebaseAuth.SendPasswordResetEmailAsync(email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email");
                return false;
            }
        }
        
        private async Task StoreAuthTokensAsync(IFirebaseUser user)
        {
            try
            {
                await _secureStorage.SetAsync(UserIdKey, user.Uid);
                await _secureStorage.SetAsync(UserEmailKey, user.Email ?? string.Empty);
                
                var token = await user.GetIdTokenAsync(false);
                if (!string.IsNullOrEmpty(token))
                {
                    await _secureStorage.SetAsync(RefreshTokenKey, token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store auth tokens");
            }
        }
        
        private async Task ClearStoredTokensAsync()
        {
            try
            {
                _secureStorage.Remove(RefreshTokenKey);
                _secureStorage.Remove(UserIdKey);
                _secureStorage.Remove(UserEmailKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear stored tokens");
            }
        }
        
        private User MapFirebaseUser(IFirebaseUser firebaseUser)
        {
            return new User
            {
                Id = firebaseUser.Uid,
                Email = firebaseUser.Email ?? string.Empty,
                DisplayName = firebaseUser.DisplayName ?? firebaseUser.Email ?? string.Empty,
                IsEmailVerified = firebaseUser.IsEmailVerified,
                PhotoUrl = firebaseUser.PhotoUrl,
                LastLoginAt = DateTime.UtcNow
            };
        }
        
        private string GetUserFriendlyErrorMessage(FirebaseAuthException ex)
        {
            return ex.Reason switch
            {
                AuthErrorReason.WrongPassword => "Invalid password",
                AuthErrorReason.UserNotFound => "User not found",
                AuthErrorReason.EmailAlreadyInUse => "Email already registered",
                AuthErrorReason.WeakPassword => "Password is too weak",
                AuthErrorReason.InvalidEmail => "Invalid email address",
                AuthErrorReason.TooManyRequests => "Too many attempts. Please try again later",
                _ => "Authentication failed"
            };
        }
        
        public void Dispose()
        {
            _tokenRefreshTimer?.Dispose();
            _authStateListener?.Dispose();
            _authStateSubject?.Dispose();
            _refreshLock?.Dispose();
        }
    }
}
```

### Step 6: Firestore Service with Offline Support

#### File: `Services/Firebase/FirestoreService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Plugin.Firebase.Firestore;
using Polly;
using FlockForge.Core.Interfaces;
using FlockForge.Core.Models;

namespace FlockForge.Services.Firebase
{
    public class FirestoreService : IDataService, IDisposable
    {
        private readonly IFirebaseFirestore _firestore;
        private readonly IConnectivity _connectivity;
        private readonly IAuthenticationService _authService;
        private readonly ILogger<FirestoreService> _logger;
        private readonly IAsyncPolicy _retryPolicy;
        private readonly Dictionary<string, IDisposable> _listeners = new();
        
        public FirestoreService(
            IFirebaseFirestore firestore,
            IConnectivity connectivity,
            IAuthenticationService authService,
            ILogger<FirestoreService> logger)
        {
            _firestore = firestore;
            _connectivity = connectivity;
            _authService = authService;
            _logger = logger;
            
            ConfigureRetryPolicy();
        }
        
        private void ConfigureRetryPolicy()
        {
            _retryPolicy = Policy
                .Handle<Exception>(ex => IsTransientError(ex))
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning("Retry {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
                    });
        }
        
        private bool IsTransientError(Exception ex)
        {
            return ex is FirebaseException fbEx && 
                   (fbEx.Message.Contains("unavailable") || 
                    fbEx.Message.Contains("deadline exceeded") ||
                    fbEx.Message.Contains("internal"));
        }
        
        public async Task<T?> GetAsync<T>(string documentId) where T : BaseEntity
        {
            try
            {
                if (!_authService.IsAuthenticated)
                {
                    _logger.LogWarning("Attempted to get document while not authenticated");
                    return null;
                }
                
                var docRef = _firestore
                    .Collection(GetCollectionName<T>())
                    .Document(documentId);
                
                // Firestore will automatically use cached data when offline
                var snapshot = await _retryPolicy.ExecuteAsync(async () => 
                    await docRef.GetAsync());
                
                if (snapshot.Exists)
                {
                    var data = snapshot.ToObject<T>();
                    return data;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get document {DocumentId}", documentId);
                return null;
            }
        }
        
        public async Task<IReadOnlyList<T>> GetAllAsync<T>() where T : BaseEntity
        {
            try
            {
                if (!_authService.IsAuthenticated)
                {
                    return new List<T>();
                }
                
                var userId = _authService.CurrentUser!.Id;
                var collectionName = GetCollectionName<T>();
                
                var query = _firestore
                    .Collection(collectionName)
                    .WhereEqualTo("userId", userId)
                    .WhereEqualTo("isDeleted", false)
                    .OrderBy("updatedAt", false)
                    .Limit(1000);
                
                var snapshot = await _retryPolicy.ExecuteAsync(async () => 
                    await query.GetAsync());
                
                return snapshot.Documents
                    .Select(d => d.ToObject<T>())
                    .Where(d => d != null)
                    .ToList()!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all documents");
                return new List<T>();
            }
        }
        
        public async Task<bool> SaveAsync<T>(T entity) where T : BaseEntity
        {
            try
            {
                if (!_authService.IsAuthenticated)
                {
                    _logger.LogWarning("Cannot save - no authenticated user");
                    return false;
                }
                
                // Update metadata
                entity.UpdatedAt = DateTime.UtcNow;
                if (string.IsNullOrEmpty(entity.Id))
                {
                    entity.Id = Guid.NewGuid().ToString();
                    entity.CreatedAt = DateTime.UtcNow;
                }
                
                // Set user association - this works even offline
                entity.UserId = _authService.CurrentUser!.Id;
                
                var docRef = _firestore
                    .Collection(GetCollectionName<T>())
                    .Document(entity.Id);
                
                // This will queue for sync if offline
                await _retryPolicy.ExecuteAsync(async () => 
                    await docRef.SetAsync(entity));
                
                _logger.LogInformation("Saved entity {EntityId} for user {UserId} (offline: {IsOffline})", 
                    entity.Id, 
                    entity.UserId, 
                    _connectivity.NetworkAccess != NetworkAccess.Internet);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save entity {EntityId}", entity.Id);
                return false;
            }
        }
        
        public async Task<bool> DeleteAsync<T>(string documentId) where T : BaseEntity
        {
            try
            {
                var entity = await GetAsync<T>(documentId);
                if (entity == null) return false;
                
                // Soft delete
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.UtcNow;
                
                return await SaveAsync(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete document {DocumentId}", documentId);
                return false;
            }
        }
        
        public async Task<bool> BatchSaveAsync<T>(IEnumerable<T> entities) where T : BaseEntity
        {
            try
            {
                var entityList = entities.ToList();
                if (!entityList.Any()) return true;
                
                if (!_authService.IsAuthenticated)
                {
                    return false;
                }
                
                var userId = _authService.CurrentUser!.Id;
                
                // Firestore batch limit is 500
                var batches = entityList.Chunk(500);
                
                foreach (var batch in batches)
                {
                    var writeBatch = _firestore.StartBatch();
                    
                    foreach (var entity in batch)
                    {
                        entity.UpdatedAt = DateTime.UtcNow;
                        entity.UserId = userId;
                        
                        if (string.IsNullOrEmpty(entity.Id))
                        {
                            entity.Id = Guid.NewGuid().ToString();
                            entity.CreatedAt = DateTime.UtcNow;
                        }
                        
                        var docRef = _firestore
                            .Collection(GetCollectionName<T>())
                            .Document(entity.Id);
                        
                        writeBatch.Set(docRef, entity);
                    }
                    
                    await _retryPolicy.ExecuteAsync(async () => 
                        await writeBatch.CommitAsync());
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to batch save entities");
                return false;
            }
        }
        
        public async Task<IReadOnlyList<T>> QueryAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity
        {
            try
            {
                // For complex expression tree parsing, you would implement an expression visitor
                // For now, we'll use a simpler approach
                var allData = await GetAllAsync<T>();
                var compiledPredicate = predicate.Compile();
                return allData.Where(compiledPredicate).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query documents");
                return new List<T>();
            }
        }
        
        public IObservable<T> DocumentChanged<T>(string documentId) where T : BaseEntity
        {
            var subject = new Subject<T>();
            var key = $"{typeof(T).Name}:{documentId}";
            
            // Remove existing listener if any
            if (_listeners.TryGetValue(key, out var existing))
            {
                existing.Dispose();
            }
            
            var docRef = _firestore
                .Collection(GetCollectionName<T>())
                .Document(documentId);
            
            var listener = docRef.AddSnapshotListener((snapshot, error) =>
            {
                if (error != null)
                {
                    _logger.LogError(error, "Document listener error");
                    return;
                }
                
                if (snapshot != null && snapshot.Exists)
                {
                    var data = snapshot.ToObject<T>();
                    if (data != null)
                    {
                        subject.OnNext(data);
                    }
                }
            });
            
            _listeners[key] = listener;
            
            return subject;
        }
        
        public IObservable<IReadOnlyList<T>> CollectionChanged<T>() where T : BaseEntity
        {
            var subject = new Subject<IReadOnlyList<T>>();
            var key = $"{typeof(T).Name}:collection";
            
            // Remove existing listener if any
            if (_listeners.TryGetValue(key, out var existing))
            {
                existing.Dispose();
            }
            
            if (!_authService.IsAuthenticated)
            {
                return subject;
            }
            
            var userId = _authService.CurrentUser!.Id;
            
            var query = _firestore
                .Collection(GetCollectionName<T>())
                .WhereEqualTo("userId", userId)
                .WhereEqualTo("isDeleted", false)
                .OrderBy("updatedAt", false);
            
            var listener = query.AddSnapshotListener((snapshot, error) =>
            {
                if (error != null)
                {
                    _logger.LogError(error, "Collection listener error");
                    return;
                }
                
                if (snapshot != null)
                {
                    var data = snapshot.Documents
                        .Select(d => d.ToObject<T>())
                        .Where(d => d != null)
                        .ToList();
                    
                    subject.OnNext(data!);
                }
            });
            
            _listeners[key] = listener;
            
            return subject;
        }
        
        private string GetCollectionName<T>() where T : BaseEntity
        {
            var typeName = typeof(T).Name.ToLowerInvariant();
            
            // Handle pluralization
            return typeName switch
            {
                "farm" => "farms",
                "farmer" => "farmers",
                "lambingseason" => "lambing_seasons",
                "breeding" => "breeding",
                "scanning" => "scanning",
                "lambing" => "lambing",
                "weaning" => "weaning",
                _ => $"{typeName}s"
            };
        }
        
        public void Dispose()
        {
            foreach (var listener in _listeners.Values)
            {
                listener?.Dispose();
            }
            _listeners.Clear();
        }
    }
}
```

### Step 7: Platform-Specific Firebase Initialization

#### File: `Platforms/iOS/Services/iOSFirebaseInitializer.cs`

```csharp
using Firebase.Core;
using Firebase.Auth;
using Firebase.CloudFirestore;
using FlockForge.Core.Interfaces;

namespace FlockForge.Platforms.iOS.Services
{
    public class iOSFirebaseInitializer : IFirebaseInitializer
    {
        public void Initialize()
        {
            // Initialize Firebase
            App.Configure();
            
            // Configure Firestore settings with offline persistence
            var settings = new Settings
            {
                IsPersistenceEnabled = true,
                CacheSizeBytes = 104857600 // 100MB (use numeric value)
            };
            
            Firestore.SharedInstance.Settings = settings;
        }
    }
}
```

#### File: `Platforms/iOS/AppDelegate.cs`

```csharp
using Foundation;
using UIKit;
using Microsoft.Maui;
using FlockForge.Platforms.iOS.Services;

namespace FlockForge.Platforms.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp()
        {
            // Initialize Firebase before creating the app
            var firebaseInitializer = new iOSFirebaseInitializer();
            firebaseInitializer.Initialize();
            
            return MauiProgram.CreateMauiApp();
        }
    }
}
```

#### File: `Platforms/Android/Services/AndroidFirebaseInitializer.cs`

```csharp
using Android.App;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using FlockForge.Core.Interfaces;

namespace FlockForge.Platforms.Android.Services
{
    public class AndroidFirebaseInitializer : IFirebaseInitializer
    {
        public void Initialize()
        {
            // Initialize Firebase
            FirebaseApp.InitializeApp(Application.Context);
            
            // Configure Firestore settings with offline persistence
            var settings = new FirebaseFirestoreSettings.Builder()
                .SetPersistenceEnabled(true)
                .SetCacheSizeBytes(104857600) // 100MB
                .Build();
            
            FirebaseFirestore.Instance.FirestoreSettings = settings;
        }
    }
}
```

#### File: `Platforms/Android/MainActivity.cs`

```csharp
using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui;
using FlockForge.Platforms.Android.Services;

namespace FlockForge.Platforms.Android
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Initialize Firebase
            var firebaseInitializer = new AndroidFirebaseInitializer();
            firebaseInitializer.Initialize();
        }
    }
}
```

### Step 8: Configure Dependency Injection

#### File: `MauiProgram.cs`

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;
using FlockForge.Core.Interfaces;
using FlockForge.Services.Firebase;
using FlockForge.Services.Navigation;
using FlockForge.ViewModels;
using FlockForge.Views;

namespace FlockForge
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register Firebase services
            builder.Services.AddSingleton<IFirebaseAuth>(CrossFirebaseAuth.Current);
            builder.Services.AddSingleton<IFirebaseFirestore>(CrossFirebaseFirestore.Current);
            
            // Register authentication service
            builder.Services.AddSingleton<IAuthenticationService, FirebaseAuthenticationService>();
            
            // Register data service
            builder.Services.AddSingleton<IDataService, FirestoreService>();
            
            // Register navigation service
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            
            // Register platform services
            builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
            builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);
            
            // Register ViewModels
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<RegisterViewModel>();
            builder.Services.AddTransient<FarmListViewModel>();
            builder.Services.AddTransient<FarmDetailViewModel>();
            
            // Register Views
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<FarmListPage>();
            builder.Services.AddTransient<FarmDetailPage>();
            
            // Configure logging
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
```

### Step 9: Update App.xaml.cs for Auth State Management

#### File: `App.xaml.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using FlockForge.Core.Interfaces;
using FlockForge.Core.Models;
using FlockForge.Views;

namespace FlockForge
{
    public partial class App : Application
    {
        private readonly IAuthenticationService _authService;
        private IDisposable? _authSubscription;
        
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            
            _authService = serviceProvider.GetRequiredService<IAuthenticationService>();
            
            // Subscribe to auth state changes
            _authSubscription = _authService.AuthStateChanged.Subscribe(OnAuthStateChanged);
            
            // Set initial page based on auth state
            MainPage = _authService.IsAuthenticated 
                ? new AppShell() 
                : new NavigationPage(new LoginPage());
        }
        
        private void OnAuthStateChanged(User? user)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (user != null)
                {
                    // User is authenticated
                    if (MainPage is not AppShell)
                    {
                        MainPage = new AppShell();
                    }
                }
                else
                {
                    // User is not authenticated
                    if (MainPage is not NavigationPage navPage || 
                        navPage.CurrentPage is not LoginPage)
                    {
                        MainPage = new NavigationPage(new LoginPage());
                    }
                }
            });
        }
        
        protected override void OnSleep()
        {
            // Don't clear auth on sleep
            base.OnSleep();
        }
        
        protected override void OnResume()
        {
            // Optionally attempt token refresh on resume if online
            Task.Run(async () =>
            {
                if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                {
                    await _authService.RefreshTokenAsync();
                }
            });
            
            base.OnResume();
        }
    }
}
```

### Step 10: Firebase Security Rules

#### File: `firestore.rules`

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    // Helper functions
    function isAuthenticated() {
      return request.auth != null;
    }
    
    function isEmailVerified() {
      return request.auth.token.email_verified == true;
    }
    
    function isOwner(userId) {
      return request.auth.uid == userId;
    }
    
    function hasRequiredFields(fields) {
      return request.resource.data.keys().hasAll(fields);
    }
    
    // Farms collection
    match /farms/{farmId} {
      allow read: if isAuthenticated() && isEmailVerified() && 
                    isOwner(resource.data.userId);
      allow create: if isAuthenticated() && isEmailVerified() &&
                    request.resource.data.userId == request.auth.uid &&
                    hasRequiredFields(['farmName', 'userId']);
      allow update: if isAuthenticated() && isEmailVerified() && 
                    isOwner(resource.data.userId) &&
                    request.resource.data.userId == resource.data.userId;
      allow delete: if false; // Soft delete only
    }
    
    // Lambing seasons collection
    match /lambing_seasons/{seasonId} {
      allow read: if isAuthenticated() && isEmailVerified() && 
                    isOwner(resource.data.userId);
      allow create: if isAuthenticated() && isEmailVerified() &&
                    request.resource.data.userId == request.auth.uid;
      allow update: if isAuthenticated() && isEmailVerified() && 
                    isOwner(resource.data.userId);
      allow delete: if false;
    }
    
    // Apply same pattern to all collections
    match /{collection}/{document} {
      allow read: if isAuthenticated() && isEmailVerified() &&
                    resource.data.userId == request.auth.uid;
      allow write: if isAuthenticated() && isEmailVerified() &&
                    request.resource.data.userId == request.auth.uid;
    }
  }
}
```

### Step 11: Firebase Configuration Files

#### For Android: Place `google-services.json` in `Platforms/Android/`
#### For iOS: Place `GoogleService-Info.plist` in `Platforms/iOS/`

Ensure both files have the following build actions:
- Android: `GoogleServicesJson`
- iOS: `BundleResource`

### Step 12: Create Example ViewModels

#### File: `ViewModels/BaseViewModel.cs`

```csharp
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FlockForge.Core.Interfaces;

namespace FlockForge.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        protected readonly IAuthenticationService AuthService;
        protected readonly IDataService DataService;
        
        [ObservableProperty]
        private bool isBusy;
        
        [ObservableProperty]
        private string? errorMessage;
        
        [ObservableProperty]
        private bool isOffline;
        
        protected BaseViewModel(
            IAuthenticationService authService,
            IDataService dataService,
            IConnectivity connectivity)
        {
            AuthService = authService;
            DataService = dataService;
            
            // Monitor connectivity
            connectivity.ConnectivityChanged += OnConnectivityChanged;
            IsOffline = connectivity.NetworkAccess != NetworkAccess.Internet;
        }
        
        private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            IsOffline = e.NetworkAccess != NetworkAccess.Internet;
        }
        
        protected async Task ExecuteSafelyAsync(Func<Task> operation, string? errorMessage = null)
        {
            if (IsBusy) return;
            
            try
            {
                IsBusy = true;
                ErrorMessage = null;
                await operation();
            }
            catch (Exception ex)
            {
                ErrorMessage = errorMessage ?? "An error occurred";
                // Log error
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
```

### Step 13: Deployment Checklist

#### Firebase Console Setup:
1. **Authentication**:
   - Enable Email/Password provider
   - Enable Google provider
   - Enable Apple provider (configure with Apple Developer account)
   - Configure authorized domains

2. **Firestore**:
   - Create database in production mode
   - Deploy security rules from `firestore.rules`
   - Create composite indexes:
     ```
     Collection: farms
     Fields: userId (Ascending), isDeleted (Ascending), updatedAt (Descending)
     
     Collection: lambing_seasons
     Fields: userId (Ascending), isDeleted (Ascending), updatedAt (Descending)
     ```

3. **Project Settings**:
   - Download `google-services.json` for Android
   - Download `GoogleService-Info.plist` for iOS
   - Configure OAuth redirect URLs

#### Build Configuration:
1. Update `Platforms/Android/AndroidManifest.xml`:
   ```xml
   <uses-permission android:name="android.permission.INTERNET" />
   <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
   ```

2. Update iOS Info.plist with URL schemes for OAuth

3. Ensure minimum SDK versions:
   - Android: API 21+
   - iOS: 11.0+

### Step 14: Testing Procedure

1. **Online Authentication Test**:
   - Test email/password sign up
   - Test email/password sign in
   - Test Google SSO
   - Test Apple SSO (iOS only)
   - Verify email verification flow

2. **Offline Persistence Test**:
   - Sign in while online
   - Create a farm record
   - Enable airplane mode
   - Close and restart app
   - Verify user remains authenticated
   - Create another farm record offline
   - Verify both records appear
   - Disable airplane mode
   - Verify sync occurs automatically

3. **Long-term Offline Test**:
   - Sign in and create data
   - Enable airplane mode
   - Use app for extended period (hours/days)
   - Verify no authentication prompts
   - Verify all data operations work
   - Re-enable network
   - Verify sync completes successfully

### Critical Implementation Notes:

1. **NEVER** call sign out unless user explicitly requests it
2. **ALWAYS** check connectivity before network operations
3. **NEVER** block UI for auth refresh operations
4. **ALWAYS** allow offline data operations for authenticated users
5. **NEVER** clear user session on network errors
6. **ALWAYS** persist user credentials in secure storage
7. **NEVER** require network for existing user sign-in

### Success Criteria:

1. ✅ User can sign in once with network
2. ✅ User remains authenticated indefinitely offline
3. ✅ All CRUD operations work offline
4. ✅ Data syncs automatically when online
5. ✅ No authentication interruptions in the field
6. ✅ Token refresh happens silently when online
7. ✅ App handles months of offline usage

This implementation ensures farmers can work uninterrupted in the field without any authentication issues, while maintaining security when network is available.