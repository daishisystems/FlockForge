using Foundation;
using UIKit;
using FlockForge.Services.Platform;
using Microsoft.Extensions.Logging;

namespace FlockForge;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    private ILogger<AppDelegate>? _logger;
    private IPlatformMemoryService? _memoryService;

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        var result = base.FinishedLaunching(application, launchOptions);
        
        try
        {
            // Get services from DI container after MAUI app is created
            var mauiApp = IPlatformApplication.Current?.Services;
            if (mauiApp != null)
            {
                _logger = mauiApp.GetService<ILogger<AppDelegate>>();
                _memoryService = mauiApp.GetService<IPlatformMemoryService>();
            }
            
            SetupMemoryManagement();
            ConfigureForPerformance();
            
            _logger?.LogInformation("iOS AppDelegate finished launching successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during iOS app launch");
        }
        
        return result;
    }

    private void SetupMemoryManagement()
    {
        try
        {
            // Register for memory warning notifications
            NSNotificationCenter.DefaultCenter.AddObserver(
                UIApplication.DidReceiveMemoryWarningNotification,
                HandleMemoryWarning);
            
            // Register for background/foreground notifications
            NSNotificationCenter.DefaultCenter.AddObserver(
                UIApplication.DidEnterBackgroundNotification,
                HandleDidEnterBackground);
                
            NSNotificationCenter.DefaultCenter.AddObserver(
                UIApplication.WillEnterForegroundNotification,
                HandleWillEnterForeground);
            
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
            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
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
            GC.Collect(0, GCCollectionMode.Optimized);
            
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
                // Unregister notifications
                NSNotificationCenter.DefaultCenter.RemoveObserver(this);
                _logger?.LogInformation("iOS AppDelegate disposed");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during iOS AppDelegate disposal");
            }
        }
        
        base.Dispose(disposing);
    }
}

