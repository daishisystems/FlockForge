using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Runtime.CompilerServices;

namespace FlockForge.ViewModels.Base;

/// <summary>
/// Base ViewModel with improved busy state management and progress reporting
/// </summary>
public abstract partial class BaseViewModel : ObservableObject, IDisposable
{
    private readonly SemaphoreSlim _busySemaphore = new(1, 1);
    private readonly CancellationTokenSource _disposalCts = new();
    private bool _disposed;
    
    [ObservableProperty]
    private bool _isBusy;
    
    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string _title = string.Empty;
    
    [ObservableProperty]
    private string? _busyMessage;
    
    [ObservableProperty]
    private double _progress;
    
    [ObservableProperty]
    private bool _hasError;
    
    [ObservableProperty]
    private string? _errorMessage;
    
    protected CancellationToken DisposalToken => _disposalCts.Token;
    
    /// <summary>
    /// Executes an action with busy state management and error handling
    /// </summary>
    protected async Task<bool> ExecuteAsync(
        Func<CancellationToken, Task> action,
        string? busyMessage = null,
        [CallerMemberName] string? caller = null)
    {
        if (_disposed) return false;
        
        await _busySemaphore.WaitAsync(DisposalToken);
        try
        {
            if (IsBusy) return false;
            
            HasError = false;
            ErrorMessage = null;
            IsBusy = true;
            BusyMessage = busyMessage;
            Progress = 0;
            
            await action(DisposalToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            // Silently handle cancellation
            return false;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = GetUserFriendlyMessage(ex);
            OnError(ex, caller);
            return false;
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
            Progress = 0;
            _busySemaphore.Release();
        }
    }
    
    /// <summary>
    /// Reports progress for long-running operations
    /// </summary>
    protected void ReportProgress(double value, string? message = null)
    {
        Progress = Math.Clamp(value, 0, 1);
        if (message != null)
            BusyMessage = message;
    }
    
    [RelayCommand]
    protected virtual async Task RefreshAsync()
    {
        if (IsRefreshing) return;
        
        IsRefreshing = true;
        try
        {
            await OnRefreshAsync(DisposalToken);
        }
        finally
        {
            IsRefreshing = false;
        }
    }
    
    protected virtual Task OnRefreshAsync(CancellationToken cancellationToken) 
        => Task.CompletedTask;
    
    protected virtual void OnError(Exception ex, string? caller) 
    {
        // Override for logging
    }
    
    protected virtual string GetUserFriendlyMessage(Exception ex) 
        => "An error occurred. Please try again.";
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _disposalCts.Cancel();
                _disposalCts.Dispose();
                _busySemaphore.Dispose();
            }
            _disposed = true;
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}