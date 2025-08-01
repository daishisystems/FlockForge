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

Create `Core/Models/LambingSeason.cs`:
```csharp
using System;
using Plugin.Firebase.Firestore;

namespace FlockForge.Core.Models
{
    [FirestoreObject]
    public class LambingSeason : BaseEntity
    {
        [FirestoreProperty("farmId")]
        public string FarmId { get; set; } = string.Empty;
        
        [FirestoreProperty("code")]
        public string Code { get; set; } = string.Empty;
        
        [FirestoreProperty("groupName")]
        public string GroupName { get; set; } = string.Empty;
        
        [FirestoreProperty("matingStart")]
        public DateTime MatingStart { get; set; }
        
        [FirestoreProperty("matingEnd")]
        public DateTime MatingEnd { get; set; }
        
        [FirestoreProperty("lambingStart")]
        public DateTime LambingStart { get; set; }
        
        [FirestoreProperty("lambingEnd")]
        public DateTime LambingEnd { get; set; }
        
        [FirestoreProperty("active")]
        public bool Active { get; set; }
    }
}
```

## Phase 6: Platform-Specific Configuration

### Step 6.1: iOS Platform Configuration

Create `Platforms/iOS/Services/iOSFirebaseInitializer.cs`:

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

Update `Platforms/iOS/AppDelegate.cs`:

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

### Step 6.2: Android Platform Configuration

Create `Platforms/Android/Services/AndroidFirebaseInitializer.cs`:

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

Update `Platforms/Android/MainActivity.cs`:

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

## Phase 7: Dependency Injection Setup

Update `MauiProgram.cs`:

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;
using FlockForge.Core.Interfaces;
using FlockForge.Services.Firebase;

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
            
            // Register platform services
            builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
            builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);
            
            // Register ViewModels
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<RegisterViewModel>();
            builder.Services.AddTransient<FarmListViewModel>();
            builder.Services.AddTransient<FarmDetailViewModel>();
            
            // Configure logging
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
```

## Phase 8: Update ViewModels to Use New Services

### Step 8.1: Create Base ViewModel

Create `ViewModels/BaseViewModel.cs`:

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

### Step 8.2: Update Login ViewModel

Create `ViewModels/LoginViewModel.cs`:

```csharp
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlockForge.Core.Interfaces;

