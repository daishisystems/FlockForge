using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FlockForge.Services.Navigation
{
    public class NavigationService : INavigationService
    {
        private readonly ILogger<NavigationService> _logger;
        
        public NavigationService(ILogger<NavigationService> logger)
        {
            _logger = logger;
        }
        
        private INavigation Navigation => Application.Current?.Windows?.FirstOrDefault()?.Page?.Navigation
            ?? Application.Current?.MainPage?.Navigation
            ?? throw new InvalidOperationException("Navigation is not available");
        
        public async Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
        {
            try
            {
                if (parameters != null)
                {
                    await Shell.Current.GoToAsync(route, parameters);
                }
                else
                {
                    await Shell.Current.GoToAsync(route);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Navigation to {Route} failed", route);
                throw;
            }
        }
        
        public async Task GoBackAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Navigation back failed");
                throw;
            }
        }
        
        public async Task GoToRootAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("//");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Navigation to root failed");
                throw;
            }
        }
        
        public async Task NavigateToFarmerProfileAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("//farmer-profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Navigation to farmer profile failed");
                throw;
            }
        }
        
        public async Task NavigateToFarmProfileAsync(string? farmId = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(farmId))
                {
                    var parameters = new Dictionary<string, object>
                    {
                        ["farmId"] = farmId
                    };
                    await Shell.Current.GoToAsync("//farm-profile", parameters);
                }
                else
                {
                    await Shell.Current.GoToAsync("//farm-profile");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Navigation to farm profile failed");
                throw;
            }
        }
        
        public async Task NavigateToGroupsAsync(string farmId)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["farmId"] = farmId
                };
                await Shell.Current.GoToAsync("//groups", parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Navigation to groups failed");
                throw;
            }
        }
        
        public async Task NavigateToBreedingAsync(string groupId)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["groupId"] = groupId
                };
                await Shell.Current.GoToAsync("//breeding", parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Navigation to breeding failed");
                throw;
            }
        }
        
        public async Task NavigateToScanningAsync(string groupId)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["groupId"] = groupId
                };
                await Shell.Current.GoToAsync("//scanning", parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Navigation to scanning failed");
                throw;
            }
        }
        
        public async Task NavigateToLambingAsync(string groupId)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["groupId"] = groupId
                };
                await Shell.Current.GoToAsync("//lambing", parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Navigation to lambing failed");
                throw;
            }
        }
        
        public async Task NavigateToWeaningAsync(string groupId)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["groupId"] = groupId
                };
                await Shell.Current.GoToAsync("//weaning", parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Navigation to weaning failed");
                throw;
            }
        }
    }
}