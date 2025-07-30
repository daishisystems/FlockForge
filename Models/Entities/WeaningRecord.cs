using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlockForge.Models.Entities;

/// <summary>
/// Represents a weaning record for a lambing season
/// </summary>
[Table("WeaningRecords")]
public class WeaningRecord : BaseEntity
{
    /// <summary>
    /// Reference to the lambing season this weaning record belongs to
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string LambingSeasonId { get; set; } = string.Empty;

    /// <summary>
    /// Date when weaning was performed
    /// </summary>
    [Required]
    public DateOnly WeaningDate { get; set; }

    /// <summary>
    /// Number of lambs weaned
    /// </summary>
    [Range(0, 30000)]
    public int LambsWeaned { get; set; }
    
    /// <summary>
    /// Number of rams weaned
    /// </summary>
    [Range(0, 10000)]
    public int RamsWeaned { get; set; }
    
    /// <summary>
    /// Number of breeding ewes that died after scanning
    /// </summary>
    [Range(0, 10000)]
    public int BreedingEwesDeadFromScan { get; set; }
    
    /// <summary>
    /// Other mortalities with category
    /// </summary>
    [Range(0, 10000)]
    public int OtherMortalities { get; set; }
    
    /// <summary>
    /// Category for other mortalities
    /// </summary>
    [MaxLength(100)]
    public string? OtherMortalityCategory { get; set; }

    /// <summary>
    /// Number of male lambs weaned
    /// </summary>
    [Range(0, 30000)]
    public int MaleLambsWeaned { get; set; }

    /// <summary>
    /// Number of female lambs weaned
    /// </summary>
    [Range(0, 30000)]
    public int FemaleLambsWeaned { get; set; }

    /// <summary>
    /// Total weight of all lambs weaned (in kg)
    /// </summary>
    [Range(0, 100000)]
    public decimal? TotalWeaningWeight { get; set; }

    /// <summary>
    /// Average weaning weight per lamb (in kg)
    /// </summary>
    [Range(0, 100)]
    public decimal? AverageWeaningWeight { get; set; }

    /// <summary>
    /// Average age at weaning (in days)
    /// </summary>
    [Range(30, 200)]
    public int? AverageWeaningAge { get; set; }

    /// <summary>
    /// Number of lambs that died between birth and weaning
    /// </summary>
    [Range(0, 30000)]
    public int LambMortality { get; set; }

    /// <summary>
    /// Number of ewes that died between lambing and weaning
    /// </summary>
    [Range(0, 10000)]
    public int EweMortality { get; set; }

    /// <summary>
    /// Number of lambs sold at weaning
    /// </summary>
    [Range(0, 30000)]
    public int LambsSold { get; set; }

    /// <summary>
    /// Number of lambs retained for breeding/replacement
    /// </summary>
    [Range(0, 30000)]
    public int LambsRetained { get; set; }

    /// <summary>
    /// Total income from lamb sales (in local currency)
    /// </summary>
    [Range(0, 10000000)]
    public decimal? SalesIncome { get; set; }

    /// <summary>
    /// Average price per kg for lambs sold
    /// </summary>
    [Range(0, 1000)]
    public decimal? AveragePricePerKg { get; set; }

    /// <summary>
    /// Cost associated with weaning (labor, equipment, etc.)
    /// </summary>
    [Range(0, 100000)]
    public decimal? WeaningCost { get; set; }

    /// <summary>
    /// Feed cost from birth to weaning
    /// </summary>
    [Range(0, 1000000)]
    public decimal? FeedCost { get; set; }

    /// <summary>
    /// Veterinary and health costs from birth to weaning
    /// </summary>
    [Range(0, 100000)]
    public decimal? HealthCost { get; set; }

    /// <summary>
    /// Body condition score of ewes at weaning (1-5 scale)
    /// </summary>
    [Range(1, 5)]
    public decimal? EweBodyConditionScore { get; set; }

    /// <summary>
    /// Weather conditions during weaning
    /// </summary>
    [MaxLength(100)]
    public string? WeatherConditions { get; set; }

    /// <summary>
    /// Notes about the weaning process or performance
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Navigation properties
    /// <summary>
    /// The lambing season this weaning record belongs to
    /// </summary>
    public virtual LambingSeason? LambingSeason { get; set; }

    // Computed properties
    /// <summary>
    /// Weaning percentage (lambs weaned per ewe mated)
    /// </summary>
    [NotMapped]
    public decimal WeaningPercentage
    {
        get
        {
            if (LambingSeason?.TargetEwes == null || LambingSeason.TargetEwes == 0) return 0;
            return Math.Round((decimal)LambsWeaned / LambingSeason.TargetEwes * 100, 1);
        }
    }

