using Microsoft.Extensions.Logging;

namespace FlockForge.Services.Navigation;

public class NavigationService : INavigationService
{
    private readonly ILogger<NavigationService> _logger;

    public NavigationService(ILogger<NavigationService> logger)
    {
        _logger = logger;
    }

    public async Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        try
        {
            if (parameters != null && parameters.Any())
            {
                await Shell.Current.GoToAsync(route, parameters);
            }
            else
            {
                await Shell.Current.GoToAsync(route);
            }
            
            _logger.LogDebug("Navigated to route: {Route}", route);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to route: {Route}", route);
            throw;
        }
    }

    public async Task GoBackAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("..");
            _logger.LogDebug("Navigated back");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate back");
            throw;
        }
    }

    public async Task GoToRootAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//");
            _logger.LogDebug("Navigated to root");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to root");
            throw;
        }
    }

    public async Task NavigateToFarmerProfileAsync()
    {
        await NavigateToAsync("//farmer-profile");
    }

    public async Task NavigateToFarmProfileAsync(string? farmId = null)
    {
        var parameters = farmId != null 
            ? new Dictionary<string, object> { ["farmId"] = farmId }
            : null;
        await NavigateToAsync("//farm-profile", parameters);
    }

    public async Task NavigateToGroupsAsync(string farmId)
    {
        var parameters = new Dictionary<string, object> { ["farmId"] = farmId };
        await NavigateToAsync("//groups", parameters);
    }

    public async Task NavigateToBreedingAsync(string groupId)
    {
        var parameters = new Dictionary<string, object> { ["groupId"] = groupId };
        await NavigateToAsync("//breeding", parameters);
    }

    public async Task NavigateToScanningAsync(string groupId)
    {
        var parameters = new Dictionary<string, object> { ["groupId"] = groupId };
        await NavigateToAsync("//scanning", parameters);
    }

    public async Task NavigateToLambingAsync(string groupId)
    {
        var parameters = new Dictionary<string, object> { ["groupId"] = groupId };
        await NavigateToAsync("//lambing", parameters);
    }

    public async Task NavigateToWeaningAsync(string groupId)
    {
        var parameters = new Dictionary<string, object> { ["groupId"] = groupId };
        await NavigateToAsync("//weaning", parameters);
    }
}