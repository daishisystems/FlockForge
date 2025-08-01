using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlockForge.Models.Authentication;
using FlockForge.Services.Firebase;
using FlockForge.ViewModels.Base;
using FlockForge.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security;

namespace FlockForge.ViewModels.Pages;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IFirebaseService _firebaseService;
    
    [ObservableProperty]
    private string _email = string.Empty;
    
    [ObservableProperty]
    private string _password = string.Empty;
    
    [ObservableProperty]
    private bool _isRememberMe;
    
    [ObservableProperty]
    private string _title = "Sign In";
    
    [ObservableProperty]
    private string? _busyMessage;
    
    [ObservableProperty]
    private bool _hasError;
    
    public LoginViewModel(
        IFirebaseService firebaseService,
        IAuthenticationService authService,
        IDataService dataService,
        IConnectivity connectivity,
        ILogger<LoginViewModel> logger)
        : base(authService, dataService, connectivity, logger)
    {
        _firebaseService = firebaseService;
        Title = "Sign In";
    }
    
    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Please enter your email address";
            HasError = true;
            return;
        }
        
        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter your password";
            HasError = true;
            return;
        }
        
        await ExecuteSafelyAsync(async (cancellationToken) =>
        {
            BusyMessage = "Signing in...";
            
            // Authenticate with Firebase using the actual service method
            var result = await _firebaseService.AuthenticateAsync(Email, Password);
            
            if (result.Success)
            {
                // Navigate to main application
                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Login failed";
                HasError = true;
            }
        }, "An error occurred during login. Please try again.");
    }
    
    [RelayCommand]
    private async Task LoginWithGoogleAsync()
    {
        await ExecuteSafelyAsync(async (cancellationToken) =>
        {
            BusyMessage = "Signing in with Google...";
            
            // Authenticate with Google using the actual service method
            var result = await _firebaseService.AuthenticateWithGoogleAsync();
            
            if (result.Success)
            {
                // Navigate to main application
                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Google login failed";
                HasError = true;
            }
        }, "An error occurred during Google login. Please try again.");
    }
    
    [RelayCommand]
    private async Task RegisterAsync()
    {
        // Navigate to registration page
        await Shell.Current.GoToAsync("RegisterPage");
    }
    
    [RelayCommand]
    private async Task ResetPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Please enter your email address to reset your password";
            HasError = true;
            return;
        }
        
        await ExecuteSafelyAsync(async (cancellationToken) =>
        {
            BusyMessage = "Sending password reset email...";
            
            // Request password reset using the actual service method
            await _firebaseService.RequestPasswordResetAsync(Email);
            
            await Shell.Current.DisplayAlert(
                "Password Reset",
                "A password reset email has been sent to your email address.",
                "OK");
        }, "Failed to send password reset email. Please try again.");
    }
}