    /// <summary>
    /// Lamb survival rate from birth to weaning
    /// </summary>
    [NotMapped]
    public decimal LambSurvivalRate
    {
        get
        {
            var totalBorn = LambsWeaned + LambMortality;
            if (totalBorn == 0) return 0;
            return Math.Round((decimal)LambsWeaned / totalBorn * 100, 1);
        }
    }

    /// <summary>
    /// Lamb mortality rate from birth to weaning
    /// </summary>
    [NotMapped]
    public decimal LambMortalityRate
    {
        get
        {
            var totalBorn = LambsWeaned + LambMortality;
            if (totalBorn == 0) return 0;
            return Math.Round((decimal)LambMortality / totalBorn * 100, 1);
        }
    }

    /// <summary>
    /// Sex ratio of weaned lambs (percentage male)
    /// </summary>
    [NotMapped]
    public decimal WeanedMaleRatio
    {
        get
        {
            var totalSexed = MaleLambsWeaned + FemaleLambsWeaned;
            if (totalSexed == 0) return 0;
            return Math.Round((decimal)MaleLambsWeaned / totalSexed * 100, 1);
        }
    }

    /// <summary>
    /// Percentage of lambs sold vs retained
    /// </summary>
    [NotMapped]
    public decimal SalePercentage
    {
        get
        {
            var totalAccounted = LambsSold + LambsRetained;
            if (totalAccounted == 0) return 0;
            return Math.Round((decimal)LambsSold / totalAccounted * 100, 1);
        }
    }

    /// <summary>
    /// Average daily gain from birth to weaning (kg/day)
    /// </summary>
    [NotMapped]
    public decimal? AverageDailyGain
    {
        get
        {
            if (!AverageWeaningWeight.HasValue || !AverageWeaningAge.HasValue || AverageWeaningAge == 0)
                return null;

            // Assuming birth weight of 4kg (typical for sheep)
            const decimal assumedBirthWeight = 4.0m;
            var weightGain = AverageWeaningWeight.Value - assumedBirthWeight;
            return Math.Round(weightGain / AverageWeaningAge.Value, 3);
        }
    }

    /// <summary>
    /// Gross margin per lamb (income minus costs)
    /// </summary>
    [NotMapped]
    public decimal? GrossMarginPerLamb
    {
        get
        {
            if (!SalesIncome.HasValue || LambsSold == 0) return null;

            var totalCosts = (WeaningCost ?? 0) + (FeedCost ?? 0) + (HealthCost ?? 0);
            var incomePerLamb = SalesIncome.Value / LambsSold;
            var costPerLamb = LambsWeaned > 0 ? totalCosts / LambsWeaned : 0;

            return Math.Round(incomePerLamb - costPerLamb, 2);
        }
    }

    /// <summary>
    /// Total production costs
    /// </summary>
    [NotMapped]
    public decimal TotalCosts => (WeaningCost ?? 0) + (FeedCost ?? 0) + (HealthCost ?? 0);

    /// <summary>
    /// Return on investment percentage
    /// </summary>
    [NotMapped]
    public decimal? ReturnOnInvestment
    {
        get
        {
            if (!SalesIncome.HasValue || TotalCosts == 0) return null;
            var profit = SalesIncome.Value - TotalCosts;
            return Math.Round(profit / TotalCosts * 100, 1);
        }
    }

