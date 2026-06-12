using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;

namespace OverlayDetector
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _settings;

        public SettingsWindow()
        {
            InitializeComponent();
            _settings = SettingsService.LoadSettings();
            LoadSettingsIntoForm();
        }

        // ── Populate form fields from saved settings ──────────────────────────
        private void LoadSettingsIntoForm()
        {
            EnableEmailCheckBox.IsChecked   = _settings.EnableEmailNotifications;
            SenderEmailBox.Text             = _settings.SenderEmail;
            AppPasswordBox.Password         = _settings.SenderAppPassword;
            InterviewerEmailBox.Text        = _settings.InterviewerEmail;
        }

        // ── Save ──────────────────────────────────────────────────────────────
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields(out string error))
            {
                SetStatus(error, isError: true);
                return;
            }

            _settings.EnableEmailNotifications = EnableEmailCheckBox.IsChecked == true;
            _settings.SenderEmail              = SenderEmailBox.Text.Trim();
            _settings.SenderAppPassword        = AppPasswordBox.Password;
            _settings.InterviewerEmail         = InterviewerEmailBox.Text.Trim();

            SettingsService.SaveSettings(_settings);
            DialogResult = true;
            Close();
        }

        // ── Cancel ────────────────────────────────────────────────────────────
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // ── Send test email ───────────────────────────────────────────────────
        private async void TestEmail_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields(out string error))
            {
                SetStatus(error, isError: true);
                return;
            }

            TestEmailButton.IsEnabled = false;
            SetStatus("⏳  Sending test email…", isError: false);

            // Build a temporary settings object from the current form values
            var testSettings = new AppSettings
            {
                SenderEmail              = SenderEmailBox.Text.Trim(),
                SenderAppPassword        = AppPasswordBox.Password,
                InterviewerEmail         = InterviewerEmailBox.Text.Trim(),
                EnableEmailNotifications = true,
            };

            try
            {
                await EmailService.SendTestEmailAsync(testSettings);
                SetStatus($"✅  Test email sent to {testSettings.InterviewerEmail}", isError: false);
            }
            catch (Exception ex)
            {
                SetStatus($"❌  Failed: {ex.Message}", isError: true);
            }
            finally
            {
                TestEmailButton.IsEnabled = true;
            }
        }

        // ── Open app-password help link in browser ────────────────────────────
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private bool ValidateFields(out string error)
        {
            if (string.IsNullOrWhiteSpace(SenderEmailBox.Text))
            {
                error = "❌  Please enter your Gmail address.";
                return false;
            }
            if (AppPasswordBox.Password.Length == 0)
            {
                error = "❌  Please enter your Gmail App Password.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(InterviewerEmailBox.Text))
            {
                error = "❌  Please enter the interviewer's email address.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        private void SetStatus(string message, bool isError)
        {
            StatusLabel.Text       = message;
            StatusLabel.Foreground = isError
                ? new SolidColorBrush(Color.FromRgb(192, 57, 43))   // red
                : new SolidColorBrush(Color.FromRgb(39, 174, 96));   // green
        }
    }
}
