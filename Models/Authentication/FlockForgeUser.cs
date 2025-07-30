using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FlockForge.Models.Entities;

namespace FlockForge.Models.Authentication;

[Table("Users")]
public class FlockForgeUser : BaseEntity
{
    private string _firebaseUid = string.Empty;
    private string _email = string.Empty;
    
    [Required]
    [MaxLength(128)]
    public string FirebaseUid 
    { 
        get => _firebaseUid;
        init => _firebaseUid = value ?? throw new ArgumentNullException(nameof(value));
    }
    
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email 
    { 
        get => _email;
        init => _email = value?.ToLowerInvariant() ?? throw new ArgumentNullException(nameof(value));
    }
    
    [MaxLength(256)]
    public string? DisplayName { get; set; }
    
    [MaxLength(2048)]
    public string? PhotoUrl { get; set; }
    
    public AuthProvider Provider { get; init; } = AuthProvider.EmailPassword;
    
    public bool IsProfileComplete { get; set; }
    
    public bool IsEmailVerified { get; set; }
    
    public DateTimeOffset LastLoginAt { get; set; } = DateTimeOffset.UtcNow;
    
    // Offline support - generous for farming use cases
    [MaxLength(512)]
    public string? OfflineTokenHash { get; set; }
    
    public DateTimeOffset? OfflineTokenExpiry { get; set; }
    
    public DateTimeOffset? LastSyncAt { get; set; }
    
    [NotMapped]
    public string EffectiveDisplayName =>
        !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName : Email.Split('@')[0];
    
    [NotMapped]
    public bool CanWorkOffline =>
        !string.IsNullOrEmpty(OfflineTokenHash) &&
        OfflineTokenExpiry.HasValue &&
        OfflineTokenExpiry.Value > DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Records successful login and updates timestamps
    /// </summary>
    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }
    
    /// <summary>
    /// Enables offline access with generous expiry (30 days default for farming)
    /// </summary>
    public void EnableOfflineAccess(string tokenHash, int validityDays = 30)
    {
        OfflineTokenHash = tokenHash;
        OfflineTokenExpiry = DateTimeOffset.UtcNow.AddDays(validityDays);
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }
    
    /// <summary>
    /// Extends offline access on successful sync (resets from now, not from current expiry)
    /// </summary>
    public void ExtendOfflineAccess(int additionalDays = 30)
    {
        if (OfflineTokenExpiry.HasValue)
        {
            OfflineTokenExpiry = DateTimeOffset.UtcNow.AddDays(additionalDays);
            LastSyncAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
            IsSynced = false;
        }
    }
    
    /// <summary>
    /// Marks user profile as complete
    /// </summary>
    public void MarkProfileComplete()
    {
        IsProfileComplete = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }
    
    /// <summary>
    /// Clears offline access (for sign out)
    /// </summary>
    public void ClearOfflineAccess()
    {
        OfflineTokenHash = null;
        OfflineTokenExpiry = null;
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }
}

public enum AuthProvider
{
    EmailPassword = 0,
    Google = 1,
    Apple = 2,
    Microsoft = 3,
    Biometric = 4
}