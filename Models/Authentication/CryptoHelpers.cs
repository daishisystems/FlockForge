using System.Security.Cryptography;
using System.Text;

namespace FlockForge.Models.Authentication;

/// <summary>
/// Crypto helpers for offline token generation and management
/// </summary>
public static class CryptoHelpers
{
    /// <summary>
    /// Default offline validity period for farming apps (generous for rural connectivity)
    /// </summary>
    public const int DefaultOfflineValidityDays = 30;
    
    /// <summary>
    /// Generates a cryptographically secure offline token
    /// </summary>
    /// <returns>Base64-encoded secure random token</returns>
    public static string GenerateOfflineToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
    
    /// <summary>
    /// Creates a SHA256 hash of the token for secure storage
    /// </summary>
    /// <param name="token">The token to hash</param>
    /// <returns>Base64-encoded hash of the token</returns>
    public static string HashToken(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);
        
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
    
    /// <summary>
    /// Verifies a token against its stored hash
    /// </summary>
    /// <param name="token">The token to verify</param>
    /// <param name="storedHash">The stored hash to verify against</param>
    /// <returns>True if the token matches the hash</returns>
    public static bool VerifyToken(string token, string storedHash)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(storedHash))
            return false;
        
        var tokenHash = HashToken(token);
        return tokenHash.Equals(storedHash, StringComparison.Ordinal);
    }
}