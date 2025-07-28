namespace FlockForge.Services.Navigation;

public interface INavigationService
{
    Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null);
    Task GoBackAsync();
    Task GoToRootAsync();
    
    // Specific navigation methods for main pages
    Task NavigateToFarmerProfileAsync();
    Task NavigateToFarmProfileAsync(string? farmId = null);
    Task NavigateToGroupsAsync(string farmId);
    Task NavigateToBreedingAsync(string groupId);
    Task NavigateToScanningAsync(string groupId);
    Task NavigateToLambingAsync(string groupId);
    Task NavigateToWeaningAsync(string groupId);
}