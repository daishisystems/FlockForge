namespace FlockForge.Core.Models
{
    /// <summary>
    /// Lambing entity - simplified for offline-first development
    /// Firebase attributes will be added when proper mobile Firebase integration is implemented
    /// </summary>
    public class Lambing : BaseEntity
    {
        public string FarmId { get; set; } = string.Empty;
        
        public string LambingSeasonId { get; set; } = string.Empty;
        
        public string EweTag { get; set; } = string.Empty;
        
        public DateTime LambingDate { get; set; }
        
        public int NumberOfLambsBorn { get; set; }
        
        public int NumberOfLambsAlive { get; set; }
        
        public string? LambingDifficulty { get; set; }
        
        public bool AssistedLambing { get; set; }
        
        public string? Notes { get; set; }
        
        public List<string> LambTags { get; set; } = new();
        
        public string? VeterinarianName { get; set; }
        
        public double? EweWeightPostLambing { get; set; }
        
        public string? LambingLocation { get; set; }
    }
}