namespace FlockForge.Core.Models
{
    /// <summary>
    /// Weaning entity - simplified for offline-first development
    /// Firebase attributes will be added when proper mobile Firebase integration is implemented
    /// </summary>
    public class Weaning : BaseEntity
    {
        public string FarmId { get; set; } = string.Empty;
        
        public string LambingSeasonId { get; set; } = string.Empty;
        
        public string EweTag { get; set; } = string.Empty;
        
        public List<string> LambTags { get; set; } = new();
        
        public DateTime WeaningDate { get; set; }
        
        public int NumberOfLambsWeaned { get; set; }
        
        public double? AverageWeaningWeight { get; set; }
        
        public int LambAgeAtWeaningDays { get; set; }
        
        public string? WeaningMethod { get; set; }
        
        public string? Notes { get; set; }
        
        public double? EweWeightAtWeaning { get; set; }
        
        public string? EweConditionScore { get; set; }
        
        public List<double> IndividualLambWeights { get; set; } = new();
    }
}