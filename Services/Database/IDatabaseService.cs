using FlockForge.Models.Entities;

namespace FlockForge.Services.Database;

public interface IDatabaseService
{
    Task InitializeAsync();
    Task<T?> GetByIdAsync<T>(string id) where T : class;
    Task<IEnumerable<T>> GetAllAsync<T>() where T : class;
    Task<IEnumerable<T>> GetActiveAsync<T>() where T : class;
    Task<IEnumerable<T>> GetActiveExplicitAsync<T>() where T : BaseEntity;
    Task<string> SaveAsync<T>(T entity) where T : class;
    Task<bool> DeleteAsync<T>(string id) where T : class;
    Task<bool> SoftDeleteAsync<T>(string id) where T : class;
    Task SyncPendingChangesAsync();
}