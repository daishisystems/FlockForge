using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FlockForge.Core.Models;

namespace FlockForge.Core.Interfaces
{
    public interface IDataService
    {
        Task<T?> GetAsync<T>(string documentId) where T : BaseEntity;
        Task<IReadOnlyList<T>> GetAllAsync<T>() where T : BaseEntity;
        Task<IReadOnlyList<T>> QueryAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity;
        Task<bool> SaveAsync<T>(T entity) where T : BaseEntity;
        Task<bool> DeleteAsync<T>(string documentId) where T : BaseEntity;
        Task<bool> BatchSaveAsync<T>(IEnumerable<T> entities) where T : BaseEntity;
        IObservable<T> DocumentChanged<T>(string documentId) where T : BaseEntity;
        IObservable<IReadOnlyList<T>> CollectionChanged<T>() where T : BaseEntity;
    }
}