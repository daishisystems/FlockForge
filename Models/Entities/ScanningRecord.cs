using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlockForge.Models.Entities;

/// <summary>
/// Represents a scanning/pregnancy testing record for a lambing season
/// </summary>
[Table("ScanningRecords")]
public class ScanningRecord : BaseEntity
{
    /// <summary>
    /// Reference to the lambing season this scanning record belongs to
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string LambingSeasonId { get; set; } = string.Empty;

    /// <summary>
    /// Date when scanning was performed
    /// </summary>
    [Required]
    public DateOnly ScanDate { get; set; }

    /// <summary>
    /// Number of ewes scanned
    /// </summary>
    [Range(1, 10000)]
    public int EwesScanned { get; set; }

    /// <summary>
    /// Number of ewes mated (from breeding records)
    /// </summary>
    [Range(0, 10000)]
    public int? EwesMated { get; set; }

    /// <summary>
    /// Number of ewes found to be pregnant
    /// </summary>
    [Range(0, 10000)]
    public int EwesPregnant { get; set; }

    /// <summary>
    /// Number of ewes carrying singles
    /// </summary>
    [Range(0, 10000)]
    public int EwesSingles { get; set; }

    /// <summary>
    /// Number of ewes carrying twins
    /// </summary>
    [Range(0, 10000)]
    public int EwesTwins { get; set; }

    /// <summary>
    /// Number of ewes carrying triplets or more
    /// </summary>
    [Range(0, 10000)]
    public int EwesMultiples { get; set; }

    /// <summary>
    /// Number of empty/non-pregnant ewes
    /// </summary>
    [Range(0, 10000)]
    public int EwesEmpty { get; set; }

    /// <summary>
    /// Total number of fetuses scanned
    /// </summary>
    [Range(0, 30000)]
    public int? ScannedFetuses { get; set; }

    /// <summary>
    /// Method used for scanning
    /// </summary>
    [Required]
    public ScanningMethod Method { get; set; }

    /// <summary>
    /// Name of the person/company who performed the scanning
    /// </summary>
    [MaxLength(200)]
    public string? ScannerName { get; set; }

    /// <summary>
    /// Cost of the scanning service
    /// </summary>
    [Range(0, 100000)]
    public decimal? Cost { get; set; }

    /// <summary>
    /// Notes about the scanning process or results
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Navigation properties
    /// <summary>
    /// The lambing season this scanning record belongs to
    /// </summary>
    public virtual LambingSeason? LambingSeason { get; set; }

    // Computed properties
    /// <summary>
    /// Conception rate as a percentage
    /// </summary>
    [NotMapped]
    public decimal ConceptionRate
    {
        get
        {
            if (EwesScanned == 0) return 0;
            return Math.Round((decimal)EwesPregnant / EwesScanned * 100, 1);
        }
    }

    /// <summary>
    /// Expected lambing percentage based on scanning results
    /// </summary>
    [NotMapped]
    public decimal ExpectedLambingPercentage
    {
        get
        {
            if (EwesScanned == 0) return 0;
            var totalLambs = EwesSingles + (EwesTwins * 2) + (EwesMultiples * 3); // Assuming 3 for multiples
            return Math.Round((decimal)totalLambs / EwesScanned * 100, 1);
        }
    }

    /// <summary>
    /// Total expected lambs from scanning
    /// </summary>
    [NotMapped]
    public int ExpectedLambs => EwesSingles + (EwesTwins * 2) + (EwesMultiples * 3);

    /// <summary>
    /// Average lambs per pregnant ewe
    /// </summary>
    [NotMapped]
    public decimal AverageLambsPerPregnantEwe
    {
        get
        {
            if (EwesPregnant == 0) return 0;
            return Math.Round((decimal)ExpectedLambs / EwesPregnant, 2);
        }
    }

    /// <summary>
    /// Percentage of pregnant ewes carrying multiples
    /// </summary>
    [NotMapped]
    public decimal MultipleRate
    {
        get
        {
            if (EwesPregnant == 0) return 0;
            return Math.Round((decimal)(EwesTwins + EwesMultiples) / EwesPregnant * 100, 1);
        }
    }

    /// <summary>
    /// Days from scanning to expected lambing start
    /// </summary>
    [NotMapped]
    public int? DaysToLambing
    {
        get
        {
            if (LambingSeason?.LambingStart == null) return null;
            return LambingSeason.LambingStart.DayNumber - ScanDate.DayNumber;
        }
    }

