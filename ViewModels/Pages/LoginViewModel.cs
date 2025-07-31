using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlockForge.Models.Authentication;
using FlockForge.Services.Firebase;
using FlockForge.ViewModels.Base;
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
    
    public LoginViewModel(IFirebaseService firebaseService)
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
        
        await ExecuteAsync(async (cancellationToken) =>
        {
            BusyMessage = "Signing in...";
            
            try
            {
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
            }
            catch (Exception)
            {
                ErrorMessage = "An error occurred during login. Please try again.";
                HasError = true;
            }
        }, "Signing in...");
    }
    
    [RelayCommand]
    private async Task LoginWithGoogleAsync()
    {
        await ExecuteAsync(async (cancellationToken) =>
        {
            BusyMessage = "Signing in with Google...";
            
            try
            {
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
            }
            catch (Exception)
            {
                ErrorMessage = "An error occurred during Google login. Please try again.";
                HasError = true;
            }
        }, "Signing in with Google...");
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
        
        await ExecuteAsync(async (cancellationToken) =>
        {
            BusyMessage = "Sending password reset email...";
            
            try
            {
                // Request password reset using the actual service method
                await _firebaseService.RequestPasswordResetAsync(Email);
                
                await Shell.Current.DisplayAlert(
                    "Password Reset",
                    "A password reset email has been sent to your email address.",
                    "OK");
            }
            catch (Exception)
            {
                ErrorMessage = "Failed to send password reset email. Please try again.";
                HasError = true;
            }
        }, "Sending password reset email...");
    }
}