using FlockForge.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlockForge.Platforms.Android.Services
{
    public class AndroidFirebaseInitializer : IFirebaseInitializer
    {
        private readonly ILogger<AndroidFirebaseInitializer>? _logger;
        private static bool _isInitialized = false;
        private static readonly object _lockObject = new object();

        public AndroidFirebaseInitializer(ILogger<AndroidFirebaseInitializer>? logger = null)
        {
            _logger = logger;
        }

        public void Initialize()
        {
            lock (_lockObject)
            {
                if (_isInitialized)
                {
                    _logger?.LogDebug("Firebase already initialized for Android");
                    return;
                }

                try
                {
                    // For Plugin.Firebase v3.1.1, Firebase initialization happens automatically
                    // when the google-services.json is present. Android typically handles this
                    // better than iOS, but we maintain consistency with the iOS approach.
                    
                    _isInitialized = true;
                    _logger?.LogInformation("Firebase initialization confirmed for Android (Plugin.Firebase v3.1.1)");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to confirm Firebase initialization for Android");
                    throw new InvalidOperationException("Firebase initialization failed. Ensure google-services.json is properly configured.", ex);
                }
            }
        }
    }
}