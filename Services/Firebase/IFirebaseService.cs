using FlockForge.Models.Authentication;

namespace FlockForge.Services.Firebase;

public interface IFirebaseService
{
    Task<bool> IsAuthenticatedAsync();
    Task<bool> IsOnlineAsync();
    Task<AuthResult> AuthenticateAsync(string email, string password);
    Task<AuthResult> AuthenticateWithGoogleAsync();
    Task<AuthResult> RegisterAsync(string email, string password, string displayName);
    Task SignOutAsync();
    Task RequestPasswordResetAsync(string email);
    
    Task<T?> GetDocumentAsync<T>(string collection, string documentId) where T : class;
    Task SaveDocumentAsync<T>(string collection, string documentId, T document) where T : class;
    Task DeleteDocumentAsync(string collection, string documentId);
    
    Task SyncAllDataAsync();
    Task<bool> HasPendingSyncAsync();
    
    // Additional methods for token and user management
    FlockForgeUser? CurrentUser { get; }
    Task<bool> CanWorkOfflineAsync();
    string GenerateOfflineToken();
    string HashToken(string token);
}