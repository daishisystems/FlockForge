namespace FlockForge.Services.Firebase;

public interface IFirebaseService
{
    Task<bool> IsAuthenticatedAsync();
    Task<bool> IsOnlineAsync();
    Task AuthenticateAsync(string email, string password);
    Task SignOutAsync();
    
    Task<T?> GetDocumentAsync<T>(string collection, string documentId) where T : class;
    Task SaveDocumentAsync<T>(string collection, string documentId, T document) where T : class;
    Task DeleteDocumentAsync(string collection, string documentId);
    
    Task SyncAllDataAsync();
    Task<bool> HasPendingSyncAsync();
}