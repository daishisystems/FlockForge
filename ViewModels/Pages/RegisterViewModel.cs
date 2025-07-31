using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlockForge.Models.Authentication;
using FlockForge.Services.Firebase;
using FlockForge.ViewModels.Base;
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
    
    public RegisterViewModel(IFirebaseService firebaseService)
    {
        _firebaseService = firebaseService;
        Title = "Create Account";
    }
    
    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (!ValidateInput())
            return;
        
        await ExecuteAsync(async (cancellationToken) =>
        {
            BusyMessage = "Creating account...";
            
            try
            {
                // Register with Firebase using the actual service method
                var result = await _firebaseService.RegisterAsync(Email, Password, DisplayName);
                
                if (result.Success)
                {
                    // Navigate to main application or profile completion
                    await Shell.Current.GoToAsync("//MainPage");
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "Registration failed";
                    HasError = true;
                }
            }
            catch (Exception)
            {
                ErrorMessage = "An error occurred during registration. Please try again.";
                HasError = true;
            }
        }, "Creating account...");
    }
    
    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Please enter your email address";
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
        await Shell.Current.GoToAsync("//LoginPage");
    }
}