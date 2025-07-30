using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlockForge.Models.Entities;

/// <summary>
/// Base entity class with simplified tracking and thread-safe properties
/// </summary>
public abstract class BaseEntity
{
    [Key]
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    public bool IsDeleted { get; set; }
    
    // Simplified sync tracking - let sync service handle complexity
    public bool IsSynced { get; set; } = true;
    
    // EF Core handles concurrency - no custom implementation needed
    [Timestamp]
    public byte[]? RowVersion { get; set; }
    
    // Helper property for sync operations
    [NotMapped]
    public bool NeedsSync => !IsSynced && !IsDeleted;
}