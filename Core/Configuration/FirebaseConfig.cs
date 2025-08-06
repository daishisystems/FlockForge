namespace FlockForge.Core.Configuration
{
    public class FirebaseConfig
    {
        // Core Firebase settings
        public string ProjectId { get; set; } = string.Empty;
        public string ApplicationId { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string StorageBucket { get; set; } = string.Empty;

        // Timeout configurations
        public int DefaultOperationTimeoutMs { get; set; } = 30000;
        public int StorageOperationTimeoutMs { get; set; } = 5000;
        public int AuthRefreshTimeoutMs { get; set; } = 10000;
        
        // Retry configurations
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 1000;
        
        // Cache configurations
        public int FirestoreCacheSizeBytes { get; set; } = 104857600; // 100MB
        public int MaxListeners { get; set; } = 50;
        public int MaxCacheItems { get; set; } = 1000;
        
        // Token refresh
        public int TokenRefreshIntervalMinutes { get; set; } = 30;
        
        // Collection name mappings
        public Dictionary<string, string> CollectionNames { get; set; } = new()
        {
            ["farm"] = "farms",
            ["farmer"] = "farmers",
            ["lambingseason"] = "lambing_seasons",
            ["breeding"] = "breeding",
            ["scanning"] = "scanning",
            ["lambing"] = "lambing",
            ["weaning"] = "weaning"
        };
    }
}