    // Business methods
    /// <summary>
    /// Updates the weaning results
    /// </summary>
    /// <param name="lambsWeaned">Number of lambs weaned</param>
    /// <param name="maleLambs">Number of male lambs weaned</param>
    /// <param name="femaleLambs">Number of female lambs weaned</param>
    /// <param name="lambMortality">Number of lambs that died</param>
    public void UpdateWeaningResults(int lambsWeaned, int maleLambs, int femaleLambs, int lambMortality)
    {
        if (lambsWeaned < 0 || maleLambs < 0 || femaleLambs < 0 || lambMortality < 0)
            throw new ArgumentException("All counts must be non-negative");

        if (maleLambs + femaleLambs > lambsWeaned)
            throw new ArgumentException("Total sexed lambs cannot exceed lambs weaned");

        LambsWeaned = lambsWeaned;
        MaleLambsWeaned = maleLambs;
        FemaleLambsWeaned = femaleLambs;
        LambMortality = lambMortality;

        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Updates the weight information
    /// </summary>
    /// <param name="totalWeight">Total weight of all lambs</param>
    /// <param name="averageWeight">Average weight per lamb</param>
    /// <param name="averageAge">Average age at weaning</param>
    public void UpdateWeightInfo(decimal? totalWeight, decimal? averageWeight, int? averageAge)
    {
        if (totalWeight.HasValue && totalWeight < 0)
            throw new ArgumentException("Total weight cannot be negative");

        if (averageWeight.HasValue && averageWeight < 0)
            throw new ArgumentException("Average weight cannot be negative");

        if (averageAge.HasValue && (averageAge < 30 || averageAge > 200))
            throw new ArgumentException("Average weaning age must be between 30 and 200 days");

        TotalWeaningWeight = totalWeight;
        AverageWeaningWeight = averageWeight;
        AverageWeaningAge = averageAge;

        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Updates the sales information
    /// </summary>
    /// <param name="lambsSold">Number of lambs sold</param>
    /// <param name="lambsRetained">Number of lambs retained</param>
    /// <param name="salesIncome">Total sales income</param>
    /// <param name="pricePerKg">Average price per kg</param>
    public void UpdateSalesInfo(int lambsSold, int lambsRetained, decimal? salesIncome, decimal? pricePerKg)
    {
        if (lambsSold < 0 || lambsRetained < 0)
            throw new ArgumentException("Lamb counts cannot be negative");

        if (lambsSold + lambsRetained > LambsWeaned)
            throw new ArgumentException("Total sold and retained cannot exceed lambs weaned");

        if (salesIncome.HasValue && salesIncome < 0)
            throw new ArgumentException("Sales income cannot be negative");

        if (pricePerKg.HasValue && pricePerKg < 0)
            throw new ArgumentException("Price per kg cannot be negative");

        LambsSold = lambsSold;
        LambsRetained = lambsRetained;
        SalesIncome = salesIncome;
        AveragePricePerKg = pricePerKg;

        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Updates the cost information
    /// </summary>
    /// <param name="weaningCost">Direct weaning costs</param>
    /// <param name="feedCost">Feed costs from birth to weaning</param>
    /// <param name="healthCost">Health and veterinary costs</param>
    public void UpdateCosts(decimal? weaningCost, decimal? feedCost, decimal? healthCost)
    {
        if (weaningCost.HasValue && weaningCost < 0)
            throw new ArgumentException("Weaning cost cannot be negative");

        if (feedCost.HasValue && feedCost < 0)
            throw new ArgumentException("Feed cost cannot be negative");

        if (healthCost.HasValue && healthCost < 0)
            throw new ArgumentException("Health cost cannot be negative");

        WeaningCost = weaningCost;
        FeedCost = feedCost;
        HealthCost = healthCost;

        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Validates the weaning record before saving
    /// </summary>
    /// <returns>List of validation errors, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(LambingSeasonId))
            errors.Add("Lambing season ID is required");

        if (LambsWeaned < 0)
            errors.Add("Number of lambs weaned cannot be negative");

        if (MaleLambsWeaned + FemaleLambsWeaned > LambsWeaned)
            errors.Add("Total sexed lambs cannot exceed lambs weaned");

        if (LambsSold + LambsRetained > LambsWeaned)
            errors.Add("Total sold and retained lambs cannot exceed lambs weaned");

        if (TotalWeaningWeight.HasValue && TotalWeaningWeight < 0)
            errors.Add("Total weaning weight cannot be negative");

        if (AverageWeaningWeight.HasValue && AverageWeaningWeight < 0)
            errors.Add("Average weaning weight cannot be negative");

        if (AverageWeaningAge.HasValue && (AverageWeaningAge < 30 || AverageWeaningAge > 200))
            errors.Add("Average weaning age must be between 30 and 200 days");

        if (EweBodyConditionScore.HasValue && (EweBodyConditionScore < 1 || EweBodyConditionScore > 5))
            errors.Add("Ewe body condition score must be between 1 and 5");

        if (SalesIncome.HasValue && SalesIncome < 0)
            errors.Add("Sales income cannot be negative");

        if (AveragePricePerKg.HasValue && AveragePricePerKg < 0)
            errors.Add("Average price per kg cannot be negative");

        // Validate that costs are non-negative
        if (WeaningCost.HasValue && WeaningCost < 0)
            errors.Add("Weaning cost cannot be negative");

        if (FeedCost.HasValue && FeedCost < 0)
            errors.Add("Feed cost cannot be negative");

        if (HealthCost.HasValue && HealthCost < 0)
            errors.Add("Health cost cannot be negative");

        return errors;
    }
}