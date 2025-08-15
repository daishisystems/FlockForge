using Foundation;
using UIKit;
using FlockForge.Services.Platform;
using Microsoft.Extensions.Logging;
using Firebase.Core;
using FlockForge.Utilities.Disposal;

namespace FlockForge;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    private ILogger<AppDelegate>? _logger;
    private IPlatformMemoryService? _memoryService;
    private NSObject? _memoryWarningObserver;
    private NSObject? _didEnterBackgroundObserver;
    private NSObject? _willEnterForegroundObserver;
    private bool _observersHooked;
#if DEBUG
    private static readonly System.Collections.Generic.HashSet<IntPtr> __trackedHandles = new();
    private static void TrackNSObject(Foundation.NSObject obj, string name) {
        __trackedHandles.Add(obj.Handle);
        System.Diagnostics.Debug.WriteLine($"[OBS-TRACK] {DateTime.Now:HH:mm:ss.fff} Added {name} handle={obj.Handle}");
    }
    private static void UntrackNSObject(Foundation.NSObject? obj, string name) {
        if (obj != null && __trackedHandles.Remove(obj.Handle))
            System.Diagnostics.Debug.WriteLine($"[OBS-TRACK] {DateTime.Now:HH:mm:ss.fff} Removed {name} handle={obj.Handle}");
    }
#endif

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

#if DEBUG
            ObjCRuntime.Runtime.MarshalManagedException += (s, a) =>
                System.Diagnostics.Debug.WriteLine($"[OBS-PROOF] {DateTime.Now:HH:mm:ss.fff} MarshalManagedException: {a.Exception?.GetType().Name}: {a.Exception?.Message}");
            ObjCRuntime.Runtime.MarshalObjectiveCException += (s, a) =>
                System.Diagnostics.Debug.WriteLine($"[OBS-PROOF] {DateTime.Now:HH:mm:ss.fff} MarshalObjectiveCException occurred");
