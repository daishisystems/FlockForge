using Microsoft.Extensions.Logging;

namespace FlockForge.Helpers
{
    public static class LoggingHelper
    {
        public static void ConfigureLogging(ILoggingBuilder logging)
        {
#if DEBUG
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter("Microsoft", LogLevel.Warning);
            logging.AddFilter("System", LogLevel.Warning);
#else
            logging.SetMinimumLevel(LogLevel.Warning);
            logging.AddFilter("Microsoft", LogLevel.Error);
            logging.AddFilter("System", LogLevel.Error);
#endif
        }
    }
}