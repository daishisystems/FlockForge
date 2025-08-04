using FlockForge.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlockForge;

public partial class AppShell : Shell
{
	private readonly IAuthenticationService _authService;
	private readonly ILogger<AppShell> _logger;
	private IDisposable? _authStateSubscription;

	public AppShell(IAuthenticationService authService, ILogger<AppShell> logger)
	{
		InitializeComponent();
		
		_authService = authService;
		_logger = logger;
		
		// Subscribe to authentication state changes
		_authStateSubscription = _authService.AuthStateChanged.Subscribe(OnAuthStateChanged);
		
		// Defer initial route setting until after the Shell is fully loaded
		Loaded += OnShellLoaded;
	}

	private void OnShellLoaded(object? sender, EventArgs e)
	{
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
		if (_authService.IsAuthenticated)
		{
			ShowMainApplication();
		}
		else
		{
			ShowAuthenticationFlow();
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
					await GoToAsync("//dashboard");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error navigating to dashboard");
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
					await GoToAsync("//login");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error navigating to login page");
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
		base.OnDisappearing();
		_authStateSubscription?.Dispose();
	}
}

