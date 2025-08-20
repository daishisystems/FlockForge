#define COMMUNITY_TOOLKIT

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Networking;

#if COMMUNITY_TOOLKIT
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
#endif

namespace FlockForge.ViewModels
{
#if COMMUNITY_TOOLKIT
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty] private int gridSpan = 2;
        // TODO: Replace "Demo" with actual farm from service:
        // var farmService = Application.Current?.Handler?.MauiContext?.Services?.GetService<IFarmService>();
        // CurrentFarmName = farmService?.CurrentFarm?.Name ?? "Demo";
        [ObservableProperty] private string currentFarmName = "Demo";
        [ObservableProperty] private string connectivityLabel = "Aanlyn";
        [ObservableProperty] private Color statusPillColor = Color.FromArgb("#19A974");
#else
    using FlockForge.ViewModels.Base;
    public class DashboardViewModel : NotifyObject
    {
        private int gridSpan = 2; public int GridSpan { get => gridSpan; set => Set(ref gridSpan, value); }
        // TODO: Replace "Demo" with actual farm from service:
        // var farmService = Application.Current?.Handler?.MauiContext?.Services?.GetService<IFarmService>();
        // CurrentFarmName = farmService?.CurrentFarm?.Name ?? "Demo";
        private string currentFarmName = "Demo"; public string CurrentFarmName { get => currentFarmName; set => Set(ref currentFarmName, value); }
        private string connectivityLabel = "Aanlyn"; public string ConnectivityLabel { get => connectivityLabel; set => Set(ref connectivityLabel, value); }
        private Color statusPillColor = Color.FromArgb("#19A974"); public Color StatusPillColor { get => statusPillColor; set => Set(ref statusPillColor, value); }
#endif

        public ObservableCollection<FeatureItem> Features { get; } = new();

        public DashboardViewModel()
        {
            // Text-only Afrikaans tiles
            Features.Add(new FeatureItem("Profiel",  CreateNav("profile")));
            Features.Add(new FeatureItem("My Plase", CreateNav("farms")));
            Features.Add(new FeatureItem("Groepe",   CreateNav("groups")));
            Features.Add(new FeatureItem("Teel",     CreateNav("breeding")));
            Features.Add(new FeatureItem("Skandering (Dragtigheid)", CreateNav("scanning")));
            Features.Add(new FeatureItem("Lammering", CreateNav("lambing")));
            Features.Add(new FeatureItem("Speen",     CreateNav("weaning")));
            Features.Add(new FeatureItem("Verslae",   CreateNav("reports")));

            UpdateConnectivityLabel(); // one-shot; no timers/subscriptions
        }

        private void UpdateConnectivityLabel()
        {
            var access = Connectivity.NetworkAccess;
            if (access == NetworkAccess.Internet)
            { ConnectivityLabel = "Aanlyn"; StatusPillColor = Color.FromArgb("#19A974"); }
            else if (access == NetworkAccess.ConstrainedInternet)
            { ConnectivityLabel = "Sinkroniseer"; StatusPillColor = Color.FromArgb("#FFB300"); }
            else
            { ConnectivityLabel = "Aflyn"; StatusPillColor = Color.FromArgb("#E53935"); }
        }

#if COMMUNITY_TOOLKIT
        private IAsyncRelayCommand CreateNav(string route) =>
            new AsyncRelayCommand(async () => { try { await Shell.Current.GoToAsync(route); } catch { } });
#else
        private ICommand CreateNav(string route) =>
            new Command(async () => { try { await Shell.Current.GoToAsync(route); } catch { } });
#endif
    }

    public class FeatureItem
    {
        public string Title { get; }
        public string A11yDescription => $"Maak {Title} oop";
#if COMMUNITY_TOOLKIT
        public IAsyncRelayCommand NavigateCommand { get; }
        public FeatureItem(string title, IAsyncRelayCommand navigate)
        { Title = title; NavigateCommand = navigate; }
#else
        public ICommand NavigateCommand { get; }
        public FeatureItem(string title, ICommand navigate)
        { Title = title; NavigateCommand = navigate; }
#endif
    }
}

