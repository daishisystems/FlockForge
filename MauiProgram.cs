using Microsoft.Extensions.Logging;
using FlockForge.Core.Interfaces;
using FlockForge.Core.Configuration;
using FlockForge.Services.Firebase;
using FlockForge.Services.Navigation;
using CommunityToolkit.Maui;
using System.Net.Http;

#if IOS || ANDROID
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
                        .UseMauiCommunityToolkit();

#if ANDROID
                builder.ConfigureFonts(fonts =>
                {
                        // Re-register the same files/aliases to ensure Android alias→asset mapping is present at runtime.
                        // Use file names (not paths); build packs them from Resources/Fonts.
                        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                        fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
#endif

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

		// Firebase services - platform-specific initialization
#if IOS || ANDROID
                builder.Services.AddSingleton<Lazy<IFirebaseAuth>>(serviceProvider =>
                {
                        return new Lazy<IFirebaseAuth>(() =>
                        {
                                try
                                {
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
