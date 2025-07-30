using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlockForge.Models.Entities;

/// <summary>
/// Represents a farmer/user profile with personal information and farm ownership
/// </summary>
[Table("Farmers")]
public class Farmer : BaseEntity
{
    private string _firstName = string.Empty;
    private string _surname = string.Empty;
    private string _email = string.Empty;
    private string _mobileNumber = string.Empty;

    /// <summary>
    /// Reference to the Firebase user account
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string FirebaseUid { get; init; } = string.Empty;

    /// <summary>
    /// Farmer's first name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FirstName
    {
        get => _firstName;
        set => _firstName = value?.Trim() ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Farmer's surname/last name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Surname
    {
        get => _surname;
        set => _surname = value?.Trim() ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Primary email address
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email
    {
        get => _email;
        set => _email = value?.ToLowerInvariant().Trim() ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Mobile/cell phone number for SMS notifications
    /// </summary>
    [Required]
    [Phone]
    [MaxLength(20)]
    public string MobileNumber
    {
        get => _mobileNumber;
        set => _mobileNumber = value?.Trim() ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Physical address for correspondence
    /// </summary>
    [MaxLength(500)]
    public string? Address { get; set; }

    /// <summary>
    /// City for address autocomplete
    /// </summary>
    [MaxLength(100)]
    public string? City { get; set; }

    /// <summary>
    /// Province/State
    /// </summary>
    [MaxLength(100)]
    public string? Province { get; set; }

    /// <summary>
    /// Postal/Zip code
    /// </summary>
    [MaxLength(20)]
    public string? ZipCode { get; set; }

    /// <summary>
    /// Sub region or district
    /// </summary>
    [MaxLength(100)]
    public string? SubRegion { get; set; }

    /// <summary>
    /// Profile photo URL (Firebase Storage or external)
    /// </summary>
    [MaxLength(2048)]
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Preferred language for the application
    /// </summary>
    [MaxLength(10)]
    public string PreferredLanguage { get; set; } = "en";

    /// <summary>
    /// Timezone for date/time display (e.g., "Africa/Johannesburg")
    /// </summary>
    [MaxLength(50)]
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Whether the farmer has completed their profile setup
    /// </summary>
    public bool IsProfileComplete { get; set; }

    /// <summary>
    /// Whether the farmer has verified their email address
    /// </summary>
    public bool IsEmailVerified { get; set; }

    /// <summary>
    /// Whether the farmer has verified their mobile number
    /// </summary>
    public bool IsMobileVerified { get; set; }

    /// <summary>
    /// Last time the farmer logged into the application
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Farms owned by this farmer
    /// </summary>
    public virtual ICollection<Farm> Farms { get; set; } = new List<Farm>();

    // Computed properties
    /// <summary>
    /// Full display name combining first name and surname
    /// </summary>
    [NotMapped]
    public string FullName => $"{FirstName} {Surname}".Trim();

    /// <summary>
    /// Display name with fallback to email if names are not available
    /// </summary>
    [NotMapped]
    public string DisplayName =>
        !string.IsNullOrWhiteSpace(FullName) ? FullName : Email.Split('@')[0];

    /// <summary>
    /// Initials for compact display (e.g., "J.D." for John Doe)
    /// </summary>
    [NotMapped]
    public string Initials
    {
        get
        {
            var firstInitial = !string.IsNullOrEmpty(FirstName) ? FirstName[0].ToString().ToUpper() : "";
            var lastInitial = !string.IsNullOrEmpty(Surname) ? Surname[0].ToString().ToUpper() : "";
            return $"{firstInitial}{(string.IsNullOrEmpty(lastInitial) ? "" : "." + lastInitial)}";
        }
    }

    /// <summary>
    /// Whether the farmer's profile is ready for full application use
    /// </summary>
    [NotMapped]
    public bool IsReadyForUse => IsProfileComplete && IsEmailVerified && Farms.Any();

    // Business methods
    /// <summary>
    /// Records a successful login and updates the timestamp
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Marks the profile as complete after all required fields are filled
    /// </summary>
    public void CompleteProfile()
    {
        if (string.IsNullOrWhiteSpace(FirstName) ||
            string.IsNullOrWhiteSpace(Surname) ||
            string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(MobileNumber))
        {
            throw new InvalidOperationException("All required fields must be completed before marking profile as complete");
        }

        IsProfileComplete = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Verifies the farmer's email address
    /// </summary>
    public void VerifyEmail()
    {
        IsEmailVerified = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Verifies the farmer's mobile number
    /// </summary>
    public void VerifyMobile()
    {
        IsMobileVerified = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Updates the farmer's profile photo
    /// </summary>
    /// <param name="photoUrl">URL to the new profile photo</param>
    public void UpdatePhoto(string? photoUrl)
    {
        PhotoUrl = photoUrl;
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Updates the farmer's address information
    /// </summary>
    /// <param name="city">City name</param>
    /// <param name="province">Province/State</param>
    /// <param name="zipCode">Postal/Zip code</param>
    /// <param name="subRegion">Sub region or district</param>
    public void UpdateAddress(string? city = null, string? province = null, string? zipCode = null, string? subRegion = null)
    {
        if (city != null) City = city.Trim();
        if (province != null) Province = province.Trim();
        if (zipCode != null) ZipCode = zipCode.Trim();
        if (subRegion != null) SubRegion = subRegion.Trim();
        
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Validates the farmer entity before saving
    /// </summary>
    /// <returns>List of validation errors, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(FirstName))
            errors.Add("First name is required");

        if (string.IsNullOrWhiteSpace(Surname))
            errors.Add("Surname is required");

        if (string.IsNullOrWhiteSpace(Email))
            errors.Add("Email is required");
        else if (!new EmailAddressAttribute().IsValid(Email))
            errors.Add("Email format is invalid");

        if (string.IsNullOrWhiteSpace(MobileNumber))
            errors.Add("Mobile number is required");

        if (string.IsNullOrWhiteSpace(FirebaseUid))
            errors.Add("Firebase UID is required");

        return errors;
    }
}