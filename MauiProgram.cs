using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
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
		// Defensive: ok if called again thanks to guard.
		FirebaseBootstrap.TryInit();

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
		// IMPORTANT: Use the IServiceProvider overload so CrossFirebaseAuth.Current
		// is not evaluated during registration time.
		builder.Services.AddSingleton<IFirebaseAuth>(sp => CrossFirebaseAuth.Current);
		
		// Also register Lazy<IFirebaseAuth> for services that need lazy initialization
		builder.Services.AddSingleton<Lazy<IFirebaseAuth>>(sp =>
			new Lazy<IFirebaseAuth>(() => sp.GetRequiredService<IFirebaseAuth>()));
		
		builder.Services.AddSingleton<IFirebaseFirestore>(sp => CrossFirebaseFirestore.Current);
		
		// Also register Lazy<IFirebaseFirestore> for services that need lazy initialization
		builder.Services.AddSingleton<Lazy<IFirebaseFirestore>>(sp =>
			new Lazy<IFirebaseFirestore>(() => sp.GetRequiredService<IFirebaseFirestore>()));
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
