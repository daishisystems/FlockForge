namespace FlockForge.Core.Models
{
    /// <summary>
    /// Farmer entity - simplified for offline-first development
    /// Firebase attributes will be added when proper mobile Firebase integration is implemented
    /// </summary>
    public class Farmer : BaseEntity
    {
        public string FirstName { get; set; } = string.Empty;
        
        public string LastName { get; set; } = string.Empty;
        
        public string Email { get; set; } = string.Empty;
        
        public string? PhoneNumber { get; set; }
        
        public string? Address { get; set; }
        
        public string? City { get; set; }
        
        public string? Province { get; set; }
        
        public string? PostalCode { get; set; }
        
        public string? Country { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        
        public string? ProfileImageUrl { get; set; }
        
        // Computed property
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}