using System.Security;
using System.Runtime.InteropServices;

namespace FlockForge.Models.Authentication;

/// <summary>
/// Secure wrapper for handling passwords and sensitive data
/// </summary>
public sealed class SecureCredentials : IDisposable
{
    private SecureString? _password;
    private bool _disposed;
    
    public string Email { get; }
    
    public SecureCredentials(string email, SecureString password)
    {
        Email = email ?? throw new ArgumentNullException(nameof(email));
        _password = password ?? throw new ArgumentNullException(nameof(password));
    }
    
    /// <summary>
    /// Executes an action with the decrypted password
    /// </summary>
    public async Task<T> UsePasswordAsync<T>(Func<string, Task<T>> action)
    {
        if (_disposed || _password == null)
            throw new ObjectDisposedException(nameof(SecureCredentials));
        
        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.SecureStringToGlobalAllocUnicode(_password);
            var password = Marshal.PtrToStringUni(ptr) ?? string.Empty;
            return await action(password).ConfigureAwait(false);
        }
        finally
        {
            if (ptr != IntPtr.Zero)
                Marshal.ZeroFreeGlobalAllocUnicode(ptr);
        }
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _password?.Dispose();
            _password = null;
            _disposed = true;
        }
    }
}