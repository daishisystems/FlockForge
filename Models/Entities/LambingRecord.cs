using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlockForge.Models.Entities;

/// <summary>
/// Represents a lambing record for a lambing season
/// </summary>
[Table("LambingRecords")]
public class LambingRecord : BaseEntity
{
    /// <summary>
    /// Reference to the lambing season this lambing record belongs to
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string LambingSeasonId { get; set; } = string.Empty;

    /// <summary>
    /// Date when lambing was recorded
    /// </summary>
    [Required]
    public DateOnly LambingDate { get; set; }

    /// <summary>
    /// Number of ewes that lambed
    /// </summary>
    [Range(1, 10000)]
    public int EwesLambed { get; set; }

    /// <summary>
    /// Total number of lambs born (including dead)
    /// </summary>
    [Range(0, 30000)]
    public int TotalLambsBorn { get; set; }

    /// <summary>
    /// Number of lambs born alive
    /// </summary>
    [Range(0, 30000)]
    public int LambsBornAlive { get; set; }

    /// <summary>
    /// Number of lambs born dead
    /// </summary>
    [Range(0, 30000)]
    public int LambsBornDead { get; set; }

    /// <summary>
    /// Number of male lambs born
    /// </summary>
    [Range(0, 30000)]
    public int MaleLambs { get; set; }

    /// <summary>
    /// Number of female lambs born
    /// </summary>
    [Range(0, 30000)]
    public int FemaleLambs { get; set; }

    /// <summary>
    /// Number of ewes that had singles
    /// </summary>
    [Range(0, 10000)]
    public int EwesSingles { get; set; }

    /// <summary>
    /// Number of ewes that had twins
    /// </summary>
    [Range(0, 10000)]
    public int EwesTwins { get; set; }

    /// <summary>
    /// Number of ewes that had triplets or more
    /// </summary>
    [Range(0, 10000)]
    public int EwesMultiples { get; set; }

    /// <summary>
    /// Number of ewes that had difficult births requiring assistance
    /// </summary>
    [Range(0, 10000)]
    public int EwesAssisted { get; set; }

    /// <summary>
    /// Number of ewes that died during lambing
    /// </summary>
    [Range(0, 10000)]
    public int EwesMortality { get; set; }

    /// <summary>
    /// Average birth weight of lambs (in kg)
    /// </summary>
    [Range(0, 10)]
    public decimal? AverageBirthWeight { get; set; }

    /// <summary>
    /// Weather conditions during lambing
    /// </summary>
    [MaxLength(100)]
    public string? WeatherConditions { get; set; }

    /// <summary>
    /// Notes about the lambing process or issues
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Cost associated with lambing (veterinary, assistance, etc.)
    /// </summary>
    [Range(0, 100000)]
    public decimal? Cost { get; set; }

    // Navigation properties
    /// <summary>
    /// The lambing season this lambing record belongs to
    /// </summary>
    public virtual LambingSeason? LambingSeason { get; set; }

    // Computed properties
    /// <summary>
    /// Lambing percentage (lambs born per ewe lambed)
    /// </summary>
    [NotMapped]
    public decimal LambingPercentage
    {
        get
        {
            if (EwesLambed == 0) return 0;
            return Math.Round((decimal)TotalLambsBorn / EwesLambed * 100, 1);
        }
    }

    /// <summary>
    /// Survival rate of lambs born
    /// </summary>
    [NotMapped]
    public decimal SurvivalRate
    {
        get
        {
            if (TotalLambsBorn == 0) return 0;
            return Math.Round((decimal)LambsBornAlive / TotalLambsBorn * 100, 1);
        }
    }

    /// <summary>
    /// Mortality rate of lambs born
    /// </summary>
    [NotMapped]
    public decimal MortalityRate
    {
        get
        {
            if (TotalLambsBorn == 0) return 0;
            return Math.Round((decimal)LambsBornDead / TotalLambsBorn * 100, 1);
        }
    }

    /// <summary>
    /// Percentage of ewes requiring assistance
    /// </summary>
    [NotMapped]
    public decimal AssistedBirthRate
    {
        get
        {
            if (EwesLambed == 0) return 0;
            return Math.Round((decimal)EwesAssisted / EwesLambed * 100, 1);
        }
    }

    /// <summary>
    /// Ewe mortality rate during lambing
    /// </summary>
    [NotMapped]
    public decimal EweMortalityRate
    {
        get
        {
            if (EwesLambed == 0) return 0;
            return Math.Round((decimal)EwesMortality / EwesLambed * 100, 1);
        }
    }

