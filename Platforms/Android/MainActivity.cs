using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Content;
using FlockForge.Services.Platform;
using FlockForge.Platforms.Android.Services;
using Microsoft.Extensions.Logging;

namespace FlockForge;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private ILogger<MainActivity>? _logger;
    private IPlatformMemoryService? _memoryService;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        try
        {
            // Initialize Firebase
            var firebaseInitializer = new AndroidFirebaseInitializer();
            firebaseInitializer.Initialize();
            
            // Get services from DI container
            var serviceProvider = IPlatformApplication.Current?.Services;
            _logger = serviceProvider?.GetService<ILogger<MainActivity>>();
            _memoryService = serviceProvider?.GetService<IPlatformMemoryService>();
            
            ConfigureForPerformance();
            SetupMemoryManagement();
            
            _logger?.LogInformation("MainActivity created successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during MainActivity creation");
        }
    }

    private void ConfigureForPerformance()
    {
        try
        {
            // Enable hardware acceleration
            Window?.SetFlags(WindowManagerFlags.HardwareAccelerated,
                WindowManagerFlags.HardwareAccelerated);
            
            // Configure task description for better memory management
            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                // Android 9.0+ (API 28+) - Use the simplified constructor
                var taskDescription = new ActivityManager.TaskDescription("FlockForge");
                SetTaskDescription(taskDescription);
            }
            else if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                // Android 6.0+ (API 23+) - Use the legacy constructor with color
#pragma warning disable CA1422 // Validate platform compatibility
                var taskDescription = new ActivityManager.TaskDescription(
                    "FlockForge",
                    null,
                    Android.Graphics.Color.ParseColor("#2B5CB6"));
                SetTaskDescription(taskDescription);
#pragma warning restore CA1422 // Validate platform compatibility
            }
            
            // Set process priority for better performance
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                Android.OS.Process.SetThreadPriority(Android.OS.ThreadPriority.Default);
            }
            
            _logger?.LogDebug("Performance configuration completed");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error configuring performance settings");
        }
    }

    private void SetupMemoryManagement()
    {
        try
        {
            // Register for memory pressure notifications
            RegisterComponentCallbacks(new MemoryComponentCallbacks(_logger, _memoryService));
            
            _logger?.LogDebug("Memory management setup completed");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting up memory management");
        }
    }

    public override void OnTrimMemory(TrimMemory level)
    {
        base.OnTrimMemory(level);
        
        try
        {
            var memoryPressureLevel = MapTrimMemoryLevel(level);
            _logger?.LogWarning("Memory trim requested: {Level} -> {MappedLevel}", level, memoryPressureLevel);
            
            // Handle memory pressure through our service
            _memoryService?.HandleMemoryPressureAsync(memoryPressureLevel);
            
            // Perform immediate cleanup for critical levels
            if (level >= TrimMemory.RunningCritical)
            {
                PerformEmergencyCleanup();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling memory trim");
        }
    }

    public override void OnLowMemory()
    {
        base.OnLowMemory();
        
        try
        {
            _logger?.LogWarning("Low memory warning received");
            _memoryService?.HandleMemoryPressureAsync(MemoryPressureLevel.Critical);
            PerformEmergencyCleanup();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling low memory");
        }
    }

    private MemoryPressureLevel MapTrimMemoryLevel(TrimMemory level)
    {
        return level switch
        {
            TrimMemory.RunningModerate => MemoryPressureLevel.Low,
            TrimMemory.RunningLow => MemoryPressureLevel.Medium,
            TrimMemory.RunningCritical => MemoryPressureLevel.Critical,
            TrimMemory.UiHidden => MemoryPressureLevel.Low,
            TrimMemory.Background => MemoryPressureLevel.Medium,
            TrimMemory.Moderate => MemoryPressureLevel.Medium,
            TrimMemory.Complete => MemoryPressureLevel.Critical,
            _ => MemoryPressureLevel.Normal
        };
    }

    private void PerformEmergencyCleanup()
    {
        try
        {
            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            // Clear any Android-specific caches
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                // Clear bitmap caches, drawable caches, etc.
                Resources?.FlushLayoutCache();
            }
            
            _logger?.LogInformation("Emergency cleanup completed");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during emergency cleanup");
        }
    }

    protected override void OnDestroy()
    {
        try
        {
            _logger?.LogInformation("MainActivity being destroyed");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during MainActivity destruction");
        }
        finally
        {
            base.OnDestroy();
        }
    }
}

internal class MemoryComponentCallbacks : Java.Lang.Object, IComponentCallbacks2
{
    private readonly ILogger? _logger;
    private readonly IPlatformMemoryService? _memoryService;

    public MemoryComponentCallbacks(ILogger? logger, IPlatformMemoryService? memoryService)
    {
        _logger = logger;
        _memoryService = memoryService;
    }

    public void OnConfigurationChanged(Android.Content.Res.Configuration? newConfig)
    {
        // Handle configuration changes if needed
    }

    public void OnLowMemory()
    {
        try
        {
            _logger?.LogWarning("Component callbacks: Low memory");
            _memoryService?.HandleMemoryPressureAsync(MemoryPressureLevel.Critical);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in component callbacks low memory handler");
        }
    }

    public void OnTrimMemory(TrimMemory level)
    {
        try
        {
            var memoryPressureLevel = level switch
            {
                TrimMemory.RunningModerate => MemoryPressureLevel.Low,
                TrimMemory.RunningLow => MemoryPressureLevel.Medium,
                TrimMemory.RunningCritical => MemoryPressureLevel.Critical,
                _ => MemoryPressureLevel.Normal
            };

            _logger?.LogDebug("Component callbacks: Memory trim {Level} -> {MappedLevel}", level, memoryPressureLevel);
            _memoryService?.HandleMemoryPressureAsync(memoryPressureLevel);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in component callbacks trim memory handler");
        }
    }
}