namespace FlockForge.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthenticationService _authService;
        private readonly INavigationService _navigationService;
        
        [ObservableProperty]
        private string email = string.Empty;
        
        [ObservableProperty]
        private string password = string.Empty;
        
        [ObservableProperty]
        private bool isLoading;
        
        [ObservableProperty]
        private string? errorMessage;
        
        public LoginViewModel(
            IAuthenticationService authService,
            INavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;
        }
        
        [RelayCommand]
        private async Task SignInAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter email and password";
                return;
            }
            
            IsLoading = true;
            ErrorMessage = null;
            
            try
            {
                var result = await _authService.SignInWithEmailPasswordAsync(Email, Password);
                
                if (result.IsSuccess)
                {
                    if (result.RequiresEmailVerification)
                    {
                        await _navigationService.NavigateToAsync("EmailVerification");
                    }
                    else
                    {
                        await _navigationService.NavigateToAsync("Home");
                    }
                }
                else
                {
                    ErrorMessage = result.ErrorMessage;
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        [RelayCommand]
        private async Task SignInWithGoogleAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            
            try
            {
                var result = await _authService.SignInWithGoogleAsync();
                
                if (result.IsSuccess)
                {
                    await _navigationService.NavigateToAsync("Home");
                }
                else
                {
                    ErrorMessage = result.ErrorMessage;
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        [RelayCommand]
        private async Task SignInWithAppleAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            
            try
            {
                var result = await _authService.SignInWithAppleAsync();
                
                if (result.IsSuccess)
                {
                    await _navigationService.NavigateToAsync("Home");
                }
                else
                {
                    ErrorMessage = result.ErrorMessage;
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        [RelayCommand]
        private async Task NavigateToRegisterAsync()
        {
            await _navigationService.NavigateToAsync("Register");
        }
        
        [RelayCommand]
        private async Task ForgotPasswordAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Please enter your email address";
                return;
            }
            
            IsLoading = true;
            ErrorMessage = null;
            
            try
            {
                var success = await _authService.SendPasswordResetEmailAsync(Email);
                
                if (success)
                {
                    await _navigationService.DisplayAlertAsync(
                        "Password Reset",
                        "Check your email for password reset instructions.",
                        "OK");
                }
                else
                {
                    ErrorMessage = "Failed to send password reset email";
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
```

### Step 8.3: Create Farm List ViewModel

Create `ViewModels/FarmListViewModel.cs`:

```csharp
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlockForge.Core.Interfaces;
using FlockForge.Core.Models;

namespace FlockForge.ViewModels
{
    public partial class FarmListViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private IDisposable? _farmSubscription;
        
        [ObservableProperty]
        private ObservableCollection<Farm> farms = new();
        
        [ObservableProperty]
        private bool hasNoFarms;
        
        public FarmListViewModel(
            IAuthenticationService authService,
            IDataService dataService,
            IConnectivity connectivity,
            INavigationService navigationService)
            : base(authService, dataService, connectivity)
        {
            _navigationService = navigationService;
        }
        
        public async Task InitializeAsync()
        {
            await LoadFarmsAsync();
            SubscribeToFarmChanges();
        }
        
        private async Task LoadFarmsAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                var farmList = await DataService.GetAllAsync<Farm>();
                Farms = new ObservableCollection<Farm>(farmList);
                HasNoFarms = !Farms.Any();
            });
        }
        
        private void SubscribeToFarmChanges()
        {
            _farmSubscription?.Dispose();
            _farmSubscription = DataService.CollectionChanged<Farm>()
                .Subscribe(farms =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Farms = new ObservableCollection<Farm>(farms);
                        HasNoFarms = !Farms.Any();
                    });
                });
        }
        
        [RelayCommand]
        private async Task AddFarmAsync()
        {
            await _navigationService.NavigateToAsync("FarmDetail");
        }
        
        [RelayCommand]
        private async Task EditFarmAsync(Farm farm)
        {
            await _navigationService.NavigateToAsync("FarmDetail", farm);
        }
        
        [RelayCommand]
        private async Task DeleteFarmAsync(Farm farm)
        {
            var confirm = await _navigationService.DisplayAlertAsync(
                "Delete Farm",
                $"Are you sure you want to delete {farm.FarmName}?",
                "Delete",
                "Cancel");
            
            if (confirm)
            {
                await ExecuteSafelyAsync(async () =>
                {
                    await DataService.DeleteAsync<Farm>(farm.Id);
                });
            }
        }
        
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadFarmsAsync();
        }
        
        public void Dispose()
        {
            _farmSubscription?.Dispose();
        }
    }
}
```

## Phase 9: Setup Firestore Security Rules

Create `firestore.rules`:

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
    
    // Farmers collection
    match /farmers/{farmerId} {
      allow read: if isAuthenticated() && isEmailVerified() && isOwner(farmerId);
      allow create: if isAuthenticated() && isOwner(farmerId) && 
                    hasRequiredFields(['email']);
      allow update: if isAuthenticated() && isEmailVerified() && isOwner(farmerId) &&
                    request.resource.data.userId == farmerId;
      allow delete: if false; // Soft delete only
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
      allow delete: if false;
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
    
    // Apply same pattern to other collections
    match /{collection}/{document} {
      allow read: if isAuthenticated() && isEmailVerified() &&
                    resource.data.userId == request.auth.uid;
      allow write: if isAuthenticated() && isEmailVerified() &&
                    request.resource.data.userId == request.auth.uid;
    }
  }
}
```

## Phase 10: Testing Implementation

### Step 10.1: Create Integration Test

Create `Tests/FirebaseIntegrationTest.cs`:

