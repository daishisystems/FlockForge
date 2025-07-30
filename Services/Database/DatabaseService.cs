using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FlockForge.Data.Local;
using FlockForge.Models.Entities;
using System.Linq.Expressions;

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
            var query = _context.Set<T>().AsQueryable();
            
            // If T is BaseEntity, filter out deleted items
            if (typeof(T).IsSubclassOf(typeof(BaseEntity)))
            {
                query = query.Where(e => !((BaseEntity)(object)e).IsDeleted);
            }
            
            return await query.ToListAsync();
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
                var dbSet = property.GetValue(_context);
                if (dbSet != null)
                {
                    var queryable = (IQueryable<BaseEntity>)dbSet;
                    var unsynced = await queryable
                        .Where(e => !e.IsSynced && !e.IsDeleted)
                        .ToListAsync();
                    unsyncedEntities.AddRange(unsynced);
                }
            }
        }

        return unsyncedEntities;
    }
}