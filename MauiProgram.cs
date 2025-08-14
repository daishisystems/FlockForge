using Microsoft.Extensions.Logging;
using FlockForge.Core.Interfaces;
using FlockForge.Core.Configuration;
using FlockForge.Services.Firebase;
using FlockForge.Services.Navigation;
using CommunityToolkit.Maui;
using System.Net.Http;

#if IOS
using FlockForge.Platforms.iOS.Services;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;
#elif ANDROID
using FlockForge.Platforms.Android.Services;
using Firebase;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;
#endif

namespace FlockForge;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
                builder
                        .UseMauiApp<App>()
                        .UseMauiCommunityToolkit()
                        .ConfigureFonts(fonts =>
                        {
                                fonts.AddFont("OpenSans-Regular.ttf", FontNames.Regular);
                                fonts.AddFont("OpenSans-Semibold.ttf", FontNames.Semibold);
                        });

		// Configuration
		builder.Services.AddSingleton<FirebaseConfig>(sp =>
		{
			var config = new FirebaseConfig();
			// Load from appsettings.json or platform config
			return config;
		});
		
                // Platform services
                builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
                builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);
                builder.Services.AddSingleton<IPreferences>(Preferences.Default);
                builder.Services.AddSingleton<HttpClient>();
		
		// Firebase platform initializers
#if IOS
		builder.Services.AddSingleton<FlockForge.Core.Interfaces.IFirebaseInitializer, iOSFirebaseInitializer>();
#elif ANDROID
		builder.Services.AddSingleton<FlockForge.Core.Interfaces.IFirebaseInitializer, AndroidFirebaseInitializer>();
#endif

		// Firebase services - platform-specific initialization
#if IOS || ANDROID
		builder.Services.AddSingleton<Lazy<IFirebaseAuth>>(serviceProvider =>
		{
			return new Lazy<IFirebaseAuth>(() =>
			{
				try
				{
#if ANDROID
					// Ensure Firebase is initialized before accessing Auth
					var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;
					if (FirebaseApp.GetApps(context).Count == 0)
					{
						// Create custom Firebase options WITHOUT Crashlytics auto-initialization
						var options = new Firebase.FirebaseOptions.Builder()
							.SetProjectId("flockforge-ac0b9")
							.SetApplicationId("1:717823882706:android:6d30877be18d2f36ee040b")
							.SetApiKey("AIzaSyBWir-B9kgti50XfiePZ0kXQVitRS3EuyE")
							.SetStorageBucket("flockforge-ac0b9.firebasestorage.app")
							.Build();
						
						// Initialize Firebase with custom options (no Crashlytics)
						FirebaseApp.InitializeApp(context, options);
						System.Diagnostics.Debug.WriteLine("Firebase initialized for Android with Crashlytics excluded");
					}
#endif
					// Now safely access Firebase Auth
					return CrossFirebaseAuth.Current;
				}
				catch (Exception ex)
				{
					var logger = serviceProvider.GetService<ILogger<Lazy<IFirebaseAuth>>>();
					logger?.LogError(ex, "Failed to initialize Firebase Auth. Ensure Firebase configuration files are properly set up.");
					throw new InvalidOperationException("Firebase Auth initialization failed. Check Firebase configuration.", ex);
				}
			});
		});
		
		builder.Services.AddSingleton<Lazy<IFirebaseFirestore>>(serviceProvider =>
		{
			return new Lazy<IFirebaseFirestore>(() =>
			{
				try
				{
#if ANDROID
					// Ensure Firebase is initialized before accessing Firestore
					var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;
					if (FirebaseApp.GetApps(context).Count == 0)
					{
						// Create custom Firebase options WITHOUT Crashlytics auto-initialization
						var options = new Firebase.FirebaseOptions.Builder()
							.SetProjectId("flockforge-ac0b9")
							.SetApplicationId("1:717823882706:android:6d30877be18d2f36ee040b")
							.SetApiKey("AIzaSyBWir-B9kgti50XfiePZ0kXQVitRS3EuyE")
							.SetStorageBucket("flockforge-ac0b9.firebasestorage.app")
							.Build();
						
						// Initialize Firebase with custom options (no Crashlytics)
						FirebaseApp.InitializeApp(context, options);
						System.Diagnostics.Debug.WriteLine("Firebase initialized for Android with Crashlytics excluded");
					}
#endif
					// Now safely access Firebase Firestore
					return CrossFirebaseFirestore.Current;
				}
				catch (Exception ex)
				{
					var logger = serviceProvider.GetService<ILogger<Lazy<IFirebaseFirestore>>>();
					logger?.LogError(ex, "Failed to initialize Firebase Firestore. Ensure Firebase configuration files are properly set up.");
					throw new InvalidOperationException("Firebase Firestore initialization failed. Check Firebase configuration.", ex);
				}
			});
		});
#endif
		
		builder.Services.AddSingleton<IAuthenticationService, FirebaseAuthenticationService>();
		
		// Use real Firestore service for production
		builder.Services.AddSingleton<IDataService, FirestoreService>();
		
		// Firebase service bridge (provides compatibility layer)
		builder.Services.AddSingleton<Services.Firebase.IFirebaseService, Services.Firebase.FirebaseService>();
		
		// Navigation service
		builder.Services.AddSingleton<Services.Navigation.INavigationService, Services.Navigation.NavigationService>();
		
		// Shell
		builder.Services.AddSingleton<AppShell>();
		
		// ViewModels
		builder.Services.AddTransient<ViewModels.Pages.LoginViewModel>();
		builder.Services.AddTransient<ViewModels.Pages.RegisterViewModel>();
		
		// Pages
		builder.Services.AddTransient<Views.Pages.LoginPage>();
		builder.Services.AddTransient<Views.Pages.RegisterPage>();
		builder.Services.AddTransient<MainPage>();
		
		// Configure logging
#if DEBUG
                builder.Logging.AddDebug();
                builder.Logging.SetMinimumLevel(LogLevel.Debug);
#else
                builder.Logging.SetMinimumLevel(LogLevel.Warning);
#endif

                var app = builder.Build();

                return app;
        }
}
