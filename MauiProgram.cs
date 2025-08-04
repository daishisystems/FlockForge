using Microsoft.Extensions.Logging;
using FlockForge.Core.Interfaces;
using FlockForge.Core.Configuration;
using FlockForge.Services.Firebase;
using FlockForge.Services.Navigation;
using CommunityToolkit.Maui;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;

#if IOS
using FlockForge.Platforms.iOS.Services;
#elif ANDROID
using FlockForge.Platforms.Android.Services;
#endif

namespace FlockForge;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				// Only add fonts if they're not already registered (reduces iOS simulator warnings)
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			})
			.UseMauiCommunityToolkit();

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
		
		// Firebase platform initializers
#if IOS
		builder.Services.AddSingleton<FlockForge.Core.Interfaces.IFirebaseInitializer, iOSFirebaseInitializer>();
#elif ANDROID
		builder.Services.AddSingleton<FlockForge.Core.Interfaces.IFirebaseInitializer, AndroidFirebaseInitializer>();
#endif

		// Firebase services - using lazy initialization to avoid early access
		// This prevents Firebase from being accessed during dependency injection setup
		builder.Services.AddSingleton<Lazy<IFirebaseAuth>>(serviceProvider =>
		{
			return new Lazy<IFirebaseAuth>(() =>
			{
				try
				{
					// Plugin.Firebase v3.1.1 handles initialization automatically when config files are present
					// This will only be called when Firebase Auth is actually needed
					return CrossFirebaseAuth.Current;
				}
				catch (Exception ex)
				{
					var logger = serviceProvider.GetService<ILogger<IFirebaseAuth>>();
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
					// Plugin.Firebase v3.1.1 handles initialization automatically when config files are present
					// This will only be called when Firebase Firestore is actually needed
					return CrossFirebaseFirestore.Current;
				}
				catch (Exception ex)
				{
					var logger = serviceProvider.GetService<ILogger<IFirebaseFirestore>>();
					logger?.LogError(ex, "Failed to initialize Firebase Firestore. Ensure Firebase configuration files are properly set up.");
					throw new InvalidOperationException("Firebase Firestore initialization failed. Check Firebase configuration.", ex);
				}
			});
		});
		
		// Don't register IFirebaseAuth and IFirebaseFirestore directly
		// Let the services that need them access the Lazy<T> versions directly
		
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
		
		// Configure logging
#if DEBUG
		builder.Logging.AddDebug();
		builder.Logging.SetMinimumLevel(LogLevel.Debug);
#else
		builder.Logging.SetMinimumLevel(LogLevel.Warning);
#endif

		return builder.Build();
	}
}
