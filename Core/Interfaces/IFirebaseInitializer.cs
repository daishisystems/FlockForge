namespace FlockForge.Core.Interfaces
{
    /// <summary>
    /// Interface for platform-specific Firebase initialization
    /// </summary>
    public interface IFirebaseInitializer
    {
        /// <summary>
        /// Initializes Firebase for the current platform
        /// </summary>
        void Initialize();
    }
}