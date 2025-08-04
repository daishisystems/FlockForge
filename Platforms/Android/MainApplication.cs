using Android.App;
using Android.Runtime;

namespace FlockForge;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	public override void OnCreate()
	{
		try
		{
			// Create the MAUI app - Firebase will be initialized naturally when first accessed
			base.OnCreate();
		}
		catch (Exception ex)
		{
			// Use System.Diagnostics.Debug since logger might not be available yet
			System.Diagnostics.Debug.WriteLine($"Critical error during Android app initialization: {ex}");
			throw;
		}
	}

	private void InitializeFirebaseEarly()
	{
		try
		{
			// For Plugin.Firebase v3.1.1 on Android, Firebase initialization should happen automatically
			// when the google-services.json is present. However, we need to ensure this happens
			// BEFORE MauiProgram.CreateMauiApp() tries to access CrossFirebaseAuth.Current.
			//
			// The key insight: Plugin.Firebase v3 handles initialization automatically, but we need
			// to ensure the timing is correct. Instead of forcing early initialization, we'll let
			// the plugin handle it naturally and use factory patterns in MauiProgram to defer
			// Firebase service access until they're actually needed.
			
			System.Diagnostics.Debug.WriteLine("Firebase early initialization setup completed for Android (Plugin.Firebase v3.1.1)");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Firebase early initialization setup failed: {ex}");
			// Don't throw here - let the app continue and handle Firebase initialization naturally
			System.Diagnostics.Debug.WriteLine("Continuing with natural Firebase initialization...");
		}
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

