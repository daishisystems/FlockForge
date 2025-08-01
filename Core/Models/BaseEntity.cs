using System;

namespace FlockForge.Core.Models
{
    /// <summary>
    /// Base entity for all data models - simplified for offline-first development
    /// Firebase attributes will be added when proper mobile Firebase integration is implemented
    /// </summary>
    public abstract class BaseEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public DateTime? CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        public bool IsDeleted { get; set; } = false;
        
        public string? UserId { get; set; }
        
        // Helper properties for client-side use
        public string? DocumentId { get; set; }
        
        // DateTime access helpers
        public DateTime CreatedAtDateTime => CreatedAt ?? DateTime.MinValue;
        public DateTime UpdatedAtDateTime => UpdatedAt ?? DateTime.MinValue;
    }
}