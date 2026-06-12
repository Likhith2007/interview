using System;
using System.IO;

namespace OverlayDetector
{
    /// <summary>
    /// Simple logging service for recording overlay detection events.
    /// </summary>
    public class LoggingService
    {
        private readonly string _logFilePath;

        public LoggingService(string logFileName = "overlay_detection.log")
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "OverlayDetector");
            Directory.CreateDirectory(appFolder);
            _logFilePath = Path.Combine(appFolder, logFileName);
        }

        /// <summary>
        /// Logs a detected overlay.
        /// </summary>
        public void LogOverlay(OverlayWindow overlay)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Process: {overlay.ProcessName} | Title: {overlay.WindowTitle} | Class: {overlay.WindowClass}";
                
                lock (_logFilePath)
                {
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }

        /// <summary>
        /// Opens the log file in the default text editor.
        /// </summary>
        public void OpenLogFile()
        {
            try
            {
                if (File.Exists(_logFilePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_logFilePath)
                    {
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Could not open log file: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the full path to the log file.
        /// </summary>
        public string GetLogFilePath() => _logFilePath;
    }
}
