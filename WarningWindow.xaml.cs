using System.Windows;

namespace OverlayDetector
{
    public partial class WarningWindow : Window
    {
        public WarningWindow(string message, string processName)
        {
            InitializeComponent();
            MessageText.Text = message;
            Title = $"⚠️ Overlay Warning - {processName}";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
