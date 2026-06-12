using System;
using System.IO;
using System.Text.Json;

namespace OverlayDetector
{
    public class AppSettings
    {
        public bool EnableEmailNotifications { get; set; } = false;
        public string InterviewerEmail { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderAppPassword { get; set; } = string.Empty;
        public bool OnlyNotifyDuringFoundryIQ { get; set; } = true;
    }

    public static class SettingsService
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OverlayDetector",
            "settings.json"
        );

        public static AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            }
            return new AppSettings();
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsFilePath)!;
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }
    }
}
