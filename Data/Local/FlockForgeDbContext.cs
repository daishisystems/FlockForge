using Microsoft.EntityFrameworkCore;
using FlockForge.Models.Entities;

namespace FlockForge.Data.Local;

public class FlockForgeDbContext : DbContext
{
    public FlockForgeDbContext(DbContextOptions<FlockForgeDbContext> options) : base(options)
    {
    }

    // DbSets will be added as we create entities
    // public DbSet<Farmer> Farmers { get; set; }
    // public DbSet<Farm> Farms { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure entity relationships and constraints
        // Will be expanded as we add entities
    }
}