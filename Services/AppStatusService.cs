using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Networking;

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

    private readonly EventHandler<ConnectivityChangedEventArgs> _handler;

    private AppStatusService()
    {
        _handler = OnConnectivityChanged;
        UpdateOnline();
        Connectivity.ConnectivityChanged += _handler;
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
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
