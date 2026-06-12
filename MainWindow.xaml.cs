using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OverlayDetector
{
    public partial class MainWindow : Window
    {
        private readonly WindowScanner _windowScanner = new();
        private readonly LoggingService _loggingService = new();
        private readonly FraudAnalysisService _fraudAnalysisService = new();
        private ObservableCollection<OverlayWindow> _detectedOverlaysList = new();
        private CancellationTokenSource? _monitoringCancellationTokenSource;
        private bool _isMonitoring = false;
        private const int SCAN_INTERVAL_MS = 1000; // Scan every 1 second

        // Track which overlays have already shown warnings to prevent duplicate popups
        private readonly Dictionary<IntPtr, DateTime> _warningShownTimes = new();
        private const int WARNING_DEDUP_INTERVAL_MS = 30000; // Show warning again after 30 seconds

        // Track overlay detection frequency for escalation
        private readonly Dictionary<IntPtr, int> _detectionCount = new();
        private const int EMAIL_ALERT_THRESHOLD = 50; // Send email if risk score >= 50 (Medium+)

        public MainWindow()
        {
            InitializeComponent();
            OverlaysDataGrid.ItemsSource = _detectedOverlaysList;

            // Subscribe to scanner events
            _windowScanner.OverlayDetected += OnOverlayDetected;
            _windowScanner.OverlaysUpdated += OnOverlaysUpdated;
        }

        /// <summary>
        /// Handles the Start Monitoring button click.
        /// </summary>
        private void StartMonitoring_Click(object sender, RoutedEventArgs e)
        {
            if (_isMonitoring)
                return;

            _isMonitoring = true;
            _monitoringCancellationTokenSource = new CancellationTokenSource();
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            StatusText.Text = "Status: Monitoring...";
            StatusText.Foreground = System.Windows.Media.Brushes.Green;
            _detectedOverlaysList.Clear();

            // Start the monitoring task asynchronously
            _ = MonitoringTask(_monitoringCancellationTokenSource.Token);
        }

        /// <summary>
        /// Handles the Stop Monitoring button click.
        /// </summary>
        private void StopMonitoring_Click(object sender, RoutedEventArgs e)
        {
            if (!_isMonitoring)
                return;

            _isMonitoring = false;
            _monitoringCancellationTokenSource?.Cancel();
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            StatusText.Text = "Status: Idle";
            StatusText.Foreground = System.Windows.Media.Brushes.Gray;
        }

        /// <summary>
        /// Handles the Clear Detections button click.
        /// </summary>
        private void ClearDetections_Click(object sender, RoutedEventArgs e)
        {
            _detectedOverlaysList.Clear();
            _windowScanner.ClearDetectedOverlays();
            CountText.Text = "Overlays Detected: 0";
            WarningText.Text = "";
        }

        /// <summary>
        /// Handles the View Logs button click.
        /// </summary>
        private void ViewLogs_Click(object sender, RoutedEventArgs e)
        {
            _loggingService.OpenLogFile();
        }

        /// <summary>
        /// Handles the Debug Info button click.
        /// </summary>
        private void ShowDebug_Click(object sender, RoutedEventArgs e)
        {
            string debugInfo = string.Join("\n", _windowScanner.DebugLog.TakeLast(50));
            if (string.IsNullOrEmpty(debugInfo))
                debugInfo = "No debug information available yet.\n\nStart monitoring to see debug output.";

            var debugWindow = new Window
            {
                Title = "Debug Log - Last 50 Entries",
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var textBlock = new TextBlock
            {
                Text = debugInfo,
                Padding = new Thickness(10),
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 11
            };

            scrollViewer.Content = textBlock;
            debugWindow.Content = scrollViewer;
            debugWindow.Show();
        }

        /// <summary>
        /// Handles the Settings button click — opens the email settings dialog.
        /// </summary>
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow { Owner = this };
            settingsWindow.ShowDialog();
        }


        /// <summary>
        /// Continuous monitoring task that runs in the background.
        /// </summary>
        private async Task MonitoringTask(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Perform window scan
                    _windowScanner.ScanForOverlays();

                    // Wait before next scan
                    await Task.Delay(SCAN_INTERVAL_MS, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Monitoring was cancelled
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during monitoring: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Called when a new overlay is detected.
        /// </summary>
        private void OnOverlayDetected(object? sender, OverlayWindow overlay)
        {
            Dispatcher.Invoke(async () =>
            {
                // Log the detection
                _loggingService.LogOverlay(overlay);

                // Track detection frequency for this overlay
                IntPtr hWnd = overlay.Handle;
                if (!_detectionCount.ContainsKey(hWnd))
                {
                    _detectionCount[hWnd] = 0;
                }
                _detectionCount[hWnd]++;
                int detectionCount = _detectionCount[hWnd];

                // Check if we've already shown a warning for this overlay recently
                bool shouldShowWarning = false;

                if (_warningShownTimes.ContainsKey(hWnd))
                {
                    // Check if enough time has passed since last warning
                    TimeSpan timeSinceLastWarning = DateTime.Now - _warningShownTimes[hWnd];
                    if (timeSinceLastWarning.TotalMilliseconds >= WARNING_DEDUP_INTERVAL_MS)
                    {
                        shouldShowWarning = true;
                        _warningShownTimes[hWnd] = DateTime.Now;
                    }
                }
                else
                {
                    // First time seeing this overlay, show warning
                    shouldShowWarning = true;
                    _warningShownTimes[hWnd] = DateTime.Now;
                }

                // Show warning popup only if appropriate
                if (shouldShowWarning)
                {
                    ShowWarningPopup(overlay);
                }

                // Analyze overlay using Foundry AI fraud detection
                try
                {
                    var fraudAnalysis = await _fraudAnalysisService.AnalyzeOverlayAsync(overlay, detectionCount);
                    
                    // Send email alert if medium or high risk
                    if (fraudAnalysis.ThreatScore >= EMAIL_ALERT_THRESHOLD)
                    {
                        try
                        {
                            var settings = SettingsService.LoadSettings();
                            if (settings != null && !string.IsNullOrWhiteSpace(settings.SenderEmail))
                            {
                                await EmailService.SendFraudAlertAsync(settings, fraudAnalysis);
                                Console.WriteLine($"Fraud alert email sent: {fraudAnalysis.ThreatType} (Score: {fraudAnalysis.ThreatScore:F0})");
                            }
                        }
                        catch (Exception emailEx)
                        {
                            Console.WriteLine($"Error sending fraud alert email: {emailEx.Message}");
                        }
                    }
                }
                catch (Exception analysisEx)
                {
                    Console.WriteLine($"Error analyzing overlay for fraud: {analysisEx.Message}");
                }
            });
        }

        /// <summary>
        /// Called when the list of overlays is updated.
        /// </summary>
        private void OnOverlaysUpdated(object? sender, System.Collections.Generic.List<OverlayWindow> overlays)
        {
            Dispatcher.Invoke(() =>
            {
                // Clean up warning tracking for overlays that are no longer detected
                var detectedHandles = new HashSet<IntPtr>(overlays.Select(o => o.Handle));
                var handlesToRemove = _warningShownTimes.Keys.Where(h => !detectedHandles.Contains(h)).ToList();
                foreach (var handle in handlesToRemove)
                {
                    _warningShownTimes.Remove(handle);
                    _detectionCount.Remove(handle); // Also clean up detection count
                }

                // Update the data grid
                _detectedOverlaysList.Clear();
                foreach (var overlay in overlays)
                {
                    _detectedOverlaysList.Add(overlay);
                }

                // Update count
                CountText.Text = $"Overlays Detected: {overlays.Count}";

                // Update warning text if there are overlays
                if (overlays.Count > 0)
                {
                    WarningText.Text = $"⚠️  {overlays.Count} unauthorized overlay(s) detected!";
                    WarningText.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    WarningText.Text = "";
                }
            });
        }

        /// <summary>
        /// Displays a warning popup when an overlay is detected.
        /// </summary>
        private void ShowWarningPopup(OverlayWindow overlay)
        {
            string message = $"Overlay Detected!\n\n" +
                           $"Process: {overlay.ProcessName}\n" +
                           $"Window: {overlay.WindowTitle}\n" +
                           $"Time: {overlay.DetectedAt:HH:mm:ss}\n\n" +
                           $"Please close this window if it's unauthorized.";

            // Create and show warning window
            var warningWindow = new WarningWindow(message, overlay.ProcessName);
            warningWindow.Owner = this;
            warningWindow.Show();

            // Auto-close warning after 5 seconds
            Task.Delay(5000).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (warningWindow.IsVisible)
                    {
                        warningWindow.Close();
                    }
                });
            });
        }

        /// <summary>
        /// Handles the window closing event.
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Stop monitoring if active
            if (_isMonitoring)
            {
                _monitoringCancellationTokenSource?.Cancel();
            }

            // Clean up resources
            _windowScanner.ClearDetectedOverlays();
            _warningShownTimes.Clear();
            _detectionCount.Clear();
        }
    }
}
