namespace FlockForge.Models.Authentication;

public sealed class AuthStateChangedEventArgs : EventArgs
{
    public FlockForgeUser? User { get; init; }
    public FlockForgeUser? PreviousUser { get; init; }
    public bool IsAuthenticated { get; init; }
    public AuthStateChangeReason Reason { get; init; }
    public AuthProvider? Provider { get; init; }
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
    
    // Additional context for better UX
    public bool IsAutoReauthentication { get; init; }
    public bool RequiresUserAction { get; init; }
    public string? ActionMessage { get; init; }
}

public enum AuthStateChangeReason
{
    SignIn = 0,
    SignOut = 1,
    TokenRefresh = 2,
    UserUpdated = 3,
    SessionExpired = 4,
    AccountLocked = 5,
    PasswordChanged = 6,
    BiometricEnabled = 7,
    OfflineLogin = 8
}