```csharp
using System;
using System.Threading.Tasks;
using FlockForge.Core.Interfaces;
using FlockForge.Core.Models;

namespace FlockForge.Tests
{
    public class FirebaseIntegrationTest
    {
        private readonly IAuthenticationService _authService;
        private readonly IDataService _dataService;
        
        public FirebaseIntegrationTest(
            IAuthenticationService authService,
            IDataService dataService)
        {
            _authService = authService;
            _dataService = dataService;
        }
        
        public async Task RunTestsAsync()
        {
            Console.WriteLine("Starting Firebase Integration Tests...");
            
            // Test Authentication
            await TestAuthenticationAsync();
            
            // Test Firestore Operations
            await TestFirestoreOperationsAsync();
            
            // Test Offline Mode
            await TestOfflineModeAsync();
            
            Console.WriteLine("All tests completed!");
        }
        
        private async Task TestAuthenticationAsync()
        {
            Console.WriteLine("\n=== Testing Authentication ===");
            
            // Test Sign Up
            var signUpResult = await _authService.SignUpWithEmailPasswordAsync(
                $"test{Guid.NewGuid():N}@test.com",
                "TestPassword123!");
            
            Console.WriteLine($"Sign Up: {(signUpResult.IsSuccess ? "✓ Success" : "✗ Failed")}");
            
            // Test Sign In
            var signInResult = await _authService.SignInWithEmailPasswordAsync(
                "existing@test.com",
                "password");
            
            Console.WriteLine($"Sign In: {(signInResult.IsSuccess ? "✓ Success" : "✗ Failed")}");
        }
        
        private async Task TestFirestoreOperationsAsync()
        {
            Console.WriteLine("\n=== Testing Firestore Operations ===");
            
            // Create a test farm
            var farm = new Farm
            {
                FarmName = "Test Farm",
                Breed = "Dorper",
                TotalProductionEwes = 100,
                Size = 50,
                SizeUnit = "hectares"
            };
            
            // Test Save
            var saveResult = await _dataService.SaveAsync(farm);
            Console.WriteLine($"Save: {(saveResult ? "✓ Success" : "✗ Failed")}");
            
            // Test Get
            var retrieved = await _dataService.GetAsync<Farm>(farm.Id);
            Console.WriteLine($"Get: {(retrieved != null ? "✓ Success" : "✗ Failed")}");
            
            // Test Update
            if (retrieved != null)
            {
                retrieved.FarmName = "Updated Test Farm";
                var updateResult = await _dataService.SaveAsync(retrieved);
                Console.WriteLine($"Update: {(updateResult ? "✓ Success" : "✗ Failed")}");
            }
            
            // Test Query
            var farms = await _dataService.GetAllAsync<Farm>();
            Console.WriteLine($"Query: ✓ Retrieved {farms.Count} farms");
            
            // Test Delete (soft)
            var deleteResult = await _dataService.DeleteAsync<Farm>(farm.Id);
            Console.WriteLine($"Delete: {(deleteResult ? "✓ Success" : "✗ Failed")}");
        }
        
        private async Task TestOfflineModeAsync()
        {
            Console.WriteLine("\n=== Testing Offline Mode ===");
            Console.WriteLine("Note: Manually disable network to test offline functionality");
            
            // Create data while offline
            var offlineFarm = new Farm
            {
                FarmName = "Offline Test Farm",
                Breed = "Merino",
                TotalProductionEwes = 200
            };
            
            var saveResult = await _dataService.SaveAsync(offlineFarm);
            Console.WriteLine($"Offline Save: {(saveResult ? "✓ Success" : "✗ Failed")}");
            
            // Try to retrieve while offline
            var retrieved = await _dataService.GetAsync<Farm>(offlineFarm.Id);
            Console.WriteLine($"Offline Get: {(retrieved != null ? "✓ Success" : "✗ Failed")}");
            
            Console.WriteLine("Re-enable network to verify sync...");
        }
    }
}
```

## Phase 11: Deployment Checklist

### Pre-Deployment Steps

1. **Firebase Console Configuration**
   ```
   ✓ Enable Email/Password authentication
   ✓ Enable Google Sign-In
   ✓ Enable Apple Sign-In (for iOS)
   ✓ Configure OAuth redirect URLs
   ✓ Deploy Firestore security rules
   ✓ Create Firestore indexes for complex queries
   ```

2. **Platform Configuration Files**
   ```
   ✓ Add google-services.json to Platforms/Android/
   ✓ Add GoogleService-Info.plist to Platforms/iOS/
   ✓ Ensure files are included in project with proper build action
   ```

3. **Test Offline Functionality**
   ```
   ✓ Test app startup while offline
   ✓ Test data creation while offline
   ✓ Test data sync when coming online
   ✓ Test authentication state persistence
   ```

4. **Performance Verification**
   ```
   ✓ Verify Firestore cache size is appropriate
   ✓ Test with large datasets
   ✓ Monitor memory usage
   ✓ Check for memory leaks with listeners
   ```

### Monitoring Setup

