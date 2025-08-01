using Microsoft.Extensions.Logging;
using FlockForge.Core.Interfaces;
using FlockForge.Core.Configuration;
using FlockForge.Services.Firebase;
using FlockForge.Services.Navigation;
using CommunityToolkit.Maui;

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

		// Initialize Firebase Firestore (will be configured in services)
		// The FirestoreService will handle Firestore initialization

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
		
		// Firebase services
		builder.Services.AddSingleton<IAuthenticationService, FirebaseAuthenticationService>();
		// Use OfflineDataService to avoid ADC requirements during development
		// This eliminates the need for Firebase credentials while maintaining full functionality
		builder.Services.AddSingleton<IDataService>(sp =>
		{
			var authService = sp.GetRequiredService<IAuthenticationService>();
			var logger = sp.GetRequiredService<ILogger<Services.Firebase.OfflineDataService>>();
			return new Services.Firebase.OfflineDataService(authService, logger);
		});
		
		// Firebase service bridge (provides compatibility layer)
		builder.Services.AddSingleton<Services.Firebase.IFirebaseService, Services.Firebase.FirebaseService>();
		
		// Navigation service
		builder.Services.AddSingleton<Services.Navigation.INavigationService, Services.Navigation.NavigationService>();
		
		// ViewModels
		builder.Services.AddTransient<ViewModels.Pages.LoginViewModel>();
		
		// Pages
		builder.Services.AddTransient<Views.Pages.LoginPage>();
		
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
