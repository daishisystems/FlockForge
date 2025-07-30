using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlockForge.Models.Entities;

/// <summary>
/// Represents a lambing season/group with mating and lambing date ranges
/// </summary>
[Table("LambingSeasons")]
public class LambingSeason : BaseEntity
{
    private string _code = string.Empty;
    private string _groupName = string.Empty;

    /// <summary>
    /// Reference to the farm this lambing season belongs to
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string FarmId { get; set; } = string.Empty;

    /// <summary>
    /// Unique code for this lambing season (e.g., "2024-A", "Spring2024")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Code
    {
        get => _code;
        set => _code = value?.Trim().ToUpper() ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Descriptive name for the group (e.g., "Spring Lambing 2024", "Main Group")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string GroupName
    {
        get => _groupName;
        set => _groupName = value?.Trim() ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Start date of the mating period
    /// </summary>
    [Required]
    public DateOnly MatingStart { get; set; }

    /// <summary>
    /// End date of the mating period
    /// </summary>
    [Required]
    public DateOnly MatingEnd { get; set; }

    /// <summary>
    /// Expected start date of lambing (calculated from mating start + gestation)
    /// </summary>
    [Required]
    public DateOnly LambingStart { get; set; }

    /// <summary>
    /// Expected end date of lambing (calculated from mating end + gestation)
    /// </summary>
    [Required]
    public DateOnly LambingEnd { get; set; }

    /// <summary>
    /// Whether this is the currently active lambing season
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// Target number of ewes to be mated in this season
    /// </summary>
    [Range(1, 10000)]
    public int TargetEwes { get; set; }

    /// <summary>
    /// Expected conception rate as a percentage (0-100)
    /// </summary>
    [Range(0, 100)]
    public decimal ExpectedConceptionRate { get; set; } = 85m;

    /// <summary>
    /// Expected lambing percentage (lambs per ewe mated)
    /// </summary>
    [Range(0, 300)]
    public decimal ExpectedLambingPercentage { get; set; } = 120m;

    /// <summary>
    /// Gestation period in days for this season
    /// </summary>
    [Range(140, 160)]
    public int GestationDays { get; set; } = 150;

    /// <summary>
    /// Season-specific notes or comments
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Current status of the lambing season
    /// </summary>
    public LambingSeasonStatus Status { get; set; } = LambingSeasonStatus.Planning;

    // Navigation properties
    /// <summary>
    /// The farm this lambing season belongs to
    /// </summary>
    public virtual Farm? Farm { get; set; }

    /// <summary>
    /// Breeding records for this lambing season
    /// </summary>
    public virtual ICollection<BreedingRecord> BreedingRecords { get; set; } = new List<BreedingRecord>();

    /// <summary>
    /// Scanning records for this lambing season
    /// </summary>
    public virtual ICollection<ScanningRecord> ScanningRecords { get; set; } = new List<ScanningRecord>();

    /// <summary>
    /// Lambing records for this lambing season
    /// </summary>
    public virtual ICollection<LambingRecord> LambingRecords { get; set; } = new List<LambingRecord>();

    /// <summary>
    /// Weaning records for this lambing season
    /// </summary>
    public virtual ICollection<WeaningRecord> WeaningRecords { get; set; } = new List<WeaningRecord>();

    // Computed properties
    /// <summary>
    /// Duration of the mating period in days
    /// </summary>
    [NotMapped]
    public int MatingDuration => MatingEnd.DayNumber - MatingStart.DayNumber + 1;

    /// <summary>
    /// Duration of the lambing period in days
    /// </summary>
    [NotMapped]
    public int LambingDuration => LambingEnd.DayNumber - LambingStart.DayNumber + 1;

    /// <summary>
    /// Whether the mating period is currently active
    /// </summary>
    [NotMapped]
    public bool IsMatingActive
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return today >= MatingStart && today <= MatingEnd;
        }
    }