```csharp
// Add to FirestoreService.cs for monitoring
private void LogFirestoreMetrics()
{
    _firestore.GetSnapshotMetadataAsync().ContinueWith(task =>
    {
        if (task.IsCompletedSuccessfully)
        {
            var metadata = task.Result;
            _logger.LogInformation(
                "Firestore Cache: FromCache={FromCache}, HasPendingWrites={HasPendingWrites}",
                metadata.IsFromCache,
                metadata.HasPendingWrites);
        }
    });
}
```

## Summary

This implementation provides:

1. **Authentication**: Email/password and SSO with proper token management
2. **Offline Support**: Full Firestore offline persistence without SQLite
3. **Real-time Updates**: Reactive listeners for data changes
4. **Error Handling**: Retry policies and proper error messages
5. **Security**: Proper Firestore rules and secure token storage

The key advantage is that Firestore handles all offline persistence, conflict resolution, and sync automatically, eliminating the need for a separate SQLite database and complex sync logic.# AI Agent Implementation Guide: Firebase Integration Replacement for FlockForge

## Overview

This guide provides step-by-step instructions for replacing the existing Firebase integration in FlockForge with an optimized implementation that includes email/password authentication, SSO support, and Firestore with built-in offline persistence.

## Prerequisites

Before starting, ensure the following are in place:
- Existing FlockForge .NET MAUI solution with Firebase dependencies
- Firebase project with Authentication and Firestore enabled
- Platform-specific configuration files (`google-services.json` for Android, `GoogleService-Info.plist` for iOS)

## Phase 1: Remove Existing Firebase Implementation

### Step 1.1: Identify and Document Existing Firebase Code

Search for and document all files containing Firebase-related code:

```bash
# Search patterns to find Firebase usage
grep -r "Firebase" --include="*.cs" --include="*.csproj"
grep -r "IAuth" --include="*.cs"
grep -r "Firestore" --include="*.cs"
grep -r "SignIn" --include="*.cs"
grep -r "Authentication" --include="*.cs"
```

Create a backup branch before making changes:
```bash
git checkout -b firebase-integration-backup
git add .
git commit -m "Backup: Current Firebase implementation"
git checkout -b feature/optimized-firebase-integration
```

### Step 1.2: Remove Existing Authentication Implementation

