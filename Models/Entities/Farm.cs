using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlockForge.Models.Entities;

/// <summary>
/// Represents a sheep farm with location, breed information, and production details
/// </summary>
[Table("Farms")]
public class Farm : BaseEntity
{
    private string _farmName = string.Empty;
    private string _breed = string.Empty;

    /// <summary>
    /// Reference to the farmer who owns this farm
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string FarmerId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the farm
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string FarmName
    {
        get => _farmName;
        set => _farmName = value?.Trim() ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Company or business name (optional)
    /// </summary>
    [MaxLength(200)]
    public string? CompanyName { get; set; }

    /// <summary>
    /// Primary sheep breed on this farm
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Breed
    {
        get => _breed;
        set => _breed = value?.Trim() ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Total number of production ewes on the farm
    /// </summary>
    [Range(1, 100000)]
    public int TotalProductionEwes { get; set; }

    /// <summary>
    /// Farm size in hectares
    /// </summary>
    [Range(0.1, 1000000)]
    public decimal Size { get; set; }

    /// <summary>
    /// GPS coordinates in decimal degrees format (latitude,longitude)
    /// Example: "-25.7479,28.2293"
    /// </summary>
    [MaxLength(50)]
    public string? GPSLocation { get; set; }

    /// <summary>
    /// Production system type (e.g., "Extensive", "Intensive", "Semi-intensive")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ProductionSystem { get; set; } = "Extensive";

    /// <summary>
    /// Farm's physical address
    /// </summary>
    [MaxLength(500)]
    public string? Address { get; set; }

    /// <summary>
    /// Postal code for the farm location
    /// </summary>
    [MaxLength(20)]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Province or state where the farm is located
    /// </summary>
    [MaxLength(100)]
    public string? Province { get; set; }

    /// <summary>
    /// Country where the farm is located
    /// </summary>
    [MaxLength(100)]
    public string Country { get; set; } = "South Africa";

    /// <summary>
    /// Cooperative or association membership
    /// </summary>
    [MaxLength(200)]
    public string? CoOp { get; set; }

    // Contact Information
    /// <summary>
    /// Primary veterinarian contact name
    /// </summary>
    [MaxLength(200)]
    public string? VeterinarianName { get; set; }

    /// <summary>
    /// Primary veterinarian phone number
    /// </summary>
    [MaxLength(20)]
    public string? VeterinarianPhone { get; set; }

    /// <summary>
    /// Livestock agent contact name
    /// </summary>
    [MaxLength(200)]
    public string? AgentName { get; set; }

    /// <summary>
    /// Livestock agent phone number
    /// </summary>
    [MaxLength(20)]
    public string? AgentPhone { get; set; }

    /// <summary>
    /// Abattoir contact name
    /// </summary>
    [MaxLength(200)]
    public string? AbattoirName { get; set; }

    /// <summary>
    /// Abattoir phone number
    /// </summary>
    [MaxLength(20)]
    public string? AbattoirPhone { get; set; }

    /// <summary>
    /// Feed supplier contact name
    /// </summary>
    [MaxLength(200)]
    public string? FeedSupplierName { get; set; }

    /// <summary>
    /// Feed supplier phone number
    /// </summary>
    [MaxLength(20)]
    public string? FeedSupplierPhone { get; set; }

    // Farm Settings
    /// <summary>
    /// Whether this farm is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Default gestation period in days for this farm's breed
    /// </summary>
    [Range(140, 160)]
    public int DefaultGestationDays { get; set; } = 150;

    /// <summary>
    /// Target weaning age in days
    /// </summary>
    [Range(60, 120)]
    public int TargetWeaningAge { get; set; } = 90;

    /// <summary>
    /// Farm-specific notes or comments
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Navigation properties
    /// <summary>
    /// The farmer who owns this farm
    /// </summary>
    public virtual Farmer? Farmer { get; set; }

    /// <summary>
    /// Lambing seasons/groups for this farm
    /// </summary>
    public virtual ICollection<LambingSeason> LambingSeasons { get; set; } = new List<LambingSeason>();

    // Computed properties
    /// <summary>
    /// Display name combining farm name and company name if available
    /// </summary>
    [NotMapped]
    public string DisplayName =>
        !string.IsNullOrWhiteSpace(CompanyName) ? $"{FarmName} ({CompanyName})" : FarmName;

    /// <summary>
    /// GPS coordinates as separate latitude and longitude values
    /// </summary>
    [NotMapped]
    public (double? Latitude, double? Longitude) GPSCoordinates
    {
        get
        {
            if (string.IsNullOrWhiteSpace(GPSLocation))
                return (null, null);

            var parts = GPSLocation.Split(',');
            if (parts.Length != 2)
                return (null, null);

            if (double.TryParse(parts[0].Trim(), out var lat) &&
                double.TryParse(parts[1].Trim(), out var lng))
            {
                return (lat, lng);
            }

            return (null, null);
        }
    }

    /// <summary>
    /// Whether the farm has valid GPS coordinates
    /// </summary>
    [NotMapped]
    public bool HasValidGPS => GPSCoordinates.Latitude.HasValue && GPSCoordinates.Longitude.HasValue;

    /// <summary>
    /// Estimated total farm capacity based on size and production system
    /// </summary>
    [NotMapped]
    public int EstimatedCapacity
    {
        get
        {
            var baseCapacity = ProductionSystem.ToLower() switch
            {
                "intensive" => (int)(Size * 15), // 15 ewes per hectare
                "semi-intensive" => (int)(Size * 8), // 8 ewes per hectare
                "extensive" => (int)(Size * 3), // 3 ewes per hectare
                _ => (int)(Size * 5) // Default 5 ewes per hectare
            };
            return Math.Max(baseCapacity, TotalProductionEwes);
        }
    }

    // Business methods
    /// <summary>
    /// Sets the GPS location from latitude and longitude coordinates
    /// </summary>
    /// <param name="latitude">Latitude in decimal degrees</param>
    /// <param name="longitude">Longitude in decimal degrees</param>
    public void SetGPSLocation(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90 degrees");

        if (longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180 degrees");

        GPSLocation = $"{latitude:F6},{longitude:F6}";
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Updates the farm's contact information
    /// </summary>
    public void UpdateContacts(
        string? veterinarianName = null, string? veterinarianPhone = null,
        string? agentName = null, string? agentPhone = null,
        string? abattoirName = null, string? abattoirPhone = null,
        string? feedSupplierName = null, string? feedSupplierPhone = null)
    {
        if (veterinarianName != null) VeterinarianName = veterinarianName.Trim();
        if (veterinarianPhone != null) VeterinarianPhone = veterinarianPhone.Trim();
        if (agentName != null) AgentName = agentName.Trim();
        if (agentPhone != null) AgentPhone = agentPhone.Trim();
        if (abattoirName != null) AbattoirName = abattoirName.Trim();
        if (abattoirPhone != null) AbattoirPhone = abattoirPhone.Trim();
        if (feedSupplierName != null) FeedSupplierName = feedSupplierName.Trim();
        if (feedSupplierPhone != null) FeedSupplierPhone = feedSupplierPhone.Trim();

        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Updates the farm's cooperative information
    /// </summary>
    /// <param name="coOp">Cooperative or association membership</param>
    public void UpdateCoOp(string? coOp)
    {
        if (coOp != null) CoOp = coOp.Trim();
        
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Activates or deactivates the farm
    /// </summary>
    /// <param name="active">Whether the farm should be active</param>
    public void SetActive(bool active)
    {
        IsActive = active;
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Updates the farm's production parameters
    /// </summary>
    /// <param name="gestationDays">Gestation period in days</param>
    /// <param name="weaningAge">Target weaning age in days</param>
    public void UpdateProductionSettings(int gestationDays, int weaningAge)
    {
        if (gestationDays < 140 || gestationDays > 160)
            throw new ArgumentOutOfRangeException(nameof(gestationDays), "Gestation days must be between 140 and 160");

        if (weaningAge < 60 || weaningAge > 120)
            throw new ArgumentOutOfRangeException(nameof(weaningAge), "Weaning age must be between 60 and 120 days");

        DefaultGestationDays = gestationDays;
        TargetWeaningAge = weaningAge;
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Validates the farm entity before saving
    /// </summary>
    /// <returns>List of validation errors, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(FarmName))
            errors.Add("Farm name is required");

        if (string.IsNullOrWhiteSpace(Breed))
            errors.Add("Breed is required");

        if (string.IsNullOrWhiteSpace(FarmerId))
            errors.Add("Farmer ID is required");

        if (TotalProductionEwes <= 0)
            errors.Add("Total production ewes must be greater than 0");

        if (Size <= 0)
            errors.Add("Farm size must be greater than 0");

        if (!string.IsNullOrWhiteSpace(GPSLocation))
        {
            var coords = GPSCoordinates;
            if (!coords.Latitude.HasValue || !coords.Longitude.HasValue)
                errors.Add("GPS location format is invalid (should be 'latitude,longitude')");
        }

        return errors;
    }
}