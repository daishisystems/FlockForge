namespace FlockForge.Core.Models
{
    /// <summary>
    /// LambingSeason entity - simplified for offline-first development
    /// Firebase attributes will be added when proper mobile Firebase integration is implemented
    /// </summary>
    public class LambingSeason : BaseEntity
    {
        public string FarmId { get; set; } = string.Empty;
        
        public string Code { get; set; } = string.Empty;
        
        public string GroupName { get; set; } = string.Empty;
        
        public DateTime MatingStart { get; set; }
        
        public DateTime MatingEnd { get; set; }
        
        public DateTime LambingStart { get; set; }
        
        public DateTime LambingEnd { get; set; }
        
        public bool Active { get; set; }
        
        public string? Description { get; set; }
        
        public int? ExpectedEwes { get; set; }
        
        public string? Status { get; set; }
    }
}