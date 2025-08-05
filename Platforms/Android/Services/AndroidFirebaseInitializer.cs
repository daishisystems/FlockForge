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
                    // Manual Firebase initialization without Crashlytics
                    // Since we disabled FirebaseInitProvider, we need to initialize manually
                    var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;
                    
                    // Initialize Firebase App manually
                    var firebaseOptions = Firebase.FirebaseOptions.FromResource(context);
                    if (firebaseOptions != null)
                    {
                        var firebaseApp = Firebase.FirebaseApp.InitializeApp(context, firebaseOptions);
                        _logger?.LogInformation("Firebase App initialized manually without Crashlytics");
                    }
                    else
                    {
                        _logger?.LogWarning("Firebase options not found, using default initialization");
                        Firebase.FirebaseApp.InitializeApp(context);
                    }
                    
                    _isInitialized = true;
                    _logger?.LogInformation("Firebase initialization completed for Android (without Crashlytics)");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to initialize Firebase for Android");
                    throw new InvalidOperationException("Firebase initialization failed. Ensure google-services.json is properly configured.", ex);
                }
            }
        }
    }
}