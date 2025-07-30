using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FlockForge.Data.Local;
using FlockForge.Models.Entities;
using System.Linq.Expressions;
using System.Reflection;

namespace FlockForge.Services.Database;

public class DatabaseService : IDatabaseService
{
    private readonly FlockForgeDbContext _context;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(FlockForgeDbContext context, ILogger<DatabaseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _context.Database.EnsureCreatedAsync();
            _logger.LogInformation("Database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }

    public async Task<T?> GetByIdAsync<T>(string id) where T : class
    {
        try
        {
            return await _context.Set<T>().FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get entity by id: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<T>> GetAllAsync<T>() where T : class
    {
        try
        {
            return await _context.Set<T>().ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all entities of type {Type}", typeof(T).Name);
            throw;
        }
    }

    public async Task<IEnumerable<T>> GetActiveAsync<T>() where T : class
    {
        try
        {
            // Use EF Core's global query filters instead of manual filtering
            // BaseEntity types already have query filters configured in OnModelCreating
            return await _context.Set<T>().ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active entities of type {Type}", typeof(T).Name);
            throw;
        }
    }

    // Alternative method for explicit active filtering when needed
    public async Task<IEnumerable<T>> GetActiveExplicitAsync<T>() where T : BaseEntity
    {
        try
        {
            // This works because T is constrained to BaseEntity
            return await _context.Set<T>()
                .Where(e => !e.IsDeleted)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active entities of type {Type}", typeof(T).Name);
            throw;
        }
    }

    public async Task<string> SaveAsync<T>(T entity) where T : class
    {
        try
        {
            if (entity is BaseEntity baseEntity)
            {
                baseEntity.UpdatedAt = DateTimeOffset.UtcNow;
                baseEntity.IsSynced = false;
            }

            var entry = _context.Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                _context.Set<T>().Add(entity);
            }

            await _context.SaveChangesAsync();
            
            // Return the ID if it's a BaseEntity
            if (entity is BaseEntity be)
            {
                return be.Id;
            }
            
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save entity of type {Type}", typeof(T).Name);
            throw;
        }
    }

    public async Task<bool> DeleteAsync<T>(string id) where T : class
    {
        try
        {
            var entity = await _context.Set<T>().FindAsync(id);
            if (entity == null)
                return false;

            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete entity with id: {Id}", id);
            throw;
        }
    }

    public async Task<bool> SoftDeleteAsync<T>(string id) where T : class
    {
        try
        {
            var entity = await _context.Set<T>().FindAsync(id);
            if (entity == null)
                return false;

            if (entity is BaseEntity baseEntity)
            {
                baseEntity.IsDeleted = true;
                baseEntity.UpdatedAt = DateTimeOffset.UtcNow;
                baseEntity.IsSynced = false;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to soft delete entity with id: {Id}", id);
            throw;
        }
    }

    public async Task SyncPendingChangesAsync()
    {
        try
        {
            // Get all entities that need syncing
            var unsyncedEntities = await GetUnsyncedEntitiesAsync();
            
            _logger.LogInformation("Found {Count} entities to sync", unsyncedEntities.Count());
            
            // TODO: Implement actual sync logic with Firebase
            // For now, just mark as synced
            foreach (var entity in unsyncedEntities)
            {
                entity.IsSynced = true;
                entity.UpdatedAt = DateTimeOffset.UtcNow;
            }
            
            await _context.SaveChangesAsync();
            _logger.LogInformation("Sync completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync pending changes");
            throw;
        }
    }

    private async Task<IEnumerable<BaseEntity>> GetUnsyncedEntitiesAsync()
    {
        var unsyncedEntities = new List<BaseEntity>();
        
        // Get all DbSet properties from the context
        var dbSetProperties = _context.GetType().GetProperties()
            .Where(p => p.PropertyType.IsGenericType &&
                       p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .ToList();

        foreach (var property in dbSetProperties)
        {
            var entityType = property.PropertyType.GetGenericArguments()[0];
            if (entityType.IsSubclassOf(typeof(BaseEntity)))
            {
                // Use reflection to call the generic method
                var method = typeof(DatabaseService)
                    .GetMethod(nameof(GetUnsyncedEntitiesOfTypeAsync), BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.MakeGenericMethod(entityType);
                    
                if (method != null)
                {
                    var result = await (Task<IEnumerable<BaseEntity>>)method.Invoke(this, null)!;
                    unsyncedEntities.AddRange(result);
                }
            }
        }

        return unsyncedEntities;
    }

    private async Task<IEnumerable<BaseEntity>> GetUnsyncedEntitiesOfTypeAsync<T>() where T : BaseEntity
    {
        try
        {
            return await _context.Set<T>()
                .Where(e => !e.IsSynced && !e.IsDeleted)
                .Cast<BaseEntity>()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unsynced entities of type {Type}", typeof(T).Name);
            throw;
        }
    }
}