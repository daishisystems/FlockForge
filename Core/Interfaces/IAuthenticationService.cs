using System;
using System.Threading;
using System.Threading.Tasks;
using FlockForge.Core.Models;

namespace FlockForge.Core.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AuthResult> SignInWithEmailPasswordAsync(string email, string password, CancellationToken cancellationToken = default);
        Task<AuthResult> SignUpWithEmailPasswordAsync(string email, string password, CancellationToken cancellationToken = default);
        Task<AuthResult> SignInWithGoogleAsync(CancellationToken cancellationToken = default);
        Task<AuthResult> SignInWithAppleAsync(CancellationToken cancellationToken = default);
        Task<AuthResult> SignInWithMicrosoftAsync(CancellationToken cancellationToken = default);
        Task SignOutAsync();
        Task<bool> SendEmailVerificationAsync();
        Task<bool> SendPasswordResetEmailAsync(string email);
        Task<AuthResult> RefreshTokenAsync();
        IObservable<User?> AuthStateChanged { get; }
        User? CurrentUser { get; }
        bool IsAuthenticated { get; }
        bool IsEmailVerified { get; }
    }
}