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
    
    // Core business entities
    public DbSet<Farmer> Farmers { get; set; } = null!;
    public DbSet<Farm> Farms { get; set; } = null!;
    public DbSet<LambingSeason> LambingSeasons { get; set; } = null!;
    
    // Production record entities
    public DbSet<BreedingRecord> BreedingRecords { get; set; } = null!;
    public DbSet<ScanningRecord> ScanningRecords { get; set; } = null!;
    public DbSet<LambingRecord> LambingRecords { get; set; } = null!;
    public DbSet<WeaningRecord> WeaningRecords { get; set; } = null!;
    
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
        
        // Configure Farmer entity
        modelBuilder.Entity<Farmer>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.FirebaseUid)
                  .IsUnique()
                  .HasDatabaseName("IX_Farmers_FirebaseUid");
                  
            entity.HasIndex(e => e.Email)
                  .IsUnique()
                  .HasDatabaseName("IX_Farmers_Email");
            
            entity.Property(e => e.FirebaseUid)
                  .IsRequired()
                  .HasMaxLength(128);
                  
            entity.Property(e => e.Email)
                  .IsRequired()
                  .HasMaxLength(256);
        });
        
        // Configure Farm entity
        modelBuilder.Entity<Farm>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.FarmerId)
                  .HasDatabaseName("IX_Farms_FarmerId");
            
            entity.HasOne(e => e.Farmer)
                  .WithMany(f => f.Farms)
                  .HasForeignKey(e => e.FarmerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure LambingSeason entity
        modelBuilder.Entity<LambingSeason>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.FarmId)
                  .HasDatabaseName("IX_LambingSeasons_FarmId");
                  
            entity.HasIndex(e => new { e.FarmId, e.Code })
                  .IsUnique()
                  .HasDatabaseName("IX_LambingSeasons_FarmId_Code");
            
            entity.HasOne(e => e.Farm)
                  .WithMany(f => f.LambingSeasons)
                  .HasForeignKey(e => e.FarmId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure BreedingRecord entity
        modelBuilder.Entity<BreedingRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.LambingSeasonId)
                  .HasDatabaseName("IX_BreedingRecords_LambingSeasonId");
            
            entity.HasOne(e => e.LambingSeason)
                  .WithMany(ls => ls.BreedingRecords)
                  .HasForeignKey(e => e.LambingSeasonId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure ScanningRecord entity
        modelBuilder.Entity<ScanningRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.LambingSeasonId)
                  .HasDatabaseName("IX_ScanningRecords_LambingSeasonId");
            
            entity.HasOne(e => e.LambingSeason)
                  .WithMany(ls => ls.ScanningRecords)
                  .HasForeignKey(e => e.LambingSeasonId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure LambingRecord entity
        modelBuilder.Entity<LambingRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.LambingSeasonId)
                  .HasDatabaseName("IX_LambingRecords_LambingSeasonId");
            
            entity.HasOne(e => e.LambingSeason)
                  .WithMany(ls => ls.LambingRecords)
                  .HasForeignKey(e => e.LambingSeasonId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure WeaningRecord entity
        modelBuilder.Entity<WeaningRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.LambingSeasonId)
                  .HasDatabaseName("IX_WeaningRecords_LambingSeasonId");
            
            entity.HasOne(e => e.LambingSeason)
                  .WithMany(ls => ls.WeaningRecords)
                  .HasForeignKey(e => e.LambingSeasonId)
                  .OnDelete(DeleteBehavior.Cascade);
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