    /// <summary>
    /// Sex ratio (percentage of male lambs)
    /// </summary>
    [NotMapped]
    public decimal MaleRatio
    {
        get
        {
            var totalSexed = MaleLambs + FemaleLambs;
            if (totalSexed == 0) return 0;
            return Math.Round((decimal)MaleLambs / totalSexed * 100, 1);
        }
    }

    /// <summary>
    /// Multiple birth rate (percentage of ewes with twins or more)
    /// </summary>
    [NotMapped]
    public decimal MultipleBirthRate
    {
        get
        {
            if (EwesLambed == 0) return 0;
            return Math.Round((decimal)(EwesTwins + EwesMultiples) / EwesLambed * 100, 1);
        }
    }

    /// <summary>
    /// Average lambs per ewe lambed
    /// </summary>
    [NotMapped]
    public decimal AverageLambsPerEwe
    {
        get
        {
            if (EwesLambed == 0) return 0;
            return Math.Round((decimal)TotalLambsBorn / EwesLambed, 2);
        }
    }

    // Business methods
    /// <summary>
    /// Updates the lambing results
    /// </summary>
    /// <param name="ewesLambed">Number of ewes that lambed</param>
    /// <param name="lambsBornAlive">Number of lambs born alive</param>
    /// <param name="lambsBornDead">Number of lambs born dead</param>
    /// <param name="singles">Number of ewes with singles</param>
    /// <param name="twins">Number of ewes with twins</param>
    /// <param name="multiples">Number of ewes with multiples</param>
    public void UpdateResults(int ewesLambed, int lambsBornAlive, int lambsBornDead, 
        int singles, int twins, int multiples)
    {
        if (ewesLambed < 0 || lambsBornAlive < 0 || lambsBornDead < 0 || 
            singles < 0 || twins < 0 || multiples < 0)
            throw new ArgumentException("All counts must be non-negative");

        EwesLambed = ewesLambed;
        LambsBornAlive = lambsBornAlive;
        LambsBornDead = lambsBornDead;
        TotalLambsBorn = lambsBornAlive + lambsBornDead;
        EwesSingles = singles;
        EwesTwins = twins;
        EwesMultiples = multiples;

        // Validate that birth type totals match ewes lambed
        if (singles + twins + multiples != ewesLambed)
        {
            throw new InvalidOperationException("Total of singles, twins, and multiples must equal ewes lambed");
        }

        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Updates the sex distribution of lambs
    /// </summary>
    /// <param name="males">Number of male lambs</param>
    /// <param name="females">Number of female lambs</param>
    public void UpdateSexDistribution(int males, int females)
    {
        if (males < 0 || females < 0)
            throw new ArgumentException("Sex counts must be non-negative");

        if (males + females > TotalLambsBorn)
            throw new ArgumentException("Total sexed lambs cannot exceed total lambs born");

        MaleLambs = males;
        FemaleLambs = females;

        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Updates mortality and assistance information
    /// </summary>
    /// <param name="ewesAssisted">Number of ewes requiring assistance</param>
    /// <param name="ewesMortality">Number of ewes that died</param>
    public void UpdateMortalityAndAssistance(int ewesAssisted, int ewesMortality)
    {
        if (ewesAssisted < 0 || ewesMortality < 0)
            throw new ArgumentException("Counts must be non-negative");

        if (ewesAssisted > EwesLambed)
            throw new ArgumentException("Assisted ewes cannot exceed ewes lambed");

        if (ewesMortality > EwesLambed)
            throw new ArgumentException("Ewe mortality cannot exceed ewes lambed");

        EwesAssisted = ewesAssisted;
        EwesMortality = ewesMortality;

        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Validates the lambing record before saving
    /// </summary>
    /// <returns>List of validation errors, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(LambingSeasonId))
            errors.Add("Lambing season ID is required");

        if (EwesLambed <= 0)
            errors.Add("Number of ewes lambed must be greater than 0");

        if (TotalLambsBorn != LambsBornAlive + LambsBornDead)
            errors.Add("Total lambs born must equal alive plus dead lambs");

        if (EwesSingles + EwesTwins + EwesMultiples != EwesLambed)
            errors.Add("Sum of singles, twins, and multiples must equal ewes lambed");

        if (MaleLambs + FemaleLambs > TotalLambsBorn)
            errors.Add("Total sexed lambs cannot exceed total lambs born");

        if (EwesAssisted > EwesLambed)
            errors.Add("Assisted ewes cannot exceed ewes lambed");

        if (EwesMortality > EwesLambed)
            errors.Add("Ewe mortality cannot exceed ewes lambed");

        if (AverageBirthWeight.HasValue && (AverageBirthWeight <= 0 || AverageBirthWeight > 10))
            errors.Add("Average birth weight must be between 0 and 10 kg");

        if (Cost.HasValue && Cost < 0)
            errors.Add("Cost cannot be negative");

        return errors;
    }
}