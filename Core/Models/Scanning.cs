namespace FlockForge.Core.Models
{
    /// <summary>
    /// Scanning entity - simplified for offline-first development
    /// Firebase attributes will be added when proper mobile Firebase integration is implemented
    /// </summary>
    public class Scanning : BaseEntity
    {
        public string FarmId { get; set; } = string.Empty;
        
        public string LambingSeasonId { get; set; } = string.Empty;
        
        public string EweTag { get; set; } = string.Empty;
        
        public DateTime ScanDate { get; set; }
        
        public int NumberOfLambs { get; set; }
        
        public bool IsPregnant { get; set; }
        
        public string? ScannerName { get; set; }
        
        public string? Notes { get; set; }
        
        public DateTime? ExpectedLambingDate { get; set; }
        
        public string? ScanImageUrl { get; set; }
    }
}