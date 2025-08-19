using ObjCRuntime;
using Foundation;

namespace FlockForge.Services.Firebase;

public static partial class FirebaseBootstrap
{
    static partial void DoInit()
    {
        // iOS: Configure Firebase using native iOS API
        // This is required before any Firebase services can be used
        try
        {
            // Use runtime invocation to call FirebaseApp.configure()
            var firebaseAppClass = Runtime.GetNSObject(Class.GetHandle("FIRApp"));
            if (firebaseAppClass != null)
            {
                var configureSelector = new Selector("configure");
                firebaseAppClass.PerformSelector(configureSelector);
                System.Diagnostics.Debug.WriteLine("[Firebase] iOS: FirebaseApp.configure() called via runtime");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[Firebase] iOS: Could not find FIRApp class");
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Firebase] iOS: Firebase configuration error - {ex.Message}");
            // Don't throw - let it fail later with a more specific error
        }
    }
}