using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FlockForge.ViewModels.Base;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isOffline;

    [RelayCommand]
    protected virtual async Task RefreshAsync()
    {
        // Override in derived classes
        await Task.CompletedTask;
    }
}