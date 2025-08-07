using FlockForge.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlockForge;

public partial class AppShell : Shell
{
        private readonly IAuthenticationService _authService;
        private readonly ILogger<AppShell> _logger;
        private IDisposable? _authStateSubscription;
        private bool _isShellLoaded;
        private bool _disposed;

	public AppShell(IAuthenticationService authService, ILogger<AppShell> logger)
	{
		InitializeComponent();
		
		_authService = authService;
		_logger = logger;
		
		// Subscribe to authentication state changes
		_authStateSubscription = _authService.AuthStateChanged.Subscribe(OnAuthStateChanged);
		
		// Defer initial route setting until after the Shell is fully loaded
                Loaded += OnShellLoaded;
		
		// Also try immediate initialization as fallback for Android
		Task.Run(async () =>
		{
			await Task.Delay(100); // Small delay to ensure Shell is ready
			MainThread.BeginInvokeOnMainThread(() =>
			{
				try
				{
					if (CurrentItem == null) // Only set if no navigation has occurred
					{
						_logger?.LogDebug("Fallback: Setting initial route");
						SetInitialRoute();
					}
				}
				catch (Exception ex)
				{
					_logger?.LogError(ex, "Error in fallback initialization");
				}
			});
		});
	}

        private void OnShellLoaded(object? sender, EventArgs e)
        {
                if (_isShellLoaded)
                        return;

                _isShellLoaded = true;
                try
                {
                        _logger?.LogDebug("Shell loaded, setting initial route");
                        // Set initial route based on current auth state
                        SetInitialRoute();
                }
                catch (Exception ex)
                {
                        _logger?.LogError(ex, "Error in OnShellLoaded");
                }
        }

	private void SetInitialRoute()
	{
		try
		{
			_logger?.LogDebug("SetInitialRoute called - IsAuthenticated: {IsAuthenticated}", _authService.IsAuthenticated);
			
			if (_authService.IsAuthenticated)
			{
				ShowMainApplication();
			}
			else
			{
				ShowAuthenticationFlow();
			}
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Error in SetInitialRoute");
			// Fallback to login page
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					await GoToAsync("//login");
				}
				catch (Exception navEx)
				{
					_logger?.LogError(navEx, "Failed to navigate to login as fallback");
				}
			});
		}
	}

	private void OnAuthStateChanged(Core.Models.User? user)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			if (user != null)
			{
				_logger.LogInformation("User authenticated, showing main application");
				ShowMainApplication();
			}
			else
			{
				_logger.LogInformation("User signed out, showing login");
				ShowAuthenticationFlow();
			}
		});
	}

	private void ShowMainApplication()
	{
		try
		{
			// Add null check for MainTabBar
			if (MainTabBar != null)
			{
				MainTabBar.IsVisible = true;
			}
			else
			{
				_logger?.LogWarning("MainTabBar is null in ShowMainApplication");
			}
			
			FlyoutBehavior = FlyoutBehavior.Disabled;
			
			// Navigate to dashboard - use this instead of Shell.Current to avoid null reference
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					_logger?.LogDebug("Navigating to dashboard");
					await GoToAsync("//dashboard");
					_logger?.LogDebug("Successfully navigated to dashboard");
				}
				catch (Exception ex)
				{
					_logger?.LogError(ex, "Error navigating to dashboard");
				}
			});
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Error in ShowMainApplication");
			throw;
		}
	}

	private void ShowAuthenticationFlow()
	{
		try
		{
			// Add null check for MainTabBar
			if (MainTabBar != null)
			{
				MainTabBar.IsVisible = false;
			}
			else
			{
				_logger?.LogWarning("MainTabBar is null in ShowAuthenticationFlow");
			}
			
			FlyoutBehavior = FlyoutBehavior.Disabled;
			
			// Navigate to login - use this instead of Shell.Current to avoid null reference
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					_logger?.LogDebug("Navigating to login");
					await GoToAsync("//login");
					_logger?.LogDebug("Successfully navigated to login");
				}
				catch (Exception ex)
				{
					_logger?.LogError(ex, "Error navigating to login page");
				}
			});
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Error in ShowAuthenticationFlow");
			throw;
		}
	}

        protected override void OnDisappearing()
        {
                if (_disposed)
                        return;

                _disposed = true;

                Loaded -= OnShellLoaded;
                _authStateSubscription?.Dispose();
                base.OnDisappearing();
        }
}

