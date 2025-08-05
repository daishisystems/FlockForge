using Microsoft.Extensions.Logging;
using FlockForge.Models.Authentication;
using FlockForge.Core.Interfaces;
using FlockForge.Core.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlockForge.Services.Firebase;

public class FirebaseService : IFirebaseService
{
    private readonly ILogger<FirebaseService> _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly IDataService _dataService;

    public FirebaseService(
        ILogger<FirebaseService> logger,
        IAuthenticationService authenticationService,
        IDataService dataService)
    {
        _logger = logger;
        _authenticationService = authenticationService;
        _dataService = dataService;
    }

    public Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            return Task.FromResult(_authenticationService.IsAuthenticated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check authentication status");
            return Task.FromResult(false);
        }
    }

    public async Task<bool> IsOnlineAsync()
    {
        try
        {
            // Simple connectivity check
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var client = new HttpClient();
            var response = await client.GetAsync("https://www.google.com", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Network connectivity check failed");
            return false;
        }
    }

    public async Task<Models.Authentication.AuthResult> AuthenticateAsync(string email, string password)
    {
        try
        {
            var result = await _authenticationService.SignInWithEmailPasswordAsync(email, password);
            
            if (result.IsSuccess && result.User != null)
            {
                var flockForgeUser = new FlockForgeUser
                {
                    FirebaseUid = result.User.Id,
                    Email = result.User.Email ?? email,
                    DisplayName = result.User.DisplayName ?? email.Split('@')[0],
                    Provider = AuthProvider.EmailPassword,
                    IsEmailVerified = result.User.IsEmailVerified
                };
                
                return Models.Authentication.AuthResult.Successful(flockForgeUser);
            }
            else
            {
                return Models.Authentication.AuthResult.Failed("AUTH_FAILED", result.ErrorMessage ?? "Authentication failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed for user: {Email}", email);
            return Models.Authentication.AuthResult.Failed("AUTH_FAILED", "Authentication failed. Please try again.");
        }
    }

    public async Task<Models.Authentication.AuthResult> AuthenticateWithGoogleAsync()
    {
        try
        {
            var result = await _authenticationService.SignInWithGoogleAsync();
            
            if (result.IsSuccess && result.User != null)
            {
                var flockForgeUser = new FlockForgeUser
                {
                    FirebaseUid = result.User.Id,
                    Email = result.User.Email ?? "google.user@example.com",
                    DisplayName = result.User.DisplayName ?? "Google User",
                    Provider = AuthProvider.Google,
                    IsEmailVerified = result.User.IsEmailVerified
                };
                
                return Models.Authentication.AuthResult.Successful(flockForgeUser);
            }
            else
            {
                return Models.Authentication.AuthResult.Failed("GOOGLE_AUTH_FAILED", result.ErrorMessage ?? "Google authentication failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google authentication failed");
            return Models.Authentication.AuthResult.Failed("GOOGLE_AUTH_FAILED", "Google authentication failed. Please try again.");
        }
    }

    public async Task<Models.Authentication.AuthResult> RegisterAsync(string email, string password, string displayName)
    {
        try
        {
            var result = await _authenticationService.SignUpWithEmailPasswordAsync(email, password);
            
            if (result.IsSuccess && result.User != null)
            {
                var flockForgeUser = new FlockForgeUser
                {
                    FirebaseUid = result.User.Id,
                    Email = result.User.Email ?? email,
                    DisplayName = displayName ?? result.User.DisplayName ?? email.Split('@')[0],
                    Provider = AuthProvider.EmailPassword,
                    IsEmailVerified = result.User.IsEmailVerified
                };
                
                return Models.Authentication.AuthResult.Successful(flockForgeUser, AuthAction.None);
            }
            else
            {
                return Models.Authentication.AuthResult.Failed("REGISTRATION_FAILED", result.ErrorMessage ?? "Registration failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for user: {Email}", email);
            return Models.Authentication.AuthResult.Failed("REGISTRATION_FAILED", "Registration failed. Please try again.");
        }
    }

    public async Task SignOutAsync()
    {
        try
        {
            await _authenticationService.SignOutAsync();
            _logger.LogInformation("User signed out successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sign out failed");
            throw;
        }
    }

    public async Task RequestPasswordResetAsync(string email)
    {
        try
        {
            var success = await _authenticationService.SendPasswordResetEmailAsync(email);
            if (!success)
            {
                throw new InvalidOperationException("Failed to send password reset email");
            }
            _logger.LogInformation("Password reset email sent to: {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset failed for email: {Email}", email);
            throw new InvalidOperationException("Failed to send password reset email. Please check the email address and try again.");
        }
    }

    public Task<T?> GetDocumentAsync<T>(string collection, string documentId) where T : class
    {
        try
        {
            // For now, return null for compatibility
            // This method is deprecated in favor of using IDataService directly
            _logger.LogWarning("GetDocumentAsync called - this method is deprecated. Use IDataService directly for BaseEntity types.");
            return Task.FromResult<T?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document {DocumentId} from collection {Collection}", documentId, collection);
            throw;
        }
    }

    public async Task SaveDocumentAsync<T>(string collection, string documentId, T document) where T : class
    {
        try
        {
            // Convert to BaseEntity if possible for our new service
            if (document is BaseEntity entity)
            {
                entity.Id = documentId;
                await _dataService.SaveAsync(entity);
            }
            else
            {
                _logger.LogWarning("SaveDocumentAsync called with non-BaseEntity type: {Type}", typeof(T).Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save document {DocumentId} to collection {Collection}", documentId, collection);
            throw;
        }
    }

    public async Task DeleteDocumentAsync(string collection, string documentId)
    {
        try
        {
            // Use generic delete - this will work for any BaseEntity type
            await _dataService.DeleteAsync<BaseEntity>(documentId);
            _logger.LogDebug("Deleted document {DocumentId} from collection {Collection}", documentId, collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document {DocumentId} from collection {Collection}", documentId, collection);
            throw;
        }
    }

    public async Task SyncAllDataAsync()
    {
        try
        {
            if (!await IsOnlineAsync())
            {
                _logger.LogWarning("Cannot sync - offline");
                return;
            }
            
            // Our new Firebase implementation handles sync automatically
            // This is a no-op since Firestore handles offline sync automatically
            _logger.LogInformation("Data synchronization completed (automatic with Firestore)");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data synchronization failed");
            throw;
        }
    }

    public async Task<bool> HasPendingSyncAsync()
    {
        try
        {
            // Our new Firebase implementation handles sync automatically
            // Firestore manages pending sync internally
            return await Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check pending sync status");
            return false;
        }
    }
    
    /// <summary>
    /// Gets the current authenticated user
    /// </summary>
    public FlockForgeUser? CurrentUser
    {
        get
        {
            try
            {
                var user = _authenticationService.CurrentUser;
                if (user != null)
                {
                    return new FlockForgeUser
                    {
                        FirebaseUid = user.Id,
                        Email = user.Email ?? "",
                        DisplayName = user.DisplayName ?? "User",
                        Provider = AuthProvider.EmailPassword, // Default
                        IsEmailVerified = user.IsEmailVerified
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current user");
                return null;
            }
        }
    }
    
    /// <summary>
    /// Checks if the user can work offline with a valid token
    /// </summary>
    public Task<bool> CanWorkOfflineAsync()
    {
        try
        {
            // Our new implementation always supports offline work
            return Task.FromResult(_authenticationService.IsAuthenticated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check offline capability");
            return Task.FromResult(false);
        }
    }
    
    /// <summary>
    /// Generates an offline token for the current user
    /// </summary>
    public string GenerateOfflineToken()
    {
        // Generate a simple offline token
        return Guid.NewGuid().ToString("N");
    }
    
    /// <summary>
    /// Hashes a token for secure storage
    /// </summary>
    public string HashToken(string token)
    {
        // Simple hash implementation
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashedBytes);
    }
}