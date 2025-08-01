using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using FlockForge.Core.Interfaces;

namespace FlockForge.ViewModels.Base
{
    public abstract partial class BaseViewModel : ObservableObject, IDisposable
    {
        protected readonly IAuthenticationService AuthService;
        protected readonly IDataService DataService;
        protected readonly ILogger Logger;
        
        private readonly List<IDisposable> _subscriptions = new();
        private readonly SemaphoreSlim _operationLock = new(1, 1);
        private CancellationTokenSource? _cancellationTokenSource;
        
        [ObservableProperty]
        private bool isBusy;
        
        [ObservableProperty]
        private string? errorMessage;
        
        [ObservableProperty]
        private bool isOffline;
        
        protected bool IsDisposed { get; private set; }
        
        public IRelayCommand RefreshCommand { get; }
        
        protected BaseViewModel(
            IAuthenticationService authService,
            IDataService dataService,
            IConnectivity connectivity,
            ILogger logger)
        {
            AuthService = authService;
            DataService = dataService;
            Logger = logger;
            
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Initialize commands
            RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
            
            // Monitor connectivity with weak event handler
            connectivity.ConnectivityChanged += OnConnectivityChanged;
            IsOffline = connectivity.NetworkAccess != NetworkAccess.Internet;
            
            // Update command can execute state when IsBusy changes
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(IsBusy))
                {
                    RefreshCommand.NotifyCanExecuteChanged();
                }
            };
        }
        
        private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            IsOffline = e.NetworkAccess != NetworkAccess.Internet;
        }
        
        protected async Task ExecuteSafelyAsync(
            Func<CancellationToken, Task> operation, 
            string? errorMessage = null,
            int timeoutMs = 30000)
        {
            if (IsDisposed) return;
            
            if (!await _operationLock.WaitAsync(100))
            {
                Logger.LogWarning("Operation already in progress");
                return;
            }
            
            try
            {
                IsBusy = true;
                ErrorMessage = null;
                
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource!.Token);
                cts.CancelAfter(timeoutMs);
                
                await operation(cts.Token);
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "Operation timed out";
                Logger.LogWarning("Operation timed out");
            }
            catch (Exception ex)
            {
                ErrorMessage = errorMessage ?? "An error occurred";
                Logger.LogError(ex, "Operation failed");
            }
            finally
            {
                IsBusy = false;
                _operationLock.Release();
            }
        }
        
        protected virtual async Task RefreshAsync()
        {
            // Default implementation - override in derived classes
            await Task.CompletedTask;
        }
        
        protected void RegisterSubscription(IDisposable subscription)
        {
            _subscriptions.Add(subscription);
        }
        
        public virtual void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            
            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                
                foreach (var subscription in _subscriptions)
                {
                    subscription?.Dispose();
                }
                _subscriptions.Clear();
                
                _operationLock?.Dispose();
                
                // Unsubscribe from events
                if (Connectivity.Current != null)
                {
                    Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during view model disposal");
            }
        }
    }
}