    /// <summary>
    /// Whether the lambing period is currently active
    /// </summary>
    [NotMapped]
    public bool IsLambingActive
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return today >= LambingStart && today <= LambingEnd;
        }
    }

    /// <summary>
    /// Days until mating starts (negative if already started)
    /// </summary>
    [NotMapped]
    public int DaysToMating
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return MatingStart.DayNumber - today.DayNumber;
        }
    }

    /// <summary>
    /// Days until lambing starts (negative if already started)
    /// </summary>
    [NotMapped]
    public int DaysToLambing
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return LambingStart.DayNumber - today.DayNumber;
        }
    }

    /// <summary>
    /// Expected number of pregnant ewes based on target ewes and conception rate
    /// </summary>
    [NotMapped]
    public int ExpectedPregnantEwes => (int)(TargetEwes * ExpectedConceptionRate / 100m);

    /// <summary>
    /// Expected number of lambs based on target ewes and lambing percentage
    /// </summary>
    [NotMapped]
    public int ExpectedLambs => (int)(TargetEwes * ExpectedLambingPercentage / 100m);

    /// <summary>
    /// Display name combining code and group name
    /// </summary>
    [NotMapped]
    public string DisplayName => $"{Code} - {GroupName}";

    // Business methods
    /// <summary>
    /// Calculates lambing dates based on mating dates and gestation period
    /// </summary>
    public void CalculateLambingDates()
    {
        LambingStart = MatingStart.AddDays(GestationDays);
        LambingEnd = MatingEnd.AddDays(GestationDays);
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Sets this lambing season as active and deactivates others
    /// </summary>
    public void SetActive()
    {
        Active = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Deactivates this lambing season
    /// </summary>
    public void SetInactive()
    {
        Active = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Updates the status of the lambing season
    /// </summary>
    /// <param name="newStatus">The new status</param>
    public void UpdateStatus(LambingSeasonStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Updates the mating period dates and recalculates lambing dates
    /// </summary>
    /// <param name="matingStart">New mating start date</param>
    /// <param name="matingEnd">New mating end date</param>
    public void UpdateMatingPeriod(DateOnly matingStart, DateOnly matingEnd)
    {
        if (matingEnd < matingStart)
            throw new ArgumentException("Mating end date cannot be before start date");

        MatingStart = matingStart;
        MatingEnd = matingEnd;
        CalculateLambingDates();
    }

    /// <summary>
    /// Updates the expected performance parameters
    /// </summary>
    /// <param name="conceptionRate">Expected conception rate (0-100%)</param>
    /// <param name="lambingPercentage">Expected lambing percentage (0-300%)</param>
    public void UpdateExpectedPerformance(decimal conceptionRate, decimal lambingPercentage)
    {
        if (conceptionRate < 0 || conceptionRate > 100)
            throw new ArgumentOutOfRangeException(nameof(conceptionRate), "Conception rate must be between 0 and 100");

        if (lambingPercentage < 0 || lambingPercentage > 300)
            throw new ArgumentOutOfRangeException(nameof(lambingPercentage), "Lambing percentage must be between 0 and 300");

        ExpectedConceptionRate = conceptionRate;
        ExpectedLambingPercentage = lambingPercentage;
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Validates the lambing season entity before saving
    /// </summary>
    /// <returns>List of validation errors, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Code))
            errors.Add("Code is required");

        if (string.IsNullOrWhiteSpace(GroupName))
            errors.Add("Group name is required");

        if (string.IsNullOrWhiteSpace(FarmId))
            errors.Add("Farm ID is required");

        if (MatingEnd < MatingStart)
            errors.Add("Mating end date cannot be before start date");

        if (LambingEnd < LambingStart)
            errors.Add("Lambing end date cannot be before start date");

        if (TargetEwes <= 0)
            errors.Add("Target ewes must be greater than 0");

        if (ExpectedConceptionRate < 0 || ExpectedConceptionRate > 100)
            errors.Add("Expected conception rate must be between 0 and 100");

        if (ExpectedLambingPercentage < 0 || ExpectedLambingPercentage > 300)
            errors.Add("Expected lambing percentage must be between 0 and 300");

        if (GestationDays < 140 || GestationDays > 160)
            errors.Add("Gestation days must be between 140 and 160");

        return errors;
    }
}

/// <summary>
/// Status of a lambing season
/// </summary>
public enum LambingSeasonStatus
{
    /// <summary>
    /// Season is being planned
    /// </summary>
    Planning = 0,

    /// <summary>
    /// Mating period is active
    /// </summary>
    Mating = 1,

    /// <summary>
    /// Waiting for scanning/pregnancy testing
    /// </summary>
    WaitingForScanning = 2,

    /// <summary>
    /// Scanning/pregnancy testing completed
    /// </summary>
    Scanned = 3,

    /// <summary>
    /// Lambing period is active
    /// </summary>
    Lambing = 4,

    /// <summary>
    /// Waiting for weaning
    /// </summary>
    WaitingForWeaning = 5,

    /// <summary>
    /// Season completed (weaned)
    /// </summary>
    Completed = 6,

    /// <summary>
    /// Season was cancelled
    /// </summary>
    Cancelled = 7
}