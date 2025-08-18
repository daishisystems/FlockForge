using System.Threading;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Plugin.Firebase.Core;

#if ANDROID
using Android.OS;
using Android.App;
#endif

namespace FlockForge.Platform
{
    public static class FirebaseBootstrap
    {
        private static int _initialized; // 0 = false, 1 = true

#if ANDROID
        public static void TryInit(Activity activity, Bundle bundle)
        {
            if (Interlocked.Exchange(ref _initialized, 1) == 1)
            {
                System.Diagnostics.Debug.WriteLine("Firebase init skipped (already initialized)");
                return;
            }

            // Plugin-only init (Android overload)
            CrossFirebase.Initialize(activity, bundle);

            System.Diagnostics.Debug.WriteLine("✅ Firebase initialized via Plugin.Firebase (Android)");
        }
#else
        public static void TryInit()
        {
            if (Interlocked.Exchange(ref _initialized, 1) == 1)
            {
                System.Diagnostics.Debug.WriteLine("Firebase init skipped (already initialized)");
                return;
            }

            // Plugin-only init (iOS/macOS/Windows overload)
            CrossFirebase.Initialize();

            System.Diagnostics.Debug.WriteLine("✅ Firebase initialized via Plugin.Firebase (iOS/macOS/Windows)");
        }
#endif
    }
}

