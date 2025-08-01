namespace FlockForge.Core.Models
{
    public class AuthResult
    {
        public bool IsSuccess { get; set; }
        public User? User { get; set; }
        public string? ErrorMessage { get; set; }
        public bool RequiresEmailVerification { get; set; }
        
        public static AuthResult Success(User user, bool requiresEmailVerification = false)
        {
            return new AuthResult 
            { 
                IsSuccess = true, 
                User = user,
                RequiresEmailVerification = requiresEmailVerification
            };
        }
        
        public static AuthResult Failure(string errorMessage)
        {
            return new AuthResult 
            { 
                IsSuccess = false, 
                ErrorMessage = errorMessage 
            };
        }
    }
}