#endif

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
    
    private async Task InitializeAsync()
    {
        try
        {
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
            if (_observersHooked)
                return;

            var __obsRunId = Guid.NewGuid().ToString("N");
            var __sw = System.Diagnostics.Stopwatch.StartNew();
            int __registeredCount = 0;
            System.Diagnostics.Debug.WriteLine($"[OBS-PROOF] {DateTime.Now:HH:mm:ss.fff} START reg run={__obsRunId} t={Environment.CurrentManagedThreadId}");

            try
            {
                // Register for memory warning notifications
#if DEBUG
                _memoryWarningObserver = ObserverTracker.Mark(
                    NSNotificationCenter.DefaultCenter.AddObserver(
                        UIApplication.DidReceiveMemoryWarningNotification,
                        HandleMemoryWarning),
                    "AppDelegate.cs:SetupMemoryManagement");
#else
                _memoryWarningObserver = NSNotificationCenter.DefaultCenter.AddObserver(
                    UIApplication.DidReceiveMemoryWarningNotification,
                    HandleMemoryWarning);
#endif
                __registeredCount++;
                System.Diagnostics.Debug.WriteLine($"[OBS-PROOF] {DateTime.Now:HH:mm:ss.fff} token set: name=MemoryWarning run={__obsRunId} handle={_memoryWarningObserver?.Handle} hash={_memoryWarningObserver?.GetHashCode()}");
#if DEBUG
                TrackNSObject(_memoryWarningObserver!, "MemoryWarning");
#endif

                // Register for background/foreground notifications
#if DEBUG
                _didEnterBackgroundObserver = ObserverTracker.Mark(
                    NSNotificationCenter.DefaultCenter.AddObserver(
                        UIApplication.DidEnterBackgroundNotification,
                        HandleDidEnterBackground),
                    "AppDelegate.cs:SetupMemoryManagement");
#else
                _didEnterBackgroundObserver = NSNotificationCenter.DefaultCenter.AddObserver(
                    UIApplication.DidEnterBackgroundNotification,
                    HandleDidEnterBackground);
#endif
                __registeredCount++;
                System.Diagnostics.Debug.WriteLine($"[OBS-PROOF] {DateTime.Now:HH:mm:ss.fff} token set: name=Background run={__obsRunId} handle={_didEnterBackgroundObserver?.Handle} hash={_didEnterBackgroundObserver?.GetHashCode()}");
#if DEBUG
                TrackNSObject(_didEnterBackgroundObserver!, "Background");
#endif

#if DEBUG
                _willEnterForegroundObserver = ObserverTracker.Mark(
                    NSNotificationCenter.DefaultCenter.AddObserver(
                        UIApplication.WillEnterForegroundNotification,
                        HandleWillEnterForeground),
                    "AppDelegate.cs:SetupMemoryManagement");
#else
                _willEnterForegroundObserver = NSNotificationCenter.DefaultCenter.AddObserver(
                    UIApplication.WillEnterForegroundNotification,
                    HandleWillEnterForeground);
#endif
                __registeredCount++;
                System.Diagnostics.Debug.WriteLine($"[OBS-PROOF] {DateTime.Now:HH:mm:ss.fff} token set: name=Foreground run={__obsRunId} handle={_willEnterForegroundObserver?.Handle} hash={_willEnterForegroundObserver?.GetHashCode()}");
#if DEBUG
                TrackNSObject(_willEnterForegroundObserver!, "Foreground");
#endif
            }
            finally
            {
                __sw.Stop();
                System.Diagnostics.Debug.WriteLine($"[OBS-PROOF] {DateTime.Now:HH:mm:ss.fff} FINISH reg run={__obsRunId} elapsedMs={__sw.ElapsedMilliseconds} count={__registeredCount} t={Environment.CurrentManagedThreadId}");
            }

            _observersHooked = true;
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
                System.Diagnostics.Debug.WriteLine($"[OBS-PROOF] {DateTime.Now:HH:mm:ss.fff} DISPOSE begin mw={(_memoryWarningObserver!=null)} bg={(_didEnterBackgroundObserver!=null)} fg={(_willEnterForegroundObserver!=null)}");

                if (_memoryWarningObserver != null)
                {
                    var __obj = _memoryWarningObserver;
                    var __h = __obj.Handle;
                    __obj.Dispose();
                    _memoryWarningObserver = null;
                    System.Diagnostics.Debug.WriteLine($"[OBS-PROOF] {DateTime.Now:HH:mm:ss.fff} DISPOSED name=MemoryWarning handle={__h}");
#if DEBUG
                    UntrackNSObject(__obj, "MemoryWarning");
#endif
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[OBS-PROOF] {DateTime.Now:HH:mm:ss.fff} SKIP-DISPOSE name=MemoryWarning (already null)");
                }

                if (_didEnterBackgroundObserver != null)
                {
                    var __obj = _didEnterBackgroundObserver;
                    var __h = __obj.Handle;
                    __obj.Dispose();
                    _didEnterBackgroundObserver = null;
                    System.Diagnostics.Debug.WriteLine($"[OBS-PROOF] {DateTime.Now:HH:mm:ss.fff} DISPOSED name=Background handle={__h}");
#if DEBUG
                    UntrackNSObject(__obj, "Background");
#endif
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[OBS-PROOF] {DateTime.Now:HH:mm:ss.fff} SKIP-DISPOSE name=Background (already null)");
                }

                if (_willEnterForegroundObserver != null)
                {
                    var __obj = _willEnterForegroundObserver;
                    var __h = __obj.Handle;
                    __obj.Dispose();
                    _willEnterForegroundObserver = null;
                    System.Diagnostics.Debug.WriteLine($"[OBS-PROOF] {DateTime.Now:HH:mm:ss.fff} DISPOSED name=Foreground handle={__h}");
#if DEBUG
                    UntrackNSObject(__obj, "Foreground");
#endif
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[OBS-PROOF] {DateTime.Now:HH:mm:ss.fff} SKIP-DISPOSE name=Foreground (already null)");
                }

                _observersHooked = false;
                System.Diagnostics.Debug.WriteLine($"[OBS-PROOF] {DateTime.Now:HH:mm:ss.fff} DISPOSE end");
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
