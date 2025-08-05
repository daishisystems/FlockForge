using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlockForge.Models.Authentication;
using FlockForge.Services.Firebase;
using FlockForge.ViewModels.Base;
using FlockForge.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security;

namespace FlockForge.ViewModels.Pages;

public partial class RegisterViewModel : BaseViewModel
{
    private readonly IFirebaseService _firebaseService;
    
    [ObservableProperty]
    private string _email = string.Empty;
    
    [ObservableProperty]
    private string _password = string.Empty;
    
    [ObservableProperty]
    private string _confirmPassword = string.Empty;
    
    [ObservableProperty]
    private string _displayName = string.Empty;
    
    [ObservableProperty]
    private string _title = "Create Account";
    
    [ObservableProperty]
    private string? _busyMessage;
    
    [ObservableProperty]
    private bool _hasError;
    
    public RegisterViewModel(
        IFirebaseService firebaseService,
        IAuthenticationService authService,
        IDataService dataService,
        IConnectivity connectivity,
        ILogger<RegisterViewModel> logger)
        : base(authService, dataService, connectivity, logger)
    {
        _firebaseService = firebaseService;
        Title = "Create Account";
    }
    
    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (!ValidateInput())
            return;
        
        await ExecuteSafelyAsync(async (cancellationToken) =>
        {
            BusyMessage = "Creating account...";
            
            // Register with Firebase using the actual service method
            var result = await _firebaseService.RegisterAsync(Email, Password, DisplayName);
            
            if (result.Success)
            {
                // Navigate directly to main application since email verification is no longer required
                await Shell.Current.GoToAsync("//dashboard");
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Registration failed";
                HasError = true;
            }
        }, "An error occurred during registration. Please try again.");
    }
    
    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        try
        {
            // Use .NET's built-in email validation
            var addr = new System.Net.Mail.MailAddress(email.Trim());
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }
    
    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Please enter your email address";
            HasError = true;
            return false;
        }
        
        if (!IsValidEmail(Email))
        {
            ErrorMessage = "Please enter a valid email address";
            HasError = true;
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter a password";
            HasError = true;
            return false;
        }
        
        if (Password.Length < 6)
        {
            ErrorMessage = "Password must be at least 6 characters long";
            HasError = true;
            return false;
        }
        
        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match";
            HasError = true;
            return false;
        }
        
        HasError = false;
        ErrorMessage = null;
        return true;
    }
    
    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("//login");
    }
}