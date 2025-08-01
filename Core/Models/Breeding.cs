namespace FlockForge.Core.Models
{
    /// <summary>
    /// Breeding entity - simplified for offline-first development
    /// Firebase attributes will be added when proper mobile Firebase integration is implemented
    /// </summary>
    public class Breeding : BaseEntity
    {
        public string FarmId { get; set; } = string.Empty;
        
        public string LambingSeasonId { get; set; } = string.Empty;
        
        public string EweTag { get; set; } = string.Empty;
        
        public string RamTag { get; set; } = string.Empty;
        
        public DateTime BreedingDate { get; set; }
        
        public string? BreedingMethod { get; set; }
        
        public string? Notes { get; set; }
        
        public bool IsSuccessful { get; set; }
        
        public DateTime? ExpectedLambingDate { get; set; }
    }
}