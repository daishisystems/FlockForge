using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace FlockForge.Services;

public sealed class NavigationService : INotifyPropertyChanged
{
    public static NavigationService Instance { get; } = new();

    public ObservableCollection<NavItem> Items { get; } = new();

    public ICommand GoHomeCommand { get; }
    public ICommand GoAnimalsCommand { get; }
    public ICommand GoReportsCommand { get; }

    bool _hasItems;
    public bool HasItems
    {
        get => _hasItems;
        private set { if (_hasItems != value) { _hasItems = value; OnPropertyChanged(); } }
    }

    private NavigationService()
    {
        GoHomeCommand    = new Command(async () => { try { await Shell.Current.GoToAsync("//home");    } catch { /* no-op; optionally log */ } });
        GoAnimalsCommand = new Command(async () => { try { await Shell.Current.GoToAsync("//animals"); } catch { /* no-op; optionally log */ } });
        GoReportsCommand = new Command(async () => { try { await Shell.Current.GoToAsync("//reports"); } catch { /* no-op; optionally log */ } });

        Items.CollectionChanged += (_, __) => HasItems = Items.Count > 0;
        HasItems = Items.Count > 0;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class NavItem
{
    public string Text { get; init; } = "";
    public ICommand? Command { get; init; }
}
