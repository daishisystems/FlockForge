using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Plugin.Firebase.Firestore;
using Polly;
using FlockForge.Core.Interfaces;
using FlockForge.Core.Models;
using FlockForge.Helpers;
using FlockForge.Core.Util;

namespace FlockForge.Services.Firebase
{
    public class FirestoreService : IDataService, IDisposable
    {
        private readonly Lazy<IFirebaseFirestore> _lazyFirestore;
        private IFirebaseFirestore Firestore => _lazyFirestore.Value;
        private readonly IConnectivity _connectivity;
        private readonly IAuthenticationService _authService;
        private readonly ILogger<FirestoreService> _logger;
        private readonly IAsyncPolicy _retryPolicy;
        private readonly Dictionary<string, IDisposable> _listeners = new();
        
        public FirestoreService(
            Lazy<IFirebaseFirestore> lazyFirestore,
            IConnectivity connectivity,
            IAuthenticationService authService,
            ILogger<FirestoreService> logger)
        {
            _lazyFirestore = lazyFirestore;
            _connectivity = connectivity;
            _authService = authService;
            _logger = logger;
            
            _retryPolicy = ConfigureRetryPolicy();
        }
        
        private IAsyncPolicy ConfigureRetryPolicy()
        {
            return Policy
                .Handle<Exception>(ex => IsTransientError(ex))
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning("Retry {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
                    });
        }
        
        private bool IsTransientError(Exception ex)
        {
            // Identify transient errors that should be retried
            return ex.Message.Contains("unavailable") || 
                   ex.Message.Contains("deadline exceeded") ||
                   ex.Message.Contains("internal");
        }
        
