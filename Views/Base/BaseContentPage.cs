using FlockForge.ViewModels.Base;
using FlockForge.Services.Platform;
using Microsoft.Extensions.Logging;

namespace FlockForge.Views.Base;

public abstract class BaseContentPage<TViewModel> : ContentPage
    where TViewModel : BaseViewModel
{
    private ILogger? _logger;
    private IPlatformMemoryService? _memoryService;
    private bool _isAppearing;
    private bool _disposed;

    protected BaseContentPage(TViewModel viewModel)
    {
        BindingContext = viewModel;
        
        // Try to get services from DI container
        try
        {
            var serviceProvider = Handler?.MauiContext?.Services;
            _logger = serviceProvider?.GetService<ILogger<BaseContentPage<TViewModel>>>();
            _memoryService = serviceProvider?.GetService<IPlatformMemoryService>();
        }
        catch
        {
            // Services not available, continue without them
        }
        
        SetupPage();
        RegisterMemoryPressureCallback();
    }

    protected TViewModel ViewModel => (TViewModel)BindingContext;

    protected virtual void SetupPage()
    {
        // Configure rugged field UI defaults
        Shell.SetNavBarIsVisible(this, true);
        Shell.SetTabBarIsVisible(this, true);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        if (_disposed)
            return;
            
        _isAppearing = true;
        
        try
        {
            // Only refresh if the ViewModel supports it and we're not already busy
            if (ViewModel.RefreshCommand.CanExecute(null) && !ViewModel.IsBusy)
            {
                ViewModel.RefreshCommand.Execute(null);
            }
            
            _logger?.LogDebug("Page {PageType} appeared", GetType().Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during page appearing for {PageType}", GetType().Name);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        _isAppearing = false;
        
        try
        {
            // Dispose the ViewModel if it implements IDisposable and hasn't been disposed
            if (ViewModel is IDisposable disposableViewModel && !_disposed)
            {
                Dispose();
            }

            _logger?.LogDebug("Page {PageType} disappeared", GetType().Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during page disappearing for {PageType}", GetType().Name);
        }
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        
        try
        {
            _logger?.LogDebug("Navigated to page {PageType}", GetType().Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during navigation to {PageType}", GetType().Name);
        }
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        
        try
        {
            // Perform cleanup when navigating away
            if (!_isAppearing)
            {
                PerformCleanup();
            }
            
            _logger?.LogDebug("Navigated from page {PageType}", GetType().Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during navigation from {PageType}", GetType().Name);
        }
    }

    private void RegisterMemoryPressureCallback()
    {
        try
        {
            _memoryService?.RegisterMemoryPressureCallback(OnMemoryPressure);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to register memory pressure callback");
        }
    }

    private void OnMemoryPressure(MemoryPressureLevel level)
    {
        try
        {
            _logger?.LogInformation("Memory pressure detected: {Level} on page {PageType}", level, GetType().Name);
            
            // Perform page-specific memory cleanup
            OnMemoryPressureDetected(level);
            
            // If critical, force cleanup
            if (level == MemoryPressureLevel.Critical)
            {
                PerformCleanup();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling memory pressure on page {PageType}", GetType().Name);
        }
    }

    protected virtual void OnMemoryPressureDetected(MemoryPressureLevel level)
    {
        // Override in derived classes for specific memory cleanup
    }

    protected virtual void PerformCleanup()
    {
        try
        {
            // Clear any cached data, images, etc.
            // This is a base implementation - override in derived classes
            GC.Collect(0, GCCollectionMode.Optimized);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during cleanup for page {PageType}", GetType().Name);
        }
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        
        // Re-register services if handler changed
        if (Handler?.MauiContext?.Services != null)
        {
            try
            {
                var serviceProvider = Handler.MauiContext.Services;
                
                // Update service references if they're null
                _logger ??= serviceProvider.GetService<ILogger<BaseContentPage<TViewModel>>>();
                _memoryService ??= serviceProvider.GetService<IPlatformMemoryService>();
                
                // Re-register memory pressure callback with updated service
                RegisterMemoryPressureCallback();
            }
            catch (Exception ex)
            {
                // Log if we have a logger, otherwise silently continue
                _logger?.LogError(ex, "Error re-registering services after handler change");
            }
        }
    }

    ~BaseContentPage()
    {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                try
                {
                    if (ViewModel is IDisposable disposableViewModel)
                    {
                        disposableViewModel.Dispose();
                    }

                    _memoryService?.UnregisterMemoryPressureCallback(OnMemoryPressure);
                    PerformCleanup();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error during disposal of page {PageType}", GetType().Name);
                }
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