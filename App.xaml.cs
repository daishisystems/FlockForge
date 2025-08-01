using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FlockForge.Core.Interfaces;
using FlockForge.Core.Models;
using FlockForge.Services.Firebase;
using FlockForge.Views.Pages;

namespace FlockForge
{
    public partial class App : Application
    {
        private readonly IAuthenticationService _authService;
        private readonly IDataService _dataService;
        private readonly ILogger<App> _logger;
        private readonly IServiceProvider _serviceProvider;
        private IDisposable? _authSubscription;
        private readonly SemaphoreSlim _navigationLock = new(1, 1);
        private volatile bool _isNavigating;
        
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            
            _serviceProvider = serviceProvider;
            _authService = serviceProvider.GetRequiredService<IAuthenticationService>();
            _dataService = serviceProvider.GetRequiredService<IDataService>();
            _logger = serviceProvider.GetRequiredService<ILogger<App>>();
            
            // Set up global exception handlers
            SetupExceptionHandlers();
            
            // Subscribe to auth state changes
            _authSubscription = _authService.AuthStateChanged.Subscribe(OnAuthStateChanged);
            
            // Set initial page based on auth state
            SetMainPage(_authService.IsAuthenticated);
        }
        
        private void SetupExceptionHandlers()
        {
            // Handle unobserved task exceptions
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                _logger.LogError(args.Exception, "Unobserved task exception");
                args.SetObserved();
            };
            
            // Handle domain exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var exception = args.ExceptionObject as Exception;
                _logger.LogCritical(exception, "Unhandled domain exception");
                
                // Try to save critical data before crash
                Task.Run(async () =>
                {
                    try
                    {
                        await SaveCriticalDataAsync();
                    }
                    catch { }
                });
            };
            
            // Platform-specific crash handlers
#if ANDROID
            Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
            {
                _logger.LogCritical(args.Exception, "Android unhandled exception");
                args.Handled = true;
            };
#elif IOS
            ObjCRuntime.Runtime.MarshalManagedException += (sender, args) =>
            {
                _logger.LogCritical(args.Exception, "iOS unhandled exception");
            };
#endif
        }
        
        private async Task SaveCriticalDataAsync()
        {
            try
            {
                // Save any pending data
                Preferences.Set("app_crashed", true);
                Preferences.Set("crash_time", DateTime.UtcNow.ToString("O"));
                
                // Force a sync attempt if online
                if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                {
                    _logger.LogInformation("Attempting emergency sync before crash");
                    // Add actual sync logic here if needed
                }
                
                await Task.CompletedTask; // Ensure method is properly async
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save critical data");
            }
        }
        
        private async void OnAuthStateChanged(User? user)
        {
            if (_isNavigating) return;
            
            if (!await _navigationLock.WaitAsync(5000))
            {
                _logger.LogWarning("Navigation lock timeout");
                return;
            }
            
            try
            {
                _isNavigating = true;
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SetMainPage(user != null);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during navigation");
            }
            finally
            {
                _isNavigating = false;
                _navigationLock.Release();
            }
        }
        
        private void SetMainPage(bool isAuthenticated)
        {
            try
            {
                // Always use AppShell for consistent navigation
                if (MainPage is not AppShell)
                {
                    MainPage = new AppShell();
                }
                
                // Use MainThread to ensure Shell is ready, then navigate
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        // Wait a moment for Shell to initialize
                        await Task.Delay(100);
                        
                        // Navigate to appropriate page based on authentication state
                        if (isAuthenticated)
                        {
                            // Navigate to main page
                            await Shell.Current.GoToAsync("///MainPage");
                        }
                        else
                        {
                            // Navigate to login page
                            await Shell.Current.GoToAsync("///LoginPage");
                        }
                    }
                    catch (Exception navEx)
                    {
                        _logger.LogError(navEx, "Error during Shell navigation");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting main page");
                // Fallback to AppShell with error handling - maintain Shell architecture
                try
                {
                    MainPage = new AppShell();
                    // Try to navigate to login page as safe fallback
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Task.Delay(200); // Give more time for Shell initialization
                        try
                        {
                            await Shell.Current.GoToAsync("///LoginPage");
                        }
                        catch (Exception navEx)
                        {
                            _logger.LogError(navEx, "Error during fallback navigation");
                        }
                    });
                }
                catch (Exception shellEx)
                {
                    _logger.LogCritical(shellEx, "Critical error: Cannot create AppShell");
                    // Last resort fallback - but still try to maintain some structure
                    MainPage = new NavigationPage(new ContentPage
                    {
                        Title = "FlockForge - Critical Error",
                        Content = new Label
                        {
                            Text = "Application error occurred. Please restart the app.",
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center
                        }
                    });
                }
            }
        }
        
        protected override void OnStart()
        {
            base.OnStart();
            
            // Check if app crashed last time
            if (Preferences.Get("app_crashed", false))
            {
                var crashTime = Preferences.Get("crash_time", string.Empty);
                _logger.LogWarning("App recovered from crash at {CrashTime}", crashTime);
                
                Preferences.Remove("app_crashed");
                Preferences.Remove("crash_time");
                
                // Attempt recovery
                Task.Run(async () =>
                {
                    await Task.Delay(1000); // Give Firebase time to initialize
                    
                    if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                    {
                        await _authService.RefreshTokenAsync();
                    }
                });
            }
        }
        
        protected override void OnSleep()
        {
            base.OnSleep();
            
            // Clean up listeners to prevent memory leaks
            try
            {
                // The FirestoreService handles cleanup automatically
                _logger.LogDebug("App entering sleep mode - cleanup handled by services");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during app sleep cleanup");
            }
        }
        
        protected override void OnResume()
        {
            base.OnResume();
            
            // Attempt token refresh on resume if online
            Task.Run(async () =>
            {
                try
                {
                    if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                    {
                        await _authService.RefreshTokenAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to refresh token on resume");
                }
            });
        }
        
        ~App()
        {
            _authSubscription?.Dispose();
            _navigationLock?.Dispose();
        }
    }
}
