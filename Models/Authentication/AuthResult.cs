namespace FlockForge.Models.Authentication;

public sealed class AuthResult
{
    public bool Success { get; private init; }
    public FlockForgeUser? User { get; private init; }
    public string? ErrorCode { get; private init; }
    public string? ErrorMessage { get; private init; }
    public Dictionary<string, object>? ErrorContext { get; private init; }
    public bool RequiresAction { get; private init; }
    public AuthAction ActionRequired { get; private init; }
    
    private AuthResult() { }
    
    public static AuthResult Successful(FlockForgeUser user, AuthAction actionRequired = AuthAction.None)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new() 
        { 
            Success = true, 
            User = user,
            RequiresAction = actionRequired != AuthAction.None,
            ActionRequired = actionRequired
        };
    }
    
    public static AuthResult Failed(
        string errorCode, 
        string userMessage, 
        Dictionary<string, object>? context = null)
    {
        return new() 
        { 
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = userMessage,
            ErrorContext = context
        };
    }
    
    // Common failure scenarios
    public static AuthResult NetworkError() 
        => Failed("NETWORK_ERROR", "No internet connection. Please check your connection and try again.");
    
    public static AuthResult InvalidCredentials() 
        => Failed("INVALID_CREDENTIALS", "Invalid email or password. Please try again.");
    
    public static AuthResult AccountLocked(DateTimeOffset? until) 
        => Failed("ACCOUNT_LOCKED", 
            until.HasValue 
                ? $"Account is locked until {until.Value.ToLocalTime():g}. Please try again later."
                : "Account is locked. Please contact support.",
            new Dictionary<string, object> { ["LockedUntil"] = until ?? DateTimeOffset.MaxValue });
    
    public static AuthResult EmailNotVerified(string email) 
        => Failed("EMAIL_NOT_VERIFIED", 
            "Please verify your email address before signing in.",
            new Dictionary<string, object> { ["Email"] = email });
    
    public static AuthResult RequiresMfa() 
        => Failed("MFA_REQUIRED", "Multi-factor authentication is required.");
}

public enum AuthAction
{
    None = 0,
    CompleteProfile = 1,
    VerifyEmail = 2,
    ChangePassword = 3,
    SetupMfa = 4,
    AcceptTerms = 5
}