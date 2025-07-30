using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlockForge.Models.Entities;

/// <summary>
/// Represents a breeding/mating record for a lambing season
/// </summary>
[Table("BreedingRecords")]
public class BreedingRecord : BaseEntity
{
    /// <summary>
    /// Reference to the lambing season this breeding record belongs to
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string LambingSeasonId { get; set; } = string.Empty;

    /// <summary>
    /// Type of mating used
    /// </summary>
    [Required]
    public MatingType MatingType { get; set; }

    /// <summary>
    /// Number of ewes mated
    /// </summary>
    [Range(1, 10000)]
    public int EwesMated { get; set; }

    /// <summary>
    /// Date of artificial insemination (if applicable)
    /// </summary>
    public DateOnly? AIDate { get; set; }

    /// <summary>
    /// Start date of natural mating period (if applicable)
    /// </summary>
    public DateOnly? NaturalMatingStart { get; set; }

    /// <summary>
    /// End date of natural mating period (if applicable)
    /// </summary>
    public DateOnly? NaturalMatingEnd { get; set; }

    /// <summary>
    /// Ram/sire identification used for mating
    /// </summary>
    [MaxLength(100)]
    public string? RamId { get; set; }

    /// <summary>
    /// Breed of the ram used
    /// </summary>
    [MaxLength(100)]
    public string? RamBreed { get; set; }

    /// <summary>
    /// Notes about the breeding process
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Cost of breeding (feed, AI, ram hire, etc.)
    /// </summary>
    [Range(0, 1000000)]
    public decimal? Cost { get; set; }

    // Navigation properties
    /// <summary>
    /// The lambing season this breeding record belongs to
    /// </summary>
    public virtual LambingSeason? LambingSeason { get; set; }

    // Computed properties
    /// <summary>
    /// Duration of natural mating period in days
    /// </summary>
    [NotMapped]
    public int? NaturalMatingDuration
    {
        get
        {
            if (NaturalMatingStart.HasValue && NaturalMatingEnd.HasValue)
                return NaturalMatingEnd.Value.DayNumber - NaturalMatingStart.Value.DayNumber + 1;
            return null;
        }
    }

    /// <summary>
    /// Primary mating date for calculations
    /// </summary>
    [NotMapped]
    public DateOnly? PrimaryMatingDate
    {
        get
        {
            return MatingType switch
            {
                MatingType.ArtificialInsemination => AIDate,
                MatingType.NaturalMating => NaturalMatingStart,
                MatingType.Mixed => AIDate ?? NaturalMatingStart,
                _ => null
            };
        }
    }

    /// <summary>
    /// Expected lambing date based on primary mating date and gestation period
    /// </summary>
    [NotMapped]
    public DateOnly? ExpectedLambingDate
    {
        get
        {
            if (PrimaryMatingDate.HasValue && LambingSeason != null)
                return PrimaryMatingDate.Value.AddDays(LambingSeason.GestationDays);
            return null;
        }
    }

    // Business methods
    /// <summary>
    /// Updates the artificial insemination details
    /// </summary>
    /// <param name="aiDate">Date of AI</param>
    /// <param name="ramId">Ram identification</param>
    /// <param name="ramBreed">Ram breed</param>
    public void UpdateAIDetails(DateOnly aiDate, string? ramId = null, string? ramBreed = null)
    {
        AIDate = aiDate;
        if (ramId != null) RamId = ramId.Trim();
        if (ramBreed != null) RamBreed = ramBreed.Trim();
        
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Updates the natural mating details
    /// </summary>
    /// <param name="startDate">Start date of natural mating</param>
    /// <param name="endDate">End date of natural mating</param>
    /// <param name="ramId">Ram identification</param>
    /// <param name="ramBreed">Ram breed</param>
    public void UpdateNaturalMatingDetails(DateOnly startDate, DateOnly endDate, string? ramId = null, string? ramBreed = null)
    {
        if (endDate < startDate)
            throw new ArgumentException("End date cannot be before start date");

        NaturalMatingStart = startDate;
        NaturalMatingEnd = endDate;
        if (ramId != null) RamId = ramId.Trim();
        if (ramBreed != null) RamBreed = ramBreed.Trim();
        
        UpdatedAt = DateTimeOffset.UtcNow;
        IsSynced = false;
    }

    /// <summary>
    /// Validates the breeding record before saving
    /// </summary>
    /// <returns>List of validation errors, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(LambingSeasonId))
            errors.Add("Lambing season ID is required");

        if (EwesMated <= 0)
            errors.Add("Number of ewes mated must be greater than 0");

        switch (MatingType)
        {
            case MatingType.ArtificialInsemination:
                if (!AIDate.HasValue)
                    errors.Add("AI date is required for artificial insemination");
                break;

            case MatingType.NaturalMating:
                if (!NaturalMatingStart.HasValue || !NaturalMatingEnd.HasValue)
                    errors.Add("Natural mating start and end dates are required for natural mating");
                else if (NaturalMatingEnd < NaturalMatingStart)
                    errors.Add("Natural mating end date cannot be before start date");
                break;

            case MatingType.Mixed:
                if (!AIDate.HasValue && (!NaturalMatingStart.HasValue || !NaturalMatingEnd.HasValue))
                    errors.Add("Either AI date or natural mating dates are required for mixed mating");
                break;
        }

        if (Cost.HasValue && Cost < 0)
            errors.Add("Cost cannot be negative");

        return errors;
    }
}

/// <summary>
/// Type of mating used for breeding
/// </summary>
public enum MatingType
{
    /// <summary>
    /// Artificial insemination only
    /// </summary>
    ArtificialInsemination = 0,

    /// <summary>
    /// Natural mating with rams
    /// </summary>
    NaturalMating = 1,

    /// <summary>
    /// Combination of AI and natural mating
    /// </summary>
    Mixed = 2
}