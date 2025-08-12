using Foundation;
using UIKit;
using FlockForge.Services.Platform;
using FlockForge.Platforms.iOS.Services;
using Microsoft.Extensions.Logging;
using Firebase.Core;
using FlockForge.Platforms.iOS.Helpers;
using FlockForge.Utilities.Disposal;

namespace FlockForge;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    private ILogger<AppDelegate>? _logger;
    private ObserverManager? _observerManager;
    private IPlatformMemoryService? _memoryService;

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        try
        {
            // Initialize Firebase Core - this must happen before any Firebase services are used
            Firebase.Core.App.Configure();
// Ensure Crashlytics is properly initialized
            if (Firebase.Crashlytics.Crashlytics.SharedInstance != null)
            {
                Firebase.Crashlytics.Crashlytics.SharedInstance.SetCrashlyticsCollectionEnabled(true);
                System.Diagnostics.Debug.WriteLine("✅ Crashlytics initialized successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Crashlytics SharedInstance is null");
            }
            System.Diagnostics.Debug.WriteLine("Firebase initialized successfully for iOS");
            
            // Create the MAUI app first
            var result = base.FinishedLaunching(application, launchOptions);
            
            // Get services from DI container after MAUI app is created
            var mauiApp = IPlatformApplication.Current?.Services;
            if (mauiApp != null)
            {
                _logger = mauiApp.GetService<ILogger<AppDelegate>>();
                _memoryService = mauiApp.GetService<IPlatformMemoryService>();
            }
            
            // Move all heavy initialization off UI thread immediately
            _ = InitializeAsync();
            
            _logger?.LogInformation("iOS AppDelegate finished launching successfully");
            
            return result;
        }
        catch (Exception ex)
        {
            // Use System.Diagnostics.Debug since _logger might not be available yet
            System.Diagnostics.Debug.WriteLine($"Critical error during iOS app launch: {ex}");
            _logger?.LogError(ex, "Critical error during iOS app launch");
            throw;
        }
    }
    
    private void RegisterCustomFonts()
    {
        try
        {
            // Explicitly handle font registration to prevent conflicts
            // This prevents the "GSFont already exists" error by ensuring
            // fonts are only registered once
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Font registration workaround: {ex.Message}");
        }
    }
    
    private async Task InitializeAsync()
    {
        try
        {
            await Task.Delay(100); // Let UI settle
            
            
            
            // Perform other heavy initialization tasks here
            SetupMemoryManagement();
            ConfigureForPerformance();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during async initialization: {ex}");
            _logger?.LogError(ex, "Error during async initialization");
        }
    }
    private void SetupMemoryManagement()
    {
        try
        {
            _observerManager = new ObserverManager();
            
            // Register for memory warning notifications
            var observer1 = NSNotificationCenter.DefaultCenter.AddObserver(
                UIApplication.DidReceiveMemoryWarningNotification,
                HandleMemoryWarning).AsDisposable();
            _observerManager.Add(observer1);
            
            // Register for background/foreground notifications
            var observer2 = NSNotificationCenter.DefaultCenter.AddObserver(
                UIApplication.DidEnterBackgroundNotification,
                HandleDidEnterBackground).AsDisposable();
            _observerManager.Add(observer2);
                
            var observer3 = NSNotificationCenter.DefaultCenter.AddObserver(
                UIApplication.WillEnterForegroundNotification,
                HandleWillEnterForeground).AsDisposable();
            _observerManager.Add(observer3);
            
            _logger?.LogDebug("iOS memory management setup completed");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting up iOS memory management");
        }
    }

    private void ConfigureForPerformance()
    {
        try
        {
            // Configure app for better performance
            UIApplication.SharedApplication.IdleTimerDisabled = false; // Allow screen to sleep
            
            _logger?.LogDebug("iOS performance configuration completed");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error configuring iOS performance settings");
        }
    }

    private void HandleMemoryWarning(NSNotification notification)
    {
        try
        {
            _logger?.LogWarning("iOS memory warning received");
            _memoryService?.HandleMemoryPressureAsync(MemoryPressureLevel.Critical);
            
            // Perform immediate cleanup
            PerformEmergencyCleanup();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling iOS memory warning");
        }
    }

    private void HandleDidEnterBackground(NSNotification notification)
    {
        try
        {
            _logger?.LogInformation("App entered background");
            
            // Optimize memory usage when in background
            _memoryService?.HandleMemoryPressureAsync(MemoryPressureLevel.Medium);
            
            // Perform background cleanup
            PerformBackgroundCleanup();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling background transition");
        }
    }

    private void HandleWillEnterForeground(NSNotification notification)
    {
        try
        {
            _logger?.LogInformation("App will enter foreground");
            
            // App is coming back to foreground - normal memory pressure
            _memoryService?.HandleMemoryPressureAsync(MemoryPressureLevel.Normal);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling foreground transition");
        }
    }

    private void PerformEmergencyCleanup()
    {
        try
        {
            // TODO: Replace forced GC with proper IDisposable patterns
            // GC.Collect();
            // GC.WaitForPendingFinalizers();
            // GC.Collect();
            
            // Clear iOS-specific caches
            NSUrlCache.SharedCache.RemoveAllCachedResponses();
            
            _logger?.LogInformation("iOS emergency cleanup completed");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during iOS emergency cleanup");
        }
    }

    private void PerformBackgroundCleanup()
    {
        try
        {
            // Lighter cleanup for background state
            // TODO: Replace forced GC with proper IDisposable patterns
            
            // Reduce cache sizes
            NSUrlCache.SharedCache.MemoryCapacity = NSUrlCache.SharedCache.MemoryCapacity / 2;
            
            _logger?.LogDebug("iOS background cleanup completed");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during iOS background cleanup");
        }
    }

    public override void OnActivated(UIApplication application)
    {
        base.OnActivated(application);
        
        try
        {
            // Restore cache sizes when app becomes active
            NSUrlCache.SharedCache.MemoryCapacity = 4 * 1024 * 1024; // 4MB default
            
            _logger?.LogDebug("iOS app activated");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during iOS app activation");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                // Dispose observer manager which will properly dispose all observers
                _observerManager?.Dispose();
                _observerManager = null;
                _logger?.LogInformation("iOS AppDelegate disposed");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during iOS AppDelegate disposal");
            }
        }
        
        base.Dispose(disposing);
    }

    public override void WillTerminate(UIApplication application)
    {
        _observerManager?.Dispose();
        _observerManager = null;
        base.WillTerminate(application);
    }
}
