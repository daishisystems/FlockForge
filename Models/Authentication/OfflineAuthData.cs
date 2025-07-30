namespace FlockForge.Models.Authentication;

/// <summary>
/// Encrypted offline authentication data stored in secure storage
/// </summary>
public sealed class OfflineAuthData
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string HashedToken { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }
    public bool BiometricEnabled { get; init; }
    public DateTimeOffset LastSuccessfulAuth { get; init; }
    
    public bool IsValid => DateTimeOffset.UtcNow < ExpiresAt;
}