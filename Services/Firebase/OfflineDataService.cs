using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FlockForge.Core.Interfaces;
using FlockForge.Core.Models;

namespace FlockForge.Services.Firebase
{
    /// <summary>
    /// Offline-first data service that provides local storage capabilities
    /// while maintaining the same interface as FirestoreService.
    /// This eliminates the need for Firebase credentials during development.
    /// </summary>
    public class OfflineDataService : IDataService, IDisposable
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<OfflineDataService> _logger;
        private readonly Dictionary<string, Dictionary<string, BaseEntity>> _collections = new();
        private readonly object _lock = new object();
        private bool _disposed;

        public OfflineDataService(
            IAuthenticationService authService,
            ILogger<OfflineDataService> logger)
        {
            _authService = authService;
            _logger = logger;
            _logger.LogInformation("OfflineDataService initialized - running in offline-only mode");
        }

        public Task<T?> GetAsync<T>(string documentId) where T : BaseEntity
        {
            if (_disposed) return Task.FromResult<T?>(null);

            try
            {
                if (!_authService.IsAuthenticated)
                {
                    _logger.LogWarning("Attempted to get document while not authenticated");
                    return Task.FromResult<T?>(null);
                }

                var collectionName = GetCollectionName<T>();
                
                lock (_lock)
                {
                    if (_collections.TryGetValue(collectionName, out var collection) &&
                        collection.TryGetValue(documentId, out var entity) &&
                        entity is T typedEntity &&
                        typedEntity.UserId == _authService.CurrentUser?.Id &&
                        !typedEntity.IsDeleted)
                    {
                        _logger.LogDebug("Retrieved document {DocumentId} from offline storage", documentId);
                        return Task.FromResult<T?>(typedEntity);
                    }
                }

                return Task.FromResult<T?>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get document {DocumentId}", documentId);
                return Task.FromResult<T?>(null);
            }
        }

        public Task<IReadOnlyList<T>> GetAllAsync<T>() where T : BaseEntity
        {
            if (_disposed) return Task.FromResult<IReadOnlyList<T>>(new List<T>());

            try
            {
                if (!_authService.IsAuthenticated)
                {
                    return Task.FromResult<IReadOnlyList<T>>(new List<T>());
                }

                var userId = _authService.CurrentUser!.Id;
                var collectionName = GetCollectionName<T>();

                lock (_lock)
                {
                    if (_collections.TryGetValue(collectionName, out var collection))
                    {
                        var results = collection.Values
                            .OfType<T>()
                            .Where(e => e.UserId == userId && !e.IsDeleted)
                            .OrderByDescending(e => e.UpdatedAt)
                            .ToList();

                        _logger.LogDebug("Retrieved {Count} documents from offline storage", results.Count);
                        return Task.FromResult<IReadOnlyList<T>>(results);
                    }
                }

                return Task.FromResult<IReadOnlyList<T>>(new List<T>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all documents");
                return Task.FromResult<IReadOnlyList<T>>(new List<T>());
            }
        }

        public Task<bool> SaveAsync<T>(T entity) where T : BaseEntity
        {
            if (_disposed) return Task.FromResult(false);

            try
            {
                if (!_authService.IsAuthenticated)
                {
                    _logger.LogWarning("Cannot save - no authenticated user");
                    return Task.FromResult(false);
                }

                // Set metadata
                if (string.IsNullOrEmpty(entity.Id))
                {
                    entity.Id = Guid.NewGuid().ToString();
                }

                entity.UserId = _authService.CurrentUser!.Id;
                entity.UpdatedAt = DateTime.UtcNow;

                var collectionName = GetCollectionName<T>();

                lock (_lock)
                {
                    if (!_collections.ContainsKey(collectionName))
                    {
                        _collections[collectionName] = new Dictionary<string, BaseEntity>();
                    }

                    _collections[collectionName][entity.Id] = entity;
                }

                _logger.LogInformation("Saved entity {EntityId} to offline storage", entity.Id);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save entity {EntityId}", entity.Id);
                return Task.FromResult(false);
            }
        }

        public Task<bool> DeleteAsync<T>(string documentId) where T : BaseEntity
        {
            if (_disposed) return Task.FromResult(false);

            try
            {
                var entity = GetAsync<T>(documentId).Result;
                if (entity == null) return Task.FromResult(false);

                // Soft delete
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.UtcNow;

                return SaveAsync(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete document {DocumentId}", documentId);
                return Task.FromResult(false);
            }
        }

        public Task<bool> BatchSaveAsync<T>(IEnumerable<T> entities) where T : BaseEntity
        {
            if (_disposed) return Task.FromResult(false);

            try
            {
                var entityList = entities.ToList();
                if (!entityList.Any()) return Task.FromResult(true);

                if (!_authService.IsAuthenticated)
                {
                    return Task.FromResult(false);
                }

                var userId = _authService.CurrentUser!.Id;
                var collectionName = GetCollectionName<T>();

                lock (_lock)
                {
                    if (!_collections.ContainsKey(collectionName))
                    {
                        _collections[collectionName] = new Dictionary<string, BaseEntity>();
                    }

                    foreach (var entity in entityList)
                    {
                        if (string.IsNullOrEmpty(entity.Id))
                        {
                            entity.Id = Guid.NewGuid().ToString();
                        }

                        entity.UserId = userId;
                        entity.UpdatedAt = DateTime.UtcNow;

                        _collections[collectionName][entity.Id] = entity;
                    }
                }

                _logger.LogInformation("Batch saved {Count} entities to offline storage", entityList.Count);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to batch save entities");
                return Task.FromResult(false);
            }
        }

        public Task<IReadOnlyList<T>> QueryAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity
        {
            if (_disposed) return Task.FromResult<IReadOnlyList<T>>(new List<T>());

            try
            {
                var allData = GetAllAsync<T>().Result;
                var compiledPredicate = predicate.Compile();
                var results = allData.Where(compiledPredicate).ToList();
                
                return Task.FromResult<IReadOnlyList<T>>(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query documents");
                return Task.FromResult<IReadOnlyList<T>>(new List<T>());
            }
        }

        public IObservable<T> DocumentChanged<T>(string documentId) where T : BaseEntity
        {
            // Return empty observable for offline mode
            return System.Reactive.Linq.Observable.Empty<T>();
        }

        public IObservable<IReadOnlyList<T>> CollectionChanged<T>() where T : BaseEntity
        {
            // Return empty observable for offline mode
            return System.Reactive.Linq.Observable.Empty<IReadOnlyList<T>>();
        }

        public void UnsubscribeAll()
        {
            // No subscriptions in offline mode
            _logger.LogDebug("UnsubscribeAll called - no active listeners in offline mode");
        }

        private string GetCollectionName<T>() where T : BaseEntity
        {
            var typeName = typeof(T).Name.ToLowerInvariant();
            return $"{typeName}s"; // Simple pluralization
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                lock (_lock)
                {
                    _collections.Clear();
                }
                
                _logger.LogInformation("OfflineDataService disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disposal");
            }
        }
    }
}