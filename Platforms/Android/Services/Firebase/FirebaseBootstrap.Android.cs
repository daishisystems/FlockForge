using Android.App;
using System;

namespace FlockForge.Services.Firebase;

public static partial class FirebaseBootstrap
{
    static partial void DoInit()
    {
        // Android: Firebase is initialized automatically via google-services plugin
        // when google-services.json is present in the Android project.
        // We just verify the context is available.
        var context = Android.App.Application.Context;
        if (context == null)
            throw new InvalidOperationException("Android Application.Context is null");

        // Try to initialize Firebase if not already done
        try
        {
            var firebaseAppClass = Java.Lang.Class.ForName("com.google.firebase.FirebaseApp");
            var initMethod = firebaseAppClass.GetMethod("initializeApp", Java.Lang.Class.FromType(typeof(Android.Content.Context)));
            initMethod.Invoke(null, context);
            System.Diagnostics.Debug.WriteLine("[Firebase] Android: FirebaseApp.initializeApp() called");
        }
        catch (Exception ex)
        {
            // Firebase might already be initialized, which is fine
            System.Diagnostics.Debug.WriteLine($"[Firebase] Android: Firebase initialization - {ex.Message}");
        }
    }
}