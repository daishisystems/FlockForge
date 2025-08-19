using System;

namespace FlockForge.Services.Firebase;

public static partial class FirebaseBootstrap
{
    static bool _initialized;
    public static bool IsInitialized => _initialized;

    public static void TryInit()
    {
        if (_initialized) return;

        try
        {
            DoInit();        // platform-specific (partial)
            _initialized = true;
            System.Diagnostics.Debug.WriteLine("[Firebase] Plugin initialization OK.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Firebase] Plugin initialization FAILED: {ex}");
            throw; // surface early; do not silently continue
        }
    }

    static partial void DoInit();
}