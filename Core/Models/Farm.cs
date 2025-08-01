namespace FlockForge.Core.Models
{
    /// <summary>
    /// Farm entity - simplified for offline-first development
    /// Firebase attributes will be added when proper mobile Firebase integration is implemented
    /// </summary>
    public class Farm : BaseEntity
    {
        public string FarmerId { get; set; } = string.Empty;
        
        public string FarmName { get; set; } = string.Empty;
        
        public string? CompanyName { get; set; }
        
        public string Breed { get; set; } = string.Empty;
        
        public int TotalProductionEwes { get; set; }
        
        public double Size { get; set; }
        
        public string SizeUnit { get; set; } = "hectares";
        
        public string? Address { get; set; }
        
        public string? City { get; set; }
        
        public string? Province { get; set; }
        
        // Simplified location as string for offline development
        // Will be replaced with proper GeoPoint when Firebase is integrated
        public string? Location { get; set; }
        
        public string? ProductionSystem { get; set; }
    }
}