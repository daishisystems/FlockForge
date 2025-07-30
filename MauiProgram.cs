using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using FlockForge.Data.Local;
using FlockForge.Services.Database;
using FlockForge.Services.Firebase;
using FlockForge.Services.Navigation;
using FlockForge.Services.Sync;
using FlockForge.Services.Performance;
using FlockForge.Services.Platform;
using FlockForge.ViewModels.Base;
using System.Reflection;

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
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Configure database
		ConfigureDatabase(builder);
		
		// Register services
		RegisterServices(builder);
		
		// Register ViewModels
		RegisterViewModels(builder);
		
		// Configure HTTP clients
		ConfigureHttpClients(builder);

#if DEBUG
		builder.Logging.AddDebug();
		builder.Logging.SetMinimumLevel(LogLevel.Debug);
#else
		builder.Logging.SetMinimumLevel(LogLevel.Information);
#endif

		return builder.Build();
	}
	
	private static void ConfigureDatabase(MauiAppBuilder builder)
	{
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "flockforge.db");
		
		builder.Services.AddDbContext<FlockForgeDbContext>(options =>
		{
			options.UseSqlite($"Data Source={dbPath}")
				   .EnableSensitiveDataLogging(false)
				   .EnableServiceProviderCaching()
				   .EnableDetailedErrors(false);
				   
#if DEBUG
			options.EnableSensitiveDataLogging(true)
				   .EnableDetailedErrors(true);
#endif
		}, ServiceLifetime.Scoped);
	}
	
	private static void RegisterServices(MauiAppBuilder builder)
	{
		// Core services
		builder.Services.AddSingleton<INavigationService, NavigationService>();
		builder.Services.AddSingleton<IFirebaseService, FirebaseService>();
		builder.Services.AddScoped<IDatabaseService, DatabaseService>();
		builder.Services.AddSingleton<IBackgroundSyncService, BackgroundSyncService>();
		builder.Services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
		
		// Platform-specific services
		builder.Services.AddSingleton<IPlatformMemoryService, DefaultMemoryService>();
	}
	
	private static void RegisterViewModels(MauiAppBuilder builder)
	{
		// Register all ViewModels as transient
		var assembly = Assembly.GetExecutingAssembly();
		var viewModelTypes = assembly.GetTypes()
			.Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(BaseViewModel)))
			.ToList();
			
		foreach (var viewModelType in viewModelTypes)
		{
			builder.Services.AddTransient(viewModelType);
		}
	}
	
	private static void ConfigureHttpClients(MauiAppBuilder builder)
	{
		builder.Services.AddHttpClient("FlockForgeApi", client =>
		{
			client.Timeout = TimeSpan.FromSeconds(30);
			client.DefaultRequestHeaders.Add("User-Agent", "FlockForge/1.0");
		});
	}
	
#if !DEBUG
	private static bool ValidateCertificatePinning(System.Security.Cryptography.X509Certificates.X509Certificate2? cert)
	{
		// TODO: Implement actual certificate pinning validation
		// This is a placeholder for production certificate validation
		return cert != null;
	}
#endif
}

