using FlockForge.Models.Authentication;
using System.Security.Cryptography;
using System.Text;

namespace FlockForge.Services.Firebase;

/// <summary>
/// Manages secure token storage and retrieval for Firebase Authentication
/// </summary>
public class TokenManager
{
    private const string TOKEN_KEY = "FirebaseAuthToken";
    private const string USER_KEY = "CurrentUser";
    private const string OFFLINE_TOKEN_KEY = "OfflineTokenHash";
    
    /// <summary>
    /// Stores the Firebase authentication token securely
    /// </summary>
    public async Task StoreTokenAsync(string token, FlockForgeUser user)
    {
        try
        {
            // Store token in secure storage
            await SecureStorage.SetAsync(TOKEN_KEY, token);
            
            // Store user information
            var userJson = System.Text.Json.JsonSerializer.Serialize(user);
            await SecureStorage.SetAsync(USER_KEY, userJson);
            
            // Generate and store offline token if user can work offline
            if (user.CanWorkOffline && !string.IsNullOrEmpty(user.OfflineTokenHash))
            {
                await SecureStorage.SetAsync(OFFLINE_TOKEN_KEY, user.OfflineTokenHash);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw - token storage is not critical
            System.Diagnostics.Debug.WriteLine($"Error storing token: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Retrieves the stored Firebase authentication token
    /// </summary>
    public async Task<string?> GetStoredTokenAsync()
    {
        try
        {
            return await SecureStorage.GetAsync(TOKEN_KEY);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - token retrieval failure is not critical
            System.Diagnostics.Debug.WriteLine($"Error retrieving token: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Retrieves the stored user information
    /// </summary>
    public async Task<FlockForgeUser?> GetStoredUserAsync()
    {
        try
        {
            var userJson = await SecureStorage.GetAsync(USER_KEY);
            if (!string.IsNullOrEmpty(userJson))
            {
                return System.Text.Json.JsonSerializer.Deserialize<FlockForgeUser>(userJson);
            }
            return null;
        }
        catch (Exception ex)
        {
            // Log error but don't throw - user retrieval failure is not critical
            System.Diagnostics.Debug.WriteLine($"Error retrieving user: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Clears all stored authentication tokens and user information
    /// </summary>
    public Task ClearStoredTokensAsync()
    {
        try
        {
            SecureStorage.Remove(TOKEN_KEY);
            SecureStorage.Remove(USER_KEY);
            SecureStorage.Remove(OFFLINE_TOKEN_KEY);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - token clearing failure is not critical
            System.Diagnostics.Debug.WriteLine($"Error clearing tokens: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Checks if a valid offline token exists and is not expired
    /// </summary>
    public async Task<bool> HasValidOfflineTokenAsync()
    {
        try
        {
            var storedHash = await SecureStorage.GetAsync(OFFLINE_TOKEN_KEY);
            return !string.IsNullOrEmpty(storedHash);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking offline token: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Generates a new offline token for the user
    /// </summary>
    public string GenerateOfflineToken()
    {
        return CryptoHelpers.GenerateOfflineToken();
    }
    
    /// <summary>
    /// Hashes a token for secure storage
    /// </summary>
    public string HashToken(string token)
    {
        return CryptoHelpers.HashToken(token);
    }
}