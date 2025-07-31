using Microsoft.Extensions.Logging;
using FlockForge.Services.Firebase;
using FlockForge.Services.Navigation;
using FlockForge.Services.Performance;
using FlockForge.Services.Platform;
using FlockForge.ViewModels.Base;
using FlockForge.ViewModels.Pages;
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
	
	private static void RegisterServices(MauiAppBuilder builder)
	{
		// Core services
		builder.Services.AddSingleton<TokenManager>();
		builder.Services.AddSingleton<INavigationService, NavigationService>();
		builder.Services.AddSingleton<IFirebaseService>(serviceProvider =>
		{
			var logger = serviceProvider.GetRequiredService<ILogger<FirebaseService>>();
			var tokenManager = serviceProvider.GetRequiredService<TokenManager>();
			return new FirebaseService(logger, tokenManager);
		});
		builder.Services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
		
		// Platform-specific services
		builder.Services.AddSingleton<IPlatformMemoryService, DefaultMemoryService>();
	}
	
	private static void RegisterViewModels(MauiAppBuilder builder)
	{
		// Register all ViewModels as transient
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<RegisterViewModel>();
		
		// Also register any other ViewModels from the assembly
		var assembly = Assembly.GetExecutingAssembly();
		var viewModelTypes = assembly.GetTypes()
			.Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(BaseViewModel)))
			.ToList();
			
		foreach (var viewModelType in viewModelTypes)
		{
			// Only register if not already registered
			if (!builder.Services.Any(s => s.ServiceType == viewModelType))
			{
				builder.Services.AddTransient(viewModelType);
			}
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
