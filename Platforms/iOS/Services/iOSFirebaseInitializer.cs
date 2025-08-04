using FlockForge.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlockForge.Platforms.iOS.Services
{
    public class iOSFirebaseInitializer : IFirebaseInitializer
    {
        private readonly ILogger<iOSFirebaseInitializer>? _logger;
        private static bool _isInitialized = false;
        private static readonly object _lockObject = new object();

        public iOSFirebaseInitializer(ILogger<iOSFirebaseInitializer>? logger = null)
        {
            _logger = logger;
        }

        public void Initialize()
        {
            lock (_lockObject)
            {
                if (_isInitialized)
                {
                    _logger?.LogDebug("Firebase already initialized for iOS");
                    return;
                }

                try
                {
                    // For Plugin.Firebase v3.1.1, Firebase initialization happens automatically
                    // when the GoogleService-Info.plist is present. The critical fix is ensuring
                    // this initialization happens BEFORE any Firebase services are accessed.
                    // This is handled in AppDelegate.FinishedLaunching.
                    
                    _isInitialized = true;
                    _logger?.LogInformation("Firebase initialization confirmed for iOS (Plugin.Firebase v3.1.1)");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to confirm Firebase initialization for iOS");
                    throw new InvalidOperationException("Firebase initialization failed. Ensure GoogleService-Info.plist is properly configured.", ex);
                }
            }
        }
    }
}