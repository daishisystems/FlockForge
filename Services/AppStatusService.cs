using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Networking;
using FlockForge.Core.Interfaces;

namespace FlockForge.Services;

public sealed class AppStatusService : INotifyPropertyChanged, IDisposable
{
    public static AppStatusService Instance { get; } = new();

    string _syncLabel = "Online";
    public string SyncLabel
    {
        get => _syncLabel;
        set { if (_syncLabel != value) { _syncLabel = value; OnPropertyChanged(); } }
    }

    string _currentUserName = "Guest";
    public string CurrentUserName
    {
        get => _currentUserName;
        set { if (_currentUserName != value) { _currentUserName = value; OnPropertyChanged(); } }
    }

    private readonly EventHandler<ConnectivityChangedEventArgs> _handler;
    private IDisposable? _authSubscription;

    private AppStatusService()
    {
        _handler = OnConnectivityChanged;
        UpdateOnline();
        Connectivity.ConnectivityChanged += _handler;
    }

    public void InitializeWithAuthService(IAuthenticationService authService)
    {
        // Update current user name immediately
        UpdateCurrentUserName(authService.CurrentUser);
        
        // Subscribe to auth state changes
        _authSubscription?.Dispose();
        _authSubscription = authService.AuthStateChanged.Subscribe(user => UpdateCurrentUserName(user));
    }

    private void UpdateCurrentUserName(Core.Models.User? user)
    {
        if (user != null)
        {
            // Use DisplayName if available, otherwise use email prefix, or fallback to "User"
            CurrentUserName = !string.IsNullOrWhiteSpace(user.DisplayName) 
                ? user.DisplayName 
                : (!string.IsNullOrWhiteSpace(user.Email) 
                    ? user.Email.Split('@')[0] 
                    : "User");
        }
        else
        {
            CurrentUserName = "Guest";
        }
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e) => UpdateOnline();

    void UpdateOnline()
    {
        var online = Connectivity.NetworkAccess == NetworkAccess.Internet;
        SyncLabel = online ? "Online" : "Offline";
    }

    public void Dispose()
    {
        Connectivity.ConnectivityChanged -= _handler;
        _authSubscription?.Dispose();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
