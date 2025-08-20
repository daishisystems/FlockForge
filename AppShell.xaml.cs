using Microsoft.Maui.Controls;
using System;
using System.Linq;
namespace FlockForge
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            var regs = Routing.GetRegisteredRoutes().ToHashSet();
            void Reg(string route, Type pageType) { if (!regs.Contains(route)) Routing.RegisterRoute(route, pageType); }

            Reg("profile",  typeof(Views.Pages.ProfilePage));
            Reg("farms",    typeof(Views.Pages.FarmsPage));
            Reg("groups",   typeof(Views.Pages.GroupsPage));
            Reg("breeding", typeof(Views.Pages.BreedingPage));
            Reg("scanning", typeof(Views.Pages.ScanningPage));
            Reg("lambing",  typeof(Views.Pages.LambingPage));
            Reg("weaning",  typeof(Views.Pages.WeaningPage));
            Reg("reports",  typeof(Views.Pages.ReportsPage));
        }
    }
}

// Placeholder pages to satisfy routes if not already defined elsewhere
namespace FlockForge.Views.Pages
{
    public class BreedingPage : ContentPage { }
    public class ScanningPage : ContentPage { }
    public class LambingPage : ContentPage { }
    public class WeaningPage : ContentPage { }
    public class ReportsPage : ContentPage { }
}