    /// <summary>
    /// Expected lambing percentage of ewes pregnant
    /// </summary>
    [NotMapped]
    public decimal ExpectedLambingPercentageOfPregnant
    {
        get
        {
            if (EwesPregnant == 0) return 0;
            return Math.Round((decimal)ExpectedLambs / EwesPregnant * 100, 1);
        }
    }

    /// <summary>
    /// Expected lambing percentage of ewes mated
    /// </summary>
    [NotMapped]
    public decimal ExpectedLambingPercentageOfMated
    {
        get
        {
            if (!EwesMated.HasValue || EwesMated == 0) return 0;
            return Math.Round((decimal)ExpectedLambs / EwesMated.Value * 100, 1);
        }
    }

    // Business methods
    /// <summary>
    /// Updates the scanning results
    /// </summary>
    /// <param name="singles">Number of ewes carrying singles</param>
    /// <param name="twins">Number of ewes carrying twins</param>
    /// <param name="multiples">Number of ewes carrying multiples</param>
    /// <param name="empty">Number of empty ewes</param>
    public void UpdateResults(int singles, int twins, int multiples, int empty)
    {
        if (singles < 0 || twins < 0 || multiples < 0 || empty < 0)
            throw new ArgumentException("All counts must be non-negative");

        EwesSingles = singles;
        EwesTwins = twins;
        EwesMultiples = multiples;
        EwesEmpty = empty;
        
        // Calculate derived values
        EwesPregnant = singles + twins + multiples;
        
        // Validate that totals match
        if (EwesPregnant + EwesEmpty != EwesScanned)
        {
            throw new InvalidOperationException("Total pregnant and empty ewes must equal ewes scanned");
        }

        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Updates the scanning service details
    /// </summary>
    /// <param name="scannerName">Name of scanner</param>
    /// <param name="cost">Cost of service</param>
    /// <param name="method">Scanning method used</param>
    public void UpdateServiceDetails(string? scannerName, decimal? cost, ScanningMethod? method = null)
    {
        if (scannerName != null) ScannerName = scannerName.Trim();
        if (cost.HasValue && cost >= 0) Cost = cost;
        if (method.HasValue) Method = method.Value;

        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Updates the ewes mated count
    /// </summary>
    /// <param name="ewesMated">Number of ewes mated</param>
    public void UpdateEwesMated(int? ewesMated)
    {
        EwesMated = ewesMated;
        
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Updates the scanned fetuses count
    /// </summary>
    /// <param name="scannedFetuses">Total number of fetuses scanned</param>
    public void UpdateScannedFetuses(int? scannedFetuses)
    {
        ScannedFetuses = scannedFetuses;
        
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Validates the scanning record before saving
    /// </summary>
    /// <returns>List of validation errors, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(LambingSeasonId))
            errors.Add("Lambing season ID is required");

        if (EwesScanned <= 0)
            errors.Add("Number of ewes scanned must be greater than 0");

        if (EwesSingles < 0)
            errors.Add("Number of ewes with singles cannot be negative");

        if (EwesTwins < 0)
            errors.Add("Number of ewes with twins cannot be negative");

        if (EwesMultiples < 0)
            errors.Add("Number of ewes with multiples cannot be negative");

        if (EwesEmpty < 0)
            errors.Add("Number of empty ewes cannot be negative");

        // Check that totals add up
        var totalCounted = EwesSingles + EwesTwins + EwesMultiples + EwesEmpty;
        if (totalCounted != EwesScanned)
            errors.Add($"Total counted ewes ({totalCounted}) must equal ewes scanned ({EwesScanned})");

        // Check that pregnant count matches
        var pregnantCount = EwesSingles + EwesTwins + EwesMultiples;
        if (pregnantCount != EwesPregnant)
            errors.Add($"Pregnant ewes count ({EwesPregnant}) must equal sum of singles, twins, and multiples ({pregnantCount})");

        if (ScannedFetuses.HasValue && ScannedFetuses < 0)
            errors.Add("Scanned fetuses count cannot be negative");

        if (Cost.HasValue && Cost < 0)
            errors.Add("Cost cannot be negative");

        return errors;
    }
}

/// <summary>
/// Method used for pregnancy scanning
/// </summary>
public enum ScanningMethod
{
    /// <summary>
    /// Ultrasound scanning
    /// </summary>
    Ultrasound = 0,

    /// <summary>
    /// Blood test for pregnancy
    /// </summary>
    BloodTest = 1,

    /// <summary>
    /// Physical examination
    /// </summary>
    PhysicalExam = 2,

    /// <summary>
    /// Ram harness/crayon marking
    /// </summary>
    RamMarking = 3,

    /// <summary>
    /// Other method
    /// </summary>
    Other = 4
}