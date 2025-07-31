# Firebase Authentication Implementation

## Overview

This document describes the implementation of Firebase Authentication with email/password and Google SSO for the FlockForge application. The implementation provides secure user authentication with offline support, which is essential for the rural farming environment this application targets.

## Architecture

The authentication system follows a layered architecture:

1. **Firebase Service Layer** - Handles all Firebase Authentication operations
2. **ViewModel Layer** - Manages UI state and user interactions
3. **View Layer** - Provides the user interface for login and registration
4. **Token Management Layer** - Securely stores and manages authentication tokens
5. **Offline Support Layer** - Enables offline authentication and data access

## Core Components

### FirebaseService

The `FirebaseService` class implements the `IFirebaseService` interface and provides:

- Email/password authentication
- Google SSO authentication
- User registration
- Password reset functionality
- Token management
- Session handling

Key methods:
- `AuthenticateAsync()` - Authenticates with email/password
- `AuthenticateWithGoogleAsync()` - Authenticates with Google SSO
- `RegisterAsync()` - Registers a new user
- `RequestPasswordResetAsync()` - Sends password reset email
- `SignOutAsync()` - Signs out the current user

### TokenManager

The `TokenManager` class handles secure token storage and retrieval:

- Secure storage of Firebase authentication tokens
- User information caching
- Offline token generation and validation
- Token clearing on sign out

### ViewModels

#### LoginViewModel

Handles the login page functionality:
- Email/password validation
- Google SSO initiation
- Password reset requests
- Navigation to registration

#### RegisterViewModel

Handles the registration page functionality:
- User input validation
- Account creation
- Navigation to login

## Security Features

### Secure Password Handling

Passwords are handled using `SecureString` to prevent memory exposure:

```csharp
var securePassword = new SecureString();
foreach (char c in Password)
    securePassword.AppendChar(c);

using var credentials = new SecureCredentials(Email, securePassword);
```

### Token Storage

Authentication tokens are stored securely using platform-specific secure storage mechanisms:
- iOS: Keychain Services
- Android: Keystore System

### Offline Authentication

Users can work offline for up to 30 days with secure token validation:
- Cryptographically secure token generation
- Token expiration and renewal
- Secure token hashing for storage

## Implementation Details

### Email/Password Authentication

1. User enters email and password
2. Credentials are validated locally
3. Firebase Authentication is called
4. Successful authentication generates offline token
5. User information is stored securely
6. User is navigated to main application

### Google SSO Authentication

1. User taps "Sign in with Google" button
2. Google Sign-In flow is initiated
3. User selects/authorizes Google account
4. Firebase Authentication receives Google credentials
5. User account is created/linked if needed
6. Offline token is generated
7. User is navigated to main application

### Registration Flow

1. User provides email, password, and display name
2. Input validation is performed
3. Firebase Authentication creates new user
4. Email verification is requested
5. Offline token is generated
6. User is navigated to main application or email verification screen

### Password Reset

1. User enters email on login screen
2. User taps "Forgot Password" link
3. Firebase Authentication sends password reset email
4. User follows link in email to reset password

## Platform Configuration

### Android

- `google-services.json` file in the Android project
- SHA-1 fingerprint configured in Firebase Console
- Google Play Services dependencies

### iOS

- `GoogleService-Info.plist` file in the iOS project
- URL schemes configured for Google Sign-In callback
- iOS 9+ support for Google Sign-In

## Error Handling

The implementation provides comprehensive error handling:

- Network connectivity issues
- Invalid credentials
- Account disabled
- Email not verified
- Weak passwords
- Email already in use

Each error is mapped to a user-friendly message while preserving technical details for debugging.

## Offline Support

### Firestore Built-in Offline Persistence

Firestore provides automatic offline persistence that works seamlessly once enabled:

1. **Automatic Caching**: Firestore automatically caches all documents you read
2. **Write Queue**: When offline, all writes are queued locally
3. **Auto-Sync**: When connection returns, Firestore automatically:
   - Sends queued writes to the server
   - Retrieves any changes from the server
   - Resolves conflicts automatically

### Enabling Offline Persistence

```csharp
// Enable offline persistence (done once at app startup)
// For Plugin.Firebase:
CrossFirebaseFirestore.Current.Settings = new FirestoreSettings
{
    IsPersistenceEnabled = true
};

// That's it! No background service needed
```

### How It Works Automatically

- **Automatic Caching**: Firestore automatically caches all documents you read
- **Queue Writes**: When offline, all writes are queued locally
- **Auto-Sync**: When connection returns, Firestore automatically sends queued writes to the server and retrieves any changes from the server

### Real-time Listeners Work Offline

```csharp
// Listeners continue to work with cached data when offline
_firestore
    .GetCollection("users")
    .AddSnapshotListener<User>((snapshot) =>
    {
        // Called with cached data when offline
        // Automatically updates when back online
        var users = snapshot?.Documents?.ToList() ?? new List<User>();
        UpdateUI(users);
    });
### Token Generation

Offline tokens are generated using cryptographically secure random number generation:

```csharp
public static string GenerateOfflineToken()
{
    var bytes = RandomNumberGenerator.GetBytes(32);
    return Convert.ToBase64String(bytes);
}
```

### Token Validation

Tokens are hashed for secure storage and validated during offline access:

```csharp
public static string HashToken(string token)
{
    var bytes = Encoding.UTF8.GetBytes(token);
    var hash = SHA256.HashData(bytes);
    return Convert.ToBase64String(hash);
}
```


## Testing

The implementation has been tested with:

- Valid email/password authentication
- Invalid credentials handling
- Google SSO flow
- Registration with various input combinations
- Password reset functionality
- Offline token validation
- Session timeout and renewal

## Future Enhancements

Planned improvements include:

- Biometric authentication support (fingerprint/face recognition)
- Apple Sign-In integration
- Facebook SSO integration
- Multi-factor authentication
- Advanced account recovery options
- Device-specific authentication tokens

## Conclusion

This Firebase Authentication implementation provides a robust, secure authentication system for the FlockForge application. It supports both online and offline usage scenarios while maintaining strong security practices appropriate for agricultural applications used in remote locations.