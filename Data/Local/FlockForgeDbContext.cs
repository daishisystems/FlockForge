using Microsoft.EntityFrameworkCore;
using FlockForge.Models.Entities;
using FlockForge.Models.Authentication;

namespace FlockForge.Data.Local;

public class FlockForgeDbContext : DbContext
{
    public FlockForgeDbContext(DbContextOptions<FlockForgeDbContext> options) : base(options)
    {
    }

    // Authentication entities
    public DbSet<FlockForgeUser> Users { get; set; } = null!;
    
    // Future entities will be added here
    // public DbSet<Farmer> Farmers { get; set; }
    // public DbSet<Farm> Farms { get; set; }
    // public DbSet<Group> Groups { get; set; }
    // public DbSet<BreedingRecord> BreedingRecords { get; set; }
    // public DbSet<ScanningRecord> ScanningRecords { get; set; }
    // public DbSet<LambingRecord> LambingRecords { get; set; }
    // public DbSet<WeaningRecord> WeaningRecords { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure FlockForgeUser entity
        modelBuilder.Entity<FlockForgeUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.FirebaseUid)
                  .IsUnique()
                  .HasDatabaseName("IX_Users_FirebaseUid");
                  
            entity.HasIndex(e => e.Email)
                  .IsUnique()
                  .HasDatabaseName("IX_Users_Email");
            
            entity.Property(e => e.Id)
                  .IsRequired()
                  .HasMaxLength(128);
                  
            entity.Property(e => e.FirebaseUid)
                  .IsRequired()
                  .HasMaxLength(128);
                  
            entity.Property(e => e.Email)
                  .IsRequired()
                  .HasMaxLength(256);
                  
            entity.Property(e => e.DisplayName)
                  .HasMaxLength(256);
                  
            entity.Property(e => e.PhotoUrl)
                  .HasMaxLength(2048);
                  
            entity.Property(e => e.OfflineTokenHash)
                  .HasMaxLength(512);
                  
            entity.Property(e => e.CreatedAt)
                  .IsRequired();
                  
            entity.Property(e => e.UpdatedAt)
                  .IsRequired();
                  
            entity.Property(e => e.RowVersion)
                  .IsRowVersion();
                  
            // Configure soft delete filter
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
        
        // Configure BaseEntity properties for all entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property<DateTimeOffset>("CreatedAt")
                    .IsRequired();
                    
                modelBuilder.Entity(entityType.ClrType)
                    .Property<DateTimeOffset>("UpdatedAt")
                    .IsRequired();
                    
                modelBuilder.Entity(entityType.ClrType)
                    .Property<bool>("IsDeleted")
                    .HasDefaultValue(false);
                    
                modelBuilder.Entity(entityType.ClrType)
                    .Property<bool>("IsSynced")
                    .HasDefaultValue(true);
            }
        }
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update timestamps for BaseEntity instances
        var entries = ChangeTracker.Entries<BaseEntity>();
        
        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.IsSynced = false;
                    break;
                    
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
                    entry.Entity.IsSynced = false;
                    break;
                    
                case EntityState.Deleted:
                    // Implement soft delete
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
                    entry.Entity.IsSynced = false;
                    break;
            }
        }
        
        return await base.SaveChangesAsync(cancellationToken);
    }
}