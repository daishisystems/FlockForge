namespace FlockForge.Core.Models
{
    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; }
        public string? PhotoUrl { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}