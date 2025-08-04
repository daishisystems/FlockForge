using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FlockForge.Services.Firebase;

namespace FlockForge
{
    /// <summary>
    /// Simple test class to verify Firebase authentication debugging
    /// </summary>
    public static class TestFirebaseDebug
    {
        public static async Task RunDebugTestsAsync(IServiceProvider serviceProvider)
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("TestFirebaseDebug");
            var authService = serviceProvider.GetRequiredService<FirebaseAuthenticationService>();
            
            logger.LogInformation("=== Starting Firebase Debug Tests ===");
            
            try
            {
                // Test 1: Debug authentication state
                logger.LogInformation("Test 1: Debugging authentication state...");
                var debugResult = await authService.DebugAuthenticationStateAsync();
                logger.LogInformation("Authentication state debug result: {Result}", debugResult);
                
                // Test 2: Test Firebase configuration
                logger.LogInformation("Test 2: Testing Firebase configuration...");
                var configResult = await authService.TestFirebaseConfigurationAsync();
                logger.LogInformation("Firebase configuration test result: {Result}", configResult);
                
                // Test 3: Test sign-in with invalid credentials (should fail gracefully)
                logger.LogInformation("Test 3: Testing sign-in with invalid credentials...");
                var signInResult = await authService.SignInWithEmailPasswordAsync("", "");
                logger.LogInformation("Invalid credentials test - Success: {Success}, Error: {Error}", 
                    signInResult.IsSuccess, signInResult.ErrorMessage);
                
                // Test 4: Test sign-in with malformed email
                logger.LogInformation("Test 4: Testing sign-in with malformed email...");
                var malformedResult = await authService.SignInWithEmailPasswordAsync("invalid-email", "password123");
                logger.LogInformation("Malformed email test - Success: {Success}, Error: {Error}", 
                    malformedResult.IsSuccess, malformedResult.ErrorMessage);
                
                logger.LogInformation("=== Firebase Debug Tests Completed ===");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during Firebase debug tests");
            }
        }
    }
}