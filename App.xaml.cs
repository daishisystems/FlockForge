using FlockForge.Services.Firebase;

namespace FlockForge;

public partial class App : Application
{
    private readonly IFirebaseService _firebaseService;
    
    public App(IFirebaseService firebaseService)
    {
        InitializeComponent();
        _firebaseService = firebaseService;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // For now, always go to AppShell which contains the login page
        // In a real implementation, you would check authentication status
        // and navigate accordingly
        return new Window(new AppShell());
    }
}
