using System;
using System.IO;
using System.Text;

namespace DevDeck.Services
{
    public static class LoggerHelper
    {
        private static readonly object _lock = new();

        public static void LogToFile(string action, Exception? ex, string? projectPath = null, string? command = null)
        {
            try
            {
                string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DevDeck");
                string logsDir = Path.Combine(baseDir, "logs");
                Directory.CreateDirectory(logsDir);

                string logFile = Path.Combine(logsDir, "devdeck.log");
                
                var sb = new StringBuilder();
                sb.AppendLine("================================================================================");
                sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                sb.AppendLine("App version: 1.0.3.0");
                sb.AppendLine($"Action/Button: {action}");
                if (!string.IsNullOrEmpty(projectPath))
                {
                    sb.AppendLine($"Project path: {projectPath}");
                }
                if (!string.IsNullOrEmpty(command))
                {
                    sb.AppendLine($"Command: {command}");
                }

                if (ex != null)
                {
                    sb.AppendLine($"Exception type: {ex.GetType().FullName}");
                    sb.AppendLine($"Exception message: {ex.Message}");
                    sb.AppendLine($"Stack trace:\n{ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        sb.AppendLine($"Inner exception type: {ex.InnerException.GetType().FullName}");
                        sb.AppendLine($"Inner exception message: {ex.InnerException.Message}");
                        sb.AppendLine($"Inner exception stack trace:\n{ex.InnerException.StackTrace}");
                    }
                }
                else
                {
                    sb.AppendLine("No exception details (Logged info).");
                }
                sb.AppendLine();

                lock (_lock)
                {
                    File.AppendAllText(logFile, sb.ToString());
                }
            }
            catch
            {
                // Prevent crash if logger fails
            }
        }
    }
}
