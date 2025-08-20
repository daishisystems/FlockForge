using Microsoft.Maui.Controls;
using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using FlockForge.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Maui;                    // HandlerChangingEventArgs
using Microsoft.Maui.Controls;          // Shell, GoToAsync, events
using Microsoft.Maui.ApplicationModel;  // MainThread
using FlockForge.Views.Pages;

namespace FlockForge;

public partial class AppShell : Shell
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AppShell> _logger;

    private readonly CompositeDisposable _bag = new();
    private IDisposable? _authSub;
    private bool _loaded;
    public ICommand GoToCommand { get; }

    public AppShell(IAuthenticationService authService, ILogger<AppShell> logger)
    {
        GoToCommand = new Command<string>(async route =>
        {
            try
            {
                await GoToAsync(route);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed navigation to {Route}", route);
            }
        });

        InitializeComponent();
        _authService = authService;
        _logger = logger;

        var regs = Routing.GetRegisteredRoutes().ToHashSet();
        void Reg(string route, Type pageType)
        {
            if (!regs.Contains(route))
                Routing.RegisterRoute(route, pageType);
        }

        Reg("profile", typeof(ProfilePage));
        Reg("settings", typeof(SettingsPage));
        Reg("farms", typeof(FarmsPage));
        Reg("groups", typeof(GroupsPage));
        Reg("breeding", typeof(BreedingPage));
        Reg("scanning", typeof(ScanningPage));
        Reg("lambing", typeof(LambingPage));
        Reg("weaning", typeof(WeaningPage));
        Reg("reports", typeof(ReportsPage));

        // NEW: auth routes (keeps login/register reachable)
        Reg("login", typeof(LoginPage));
        Reg("register", typeof(RegisterPage));

        // Wire events once
        Loaded += OnLoadedOnce;
        Navigating += OnShellNavigating;

        // Subscribe to auth changes for lifetime of Shell
        _authSub = _authService.AuthStateChanged.Subscribe(OnAuthStateChanged);
        _bag.Add(_authSub);

        // Fallback tick (Android sometimes fires Loaded late)
        _ = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Task.Delay(100);
            if (!_loaded && CurrentItem == null)
            {
                _logger.LogDebug("Fallback: Setting initial route");
                SetInitialRoute();
            }
        });
    }

    private void OnLoadedOnce(object? sender, EventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        Loaded -= OnLoadedOnce;

        try
        {
            _logger.LogDebug("Shell loaded, setting initial route");
            SetInitialRoute();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnLoadedOnce");
        }
    }

    // Pre-navigation cleanup
    private void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        try
        {
            if (Current?.CurrentPage is FlockForge.Views.Base.BaseContentPage page)
            {
                page.Disposables.Clear();
                (page.BindingContext as FlockForge.ViewModels.Base.BaseViewModel)?.OnDisappearing();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pre-navigation cleanup");
        }
    }

    private void SetInitialRoute()
    {
        try
        {
            _logger.LogDebug("SetInitialRoute called - IsAuthenticated: {IsAuthenticated}", _authService.IsAuthenticated);

            if (_authService.IsAuthenticated)
                ShowMainApplication();
            else
                ShowAuthenticationFlow();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SetInitialRoute");
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try { await GoToAsync("login"); }
                catch (Exception navEx) { _logger.LogError(navEx, "Failed to navigate to login as fallback"); }
            });
        }
    }

    private void OnAuthStateChanged(FlockForge.Core.Models.User? user)
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
            if (MainTabBar != null) MainTabBar.IsVisible = true;
            else _logger.LogWarning("MainTabBar is null in ShowMainApplication");

            FlyoutBehavior = FlyoutBehavior.Flyout;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    _logger.LogDebug("Navigating to dashboard");
                    await GoToAsync("//dashboard");
                    _logger.LogDebug("Successfully navigated to dashboard");
                }
                catch (Exception ex) { _logger.LogError(ex, "Error navigating to dashboard"); }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ShowMainApplication");
            throw;
        }
    }

    private void ShowAuthenticationFlow()
    {
        try
        {
            if (MainTabBar != null) MainTabBar.IsVisible = false;
            else _logger.LogWarning("MainTabBar is null in ShowAuthenticationFlow");

            FlyoutBehavior = FlyoutBehavior.Disabled;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    _logger.LogDebug("Navigating to login");
                    await GoToAsync("login");
                    _logger.LogDebug("Successfully navigated to login");
                }
                catch (Exception ex) { _logger.LogError(ex, "Error navigating to login page"); }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ShowAuthenticationFlow");
            throw;
        }
    }

    // Detach transient events; don’t tear down the auth sub here
    protected override void OnDisappearing()
    {
        Loaded -= OnLoadedOnce;
        Navigating -= OnShellNavigating;
        base.OnDisappearing();
    }

    // Final cleanup when the control is being torn down
    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        if (args.NewHandler is null)
        {
            _authSub?.Dispose();
            _bag.Dispose();
        }
        base.OnHandlerChanging(args);
    }
}
