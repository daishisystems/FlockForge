using Microsoft.Maui.Controls;
using System;
using System.Linq;
using FlockForge.Views.Pages;

namespace FlockForge;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        var regs = Routing.GetRegisteredRoutes().ToHashSet();
        void Reg(string route, Type pageType) { if (!regs.Contains(route)) Routing.RegisterRoute(route, pageType); }

        Reg("profile",  typeof(ProfilePage));
        Reg("farms",    typeof(FarmsPage));
        Reg("groups",   typeof(GroupsPage));
        Reg("breeding", typeof(BreedingPage));
        Reg("scanning", typeof(ScanningPage));
        Reg("lambing",  typeof(LambingPage));
        Reg("weaning",  typeof(WeaningPage));
        Reg("reports",  typeof(ReportsPage));
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