        public async Task<T?> GetAsync<T>(string documentId) where T : BaseEntity
        {
            try
            {
                if (!_authService.IsAuthenticated)
                {
                    _logger.LogWarning("Attempted to get document while not authenticated");
                    return null;
                }
                
                var docRef = Firestore
                    .GetCollection(GetCollectionName<T>())
                    .GetDocument(documentId);
                
                // Firestore will automatically use cached data when offline
                var snapshot = await _retryPolicy.ExecuteAsync(async () =>
                    await docRef.GetDocumentSnapshotAsync<T>());
                
                if (snapshot != null && snapshot.Data != null)
                {
                    return snapshot.Data;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get document {DocumentId}", documentId);
                return null;
            }
        }
        
        public async Task<IReadOnlyList<T>> GetAllAsync<T>() where T : BaseEntity
        {
            try
            {
                if (!_authService.IsAuthenticated)
                {
                    return new List<T>();
                }
                
                var userId = _authService.CurrentUser!.Id;
                var collectionName = GetCollectionName<T>();
                
                var query = Firestore
                    .GetCollection(collectionName)
                    .WhereEqualsTo("userId", userId)
                    .WhereEqualsTo("isDeleted", false)
                    .OrderBy("updatedAt", true);
                
                var snapshot = await _retryPolicy.ExecuteAsync(async () =>
                    await query.GetDocumentsAsync<T>());
                
                return snapshot.Documents
                    .Select(d => d.Data)
                    .Where(d => d != null)
                    .ToList()!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all documents");
                return new List<T>();
            }
        }
        
        public async Task<bool> SaveAsync<T>(T entity) where T : BaseEntity
        {
            try
            {
                if (!_authService.IsAuthenticated)
                {
                    return false;
                }
                
                // Update metadata
                entity.UpdatedAt = DateTime.UtcNow;
                if (string.IsNullOrEmpty(entity.Id))
                {
                    entity.Id = Guid.NewGuid().ToString();
                    entity.CreatedAt = DateTime.UtcNow;
                }
                
                // Set user association
                entity.UserId = _authService.CurrentUser!.Id;
                
                var docRef = Firestore
                    .GetCollection(GetCollectionName<T>())
                    .GetDocument(entity.Id);
                
                await _retryPolicy.ExecuteAsync(async () =>
                    await docRef.SetDataAsync(entity));
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save entity {EntityId}", entity.Id);
                return false;
            }
        }
        
        public async Task<bool> DeleteAsync<T>(string documentId) where T : BaseEntity
        {
            try
            {
                var entity = await GetAsync<T>(documentId);
                if (entity == null) return false;
                
                // Soft delete
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.UtcNow;
                
                return await SaveAsync(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete document {DocumentId}", documentId);
                return false;
            }
        }
        
        public async Task<bool> BatchSaveAsync<T>(IEnumerable<T> entities) where T : BaseEntity
        {
            try
            {
                var entityList = entities.ToList();
                if (!entityList.Any()) return true;
                
                if (!_authService.IsAuthenticated)
                {
                    return false;
                }
                
                var userId = _authService.CurrentUser!.Id;
                
                // Firestore batch limit is 500
                var batches = entityList.Chunk(500);
                
                foreach (var batch in batches)
                {
                    var writeBatch = Firestore.CreateBatch();
                    
                    foreach (var entity in batch)
                    {
                        entity.UpdatedAt = DateTime.UtcNow;
                        entity.UserId = userId;
                        
                        if (string.IsNullOrEmpty(entity.Id))
                        {
                            entity.Id = Guid.NewGuid().ToString();
                            entity.CreatedAt = DateTime.UtcNow;
                        }
                        
                        var docRef = Firestore
                            .GetCollection(GetCollectionName<T>())
                            .GetDocument(entity.Id);
                        
                        writeBatch.SetData(docRef, entity);
                    }
                    
                    await _retryPolicy.ExecuteAsync(async () => 
                        await writeBatch.CommitAsync());
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to batch save entities");
                return false;
            }
        }
        
        public async Task<IReadOnlyList<T>> QueryAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity
        {
            try
            {
                // For complex expression tree parsing, you would implement an expression visitor
                // For now, we'll use a simpler approach
                var allData = await GetAllAsync<T>();
                var compiledPredicate = predicate.Compile();
                return allData.Where(compiledPredicate).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query documents");
                return new List<T>();
            }
        }
        
        public IObservable<T> DocumentChanged<T>(string documentId) where T : BaseEntity
        {
            var subject = new Subject<T>();
            var key = $"{typeof(T).Name}:{documentId}";
            
            // Remove existing listener if any
            if (_listeners.TryGetValue(key, out var existing))
            {
                existing.Dispose();
            }
            
            var docRef = Firestore
                .GetCollection(GetCollectionName<T>())
                .GetDocument(documentId);
            
            var listener = docRef.AddSnapshotListener<T>(snapshot =>
            {
                if (snapshot != null && snapshot.Data != null)
                {
                    subject.OnNext(snapshot.Data);
                }
            }, error =>
            {
                _logger.LogError("Document listener error: {Error}", error.Message);
            });
            
            _listeners[key] = listener;
            
            return subject;
        }
        
        public IDisposable SubscribeToDocument<T>(string documentId, Action<T> onNext) where T : BaseEntity
        {
            var key = $"{typeof(T).Name}:{documentId}:subscription";
            
            // Remove existing listener if any
            if (_listeners.TryGetValue(key, out var existing))
            {
                existing.Dispose();
            }
            
            var docRef = Firestore
                .GetCollection(GetCollectionName<T>())
                .GetDocument(documentId);
            
            var listener = docRef.AddSnapshotListener<T>(snapshot =>
            {
                if (snapshot != null && snapshot.Data != null)
                {
                    onNext(snapshot.Data);
                }
            }, error =>
            {
                _logger.LogError("Document listener error: {Error}", error.Message);
            });
            
            _listeners[key] = listener;
            
            return listener;
        }
        
        public IObservable<IReadOnlyList<T>> CollectionChanged<T>() where T : BaseEntity
        {
            var subject = new Subject<IReadOnlyList<T>>();
            var key = $"{typeof(T).Name}:collection";
            
            // Remove existing listener if any
            if (_listeners.TryGetValue(key, out var existing))
            {
                existing.Dispose();
            }
            
            if (!_authService.IsAuthenticated)
            {
                return subject;
            }
            
            var userId = _authService.CurrentUser!.Id;
            
            var query = Firestore
                .GetCollection(GetCollectionName<T>())
                .WhereEqualsTo("userId", userId)
                .WhereEqualsTo("isDeleted", false)
                .OrderBy("updatedAt", true);
            
            var listener = query.AddSnapshotListener<T>(snapshot =>
            {
                if (snapshot != null)
                {
                    var data = snapshot.Documents
                        .Select(d => d.Data)
                        .Where(d => d != null)
                        .ToList();
                    
                    subject.OnNext(data!);
                }
            }, error =>
            {
                _logger.LogError("Collection listener error: {Error}", error.Message);
            });
            
            _listeners[key] = listener;
            
            return subject;
        }
        
        public IDisposable SubscribeToCollection<T>(string path, Action<IReadOnlyList<T>> onSnapshot) where T : BaseEntity
        {
            var key = $"{typeof(T).Name}:{path}:subscription";
            
            // Remove existing listener if any
            if (_listeners.TryGetValue(key, out var existing))
            {
                existing.Dispose();
            }
            
            if (!_authService.IsAuthenticated)
            {
                // Return a no-op disposable if not authenticated
                return new ActionDisposable(() => { });
            }
            
            var userId = _authService.CurrentUser!.Id;
            
            var query = Firestore
                .GetCollection(path)
                .WhereEqualsTo("userId", userId)
                .WhereEqualsTo("isDeleted", false)
                .OrderBy("updatedAt", true);
            
            var listener = query.AddSnapshotListener<T>(snapshot =>
            {
                if (snapshot != null)
                {
                    var data = snapshot.Documents
                        .Select(d => d.Data)
                        .Where(d => d != null)
                        .ToList();
                    
                    onSnapshot(data!);
                }
            }, error =>
            {
                _logger.LogError("Collection listener error: {Error}", error.Message);
            });
            
            _listeners[key] = listener;
            
            return listener;
        }
        
        private string GetCollectionName<T>() where T : BaseEntity
        {
            var typeName = typeof(T).Name.ToLowerInvariant();
            
            // Handle pluralization
            return typeName switch
            {
                "farm" => "farms",
                "farmer" => "farmers",
                "lambingseason" => "lambing_seasons",
                "breeding" => "breeding",
                "scanning" => "scanning",
                "lambing" => "lambing",
                "weaning" => "weaning",
                _ => $"{typeName}s"
            };
        }
        
        public void Dispose()
        {
            foreach (var listener in _listeners.Values)
            {
                listener?.Dispose();
            }
            _listeners.Clear();
        }
    }
}