1. Locate and delete existing authentication service files (likely in `Services/` directory)
2. Remove authentication-related code from ViewModels
3. Comment out (don't delete yet) authentication calls in Views

### Step 1.3: Remove Existing Firestore Implementation

1. Locate and delete existing Firestore service files
2. Remove direct Firestore references from ViewModels
3. Comment out data access calls in Views

## Phase 2: Implement Core Interfaces and Models

### Step 2.1: Create Base Entities and Interfaces

Create the following directory structure:
```
FlockForge/
├── Core/
│   ├── Interfaces/
│   │   ├── IAuthenticationService.cs
│   │   ├── IDataService.cs
│   │   └── IFirebaseInitializer.cs
│   └── Models/
│       ├── AuthResult.cs
│       ├── BaseEntity.cs
│       └── User.cs
```

Create `Core/Models/BaseEntity.cs`:
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

Create `Core/Models/AuthResult.cs`:
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

Create `Core/Models/User.cs`:
```csharp
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

Create `Core/Interfaces/IAuthenticationService.cs`:
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

## Phase 3: Implement Firebase Authentication Service

### Step 3.1: Install Required NuGet Packages

Add the following packages to your .csproj file:

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

### Step 3.2: Create Firebase Authentication Service

Create `Services/Firebase/FirebaseAuthenticationService.cs`:

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
        
        private const string RefreshTokenKey = "firebase_refresh_token";
        private const string UserIdKey = "firebase_user_id";
        private const string UserEmailKey = "firebase_user_email";
        
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
            Task.Run(async () => await RestoreAuthStateAsync());
        }
        
        private void InitializeAuthStateListener()
        {
            _authStateListener = _firebaseAuth.AuthStateChanged.Subscribe(auth =>
            {
                var user = auth?.CurrentUser != null ? MapFirebaseUser(auth.CurrentUser) : null;
                CurrentUser = user;
                _authStateSubject.OnNext(user);
            });
        }
        
        private async Task RestoreAuthStateAsync()
        {
            try
            {
                var userId = await _secureStorage.GetAsync(UserIdKey);
                var email = await _secureStorage.GetAsync(UserEmailKey);
                
                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(email))
                {
                    // Attempt to refresh the session
                    await RefreshTokenAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore auth state");
            }
        }
        
        public async Task<AuthResult> SignInWithEmailPasswordAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_connectivity.NetworkAccess.HasFlag(NetworkAccess.Internet))
                {
                    return AuthResult.Failure("No internet connection available");
                }
                
                var result = await _firebaseAuth.SignInWithEmailAndPasswordAsync(email, password);
                
                if (result?.User == null)
                {
                    return AuthResult.Failure("Sign in failed");
                }
                
                await StoreAuthTokensAsync(result.User);
                
                if (!result.User.IsEmailVerified)
                {
                    return AuthResult.Success(MapFirebaseUser(result.User), requiresEmailVerification: true);
                }
                
                return AuthResult.Success(MapFirebaseUser(result.User));
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
                    return AuthResult.Failure("No internet connection available");
                }
                
                var result = await _firebaseAuth.CreateUserWithEmailAndPasswordAsync(email, password);
                
                if (result?.User == null)
                {
                    return AuthResult.Failure("Registration failed");
                }
                
                await result.User.SendEmailVerificationAsync();
                await StoreAuthTokensAsync(result.User);
                
                return AuthResult.Success(MapFirebaseUser(result.User), requiresEmailVerification: true);
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
                var result = await _firebaseAuth.SignInWithGoogleAsync();
                
                if (result?.User == null)
                {
                    return AuthResult.Failure("Google sign in failed");
                }
                
                await StoreAuthTokensAsync(result.User);
                return AuthResult.Success(MapFirebaseUser(result.User));
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
                var result = await _firebaseAuth.SignInWithAppleAsync();
                
                if (result?.User == null)
                {
                    return AuthResult.Failure("Apple sign in failed");
                }
                
                await StoreAuthTokensAsync(result.User);
                return AuthResult.Success(MapFirebaseUser(result.User));
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
        
        public async Task SignOutAsync()
        {
            try
            {
                await _firebaseAuth.SignOutAsync();
                await ClearStoredTokensAsync();
                CurrentUser = null;
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
                await _firebaseAuth.SendPasswordResetEmailAsync(email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email");
                return false;
            }
        }
        
        public async Task<AuthResult> RefreshTokenAsync()
        {
            await _refreshLock.WaitAsync();
            try
            {
                if (_firebaseAuth.CurrentUser != null)
                {
                    var token = await _firebaseAuth.CurrentUser.GetIdTokenAsync(true);
                    return AuthResult.Success(MapFirebaseUser(_firebaseAuth.CurrentUser));
                }
                
                return AuthResult.Failure("No current user to refresh");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                return AuthResult.Failure("Failed to refresh authentication");
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
            _authStateListener?.Dispose();
            _authStateSubject?.Dispose();
            _refreshLock?.Dispose();
        }
    }
}
```

## Phase 4: Implement Firestore Data Service with Offline Support

### Step 4.1: Create Data Service Interface

Create `Core/Interfaces/IDataService.cs`:
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

### Step 4.2: Create Firestore Service Implementation

Create `Services/Firebase/FirestoreService.cs`:

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
    public class FirestoreService : IDataService
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
            
            ConfigureFirestore();
            ConfigureRetryPolicy();
        }
        
        private void ConfigureFirestore()
        {
            // Firestore offline persistence is configured per platform
            // This is handled in platform-specific initialization
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
            // Identify transient errors that should be retried
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
                    return false;
                }
                
                // Update metadata
                entity.UpdatedAt = DateTime.UtcNow;
                if (string.IsNullOrEmpty(entity.Id))
                {
                    entity.Id = Guid.NewGuid().ToString();
                    entity.CreatedAt = DateTime.UtcNow;
                }
                
                // Set user association
                entity.UserId = _authService.CurrentUser!.Id;
                
                var docRef = _firestore
                    .Collection(GetCollectionName<T>())
                    .Document(entity.Id);
                
                await _retryPolicy.ExecuteAsync(async () => 
                    await docRef.SetAsync(entity));
                
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

## Phase 5: Create Entity Models with Firestore Attributes

### Step 5.1: Create Domain Models

Create `Core/Models/Farm.cs`:
```csharp
using System;
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