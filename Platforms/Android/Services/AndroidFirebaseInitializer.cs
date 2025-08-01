using FlockForge.Core.Interfaces;

namespace FlockForge.Platforms.Android.Services
{
    public class AndroidFirebaseInitializer : IFirebaseInitializer
    {
        public void Initialize()
        {
            // With Google.Cloud.Firestore, platform-specific initialization is not required
            // The FirestoreService handles all initialization and configuration
            // This method is kept for interface compliance but no action is needed
        }
    }
}