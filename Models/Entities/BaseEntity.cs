using System.ComponentModel.DataAnnotations;

namespace FlockForge.Models.Entities;

public abstract class BaseEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    
    // Offline sync tracking
    public bool IsSynced { get; set; } = false;
    public DateTime? LastSyncedAt { get; set; }
}