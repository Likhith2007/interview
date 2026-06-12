# Overlay Detector - Video Interview Monitor

A Windows desktop application built with C# and WPF that detects unauthorized overlay windows during video interviews using the Win32 API.

## Features

✅ **Real-time Window Monitoring** - Continuously monitors all top-level windows using Win32 API (`EnumWindows`, `GetForegroundWindow`)

✅ **Overlay Detection** - Identifies windows with `WS_EX_TOPMOST` extended style (floating/overlay windows)

✅ **Smart Whitelist** - Compares against a whitelist of authorized applications:
- Microsoft Teams
- Google Chrome (Google Meet)
- Mozilla Firefox
- Zoom
- Skype
- Webex
- OBS Studio
- And other system essentials

✅ **Warning Alerts** - Shows popup warnings when unauthorized overlays are detected with:
- Application name
- Window title
- Detection timestamp
- 5-second auto-close behavior

✅ **Comprehensive Logging** - Records all detections to `%APPDATA%\OverlayDetector\overlay_detection.log`

✅ **User-Friendly UI**:
- Start/Stop monitoring button
- Real-time DataGrid showing all detected overlays
- Detection counter
- View logs button
- Clear history option

✅ **Non-blocking Monitoring** - Uses async tasks to perform continuous monitoring without freezing the UI

## Project Structure

```
OverlayDetector/
├── App.xaml                   # WPF Application root
├── App.xaml.cs                # Application code-behind
├── MainWindow.xaml            # Main UI (controls, grid)
├── MainWindow.xaml.cs         # Main window logic (monitoring, events)
├── WarningWindow.xaml         # Popup warning UI
├── WarningWindow.xaml.cs      # Popup warning code-behind
├── WindowScanner.cs           # Core overlay detection logic
│   ├── OverlayWindow class    # Overlay window data model
│   ├── WhitelistProcesses     # Application whitelist
│   └── Monitoring methods     # ScanForOverlays(), IsWhitelisted()
├── Win32NativeMethods.cs      # P/Invoke declarations for Win32 API
│   ├── EnumWindows            # Enumerate all windows
│   ├── GetForegroundWindow    # Get active window
│   ├── GetWindowLong          # Read window extended styles
│   ├── IsWindowVisible        # Check window visibility
│   ├── IsTopMostWindow        # Check WS_EX_TOPMOST flag
│   └── Helper methods         # GetWindowTitle, GetProcessName, etc.
├── LoggingService.cs          # File-based logging system
│   ├── LogOverlay()           # Log detected overlays
│   ├── OpenLogFile()          # View logs in default editor
│   └── GetLogFilePath()       # Get log file location
└── OverlayDetector.csproj     # Project configuration
```

## Prerequisites

- **Windows 10 or later**
- **.NET 6.0 or higher** (Windows Desktop runtime)
- **Visual Studio 2022** (or Visual Studio Code with .NET extension)
  - Or: **.NET SDK 6.0+** (for command-line building)

## Installation & Setup

### Option 1: Using Visual Studio 2022

1. Open Visual Studio 2022
2. File → Open → Project/Solution
3. Select `OverlayDetector.csproj`
4. Press `F5` to build and run

### Option 2: Using .NET CLI

```powershell
cd C:\Users\ilikh\Downloads\micro
dotnet restore
dotnet build
dotnet run
```

### Option 3: Build Release Version

```powershell
cd C:\Users\ilikh\Downloads\micro
dotnet publish -c Release -r win-x64 --self-contained
# Executable will be in: bin\Release\net6.0-windows\win-x64\publish\
```

## How It Works

### 1. **Initialization**
- MainWindow initializes WindowScanner and LoggingService
- Subscribes to scanner events (OverlayDetected, OverlaysUpdated)
- UI bound to ObservableCollection of detected overlays

### 2. **Monitoring Process**
```
Start Button Clicked
    ↓
StartMonitoring_Click() starts MonitoringTask()
    ↓
Continuous loop every 1 second (SCAN_INTERVAL_MS = 1000ms)
    ↓
WindowScanner.ScanForOverlays()
    ↓
EnumWindows() iterates all top-level windows
    ↓
For each window:
    - Check if visible & top-level
    - Check if TOPMOST extended style (WS_EX_TOPMOST)
    - Check against whitelist
    ↓
If unauthorized overlay detected:
    - Raise OverlayDetected event
    - Log to file
    - Show warning popup
    ↓
Update UI with current detections
    ↓
Repeat (unless Stop clicked or app closed)
```

### 3. **Detection Algorithm**

```
For each window:
  IF IsWindowVisible(hWnd) AND IsRelevantWindow(hWnd) THEN
    IF IsTopMostWindow(hWnd) AND NOT IsWhitelisted(hWnd) THEN
      --> OVERLAY DETECTED <--
    END IF
  END IF
END FOR
```

### 4. **Whitelist Matching**
- **Process Name Match**: Checks if any whitelisted process name is contained in the window's process
  - Example: `chrome.exe` matches "chrome" in whitelist
- **Window Class Match**: Checks for system window classes (Shell_*, Progman, WorkerW)
- **Case-Insensitive**: All comparisons are case-insensitive for robustness

## Key Windows API Functions Used

| Function | Purpose |
|----------|---------|
| `EnumWindows()` | Enumerate all top-level windows |
| `GetForegroundWindow()` | Get currently active window |
| `GetWindowLong()` | Read extended window styles |
| `IsWindowVisible()` | Check if window is visible |
| `GetParent()` | Check if window is child window |
| `GetWindowText()` | Retrieve window title |
| `GetClassName()` | Get window class name |
| `GetWindowThreadProcessId()` | Get process ID from window |

## Configuration

### Adjust Monitoring Frequency
In `MainWindow.xaml.cs`, change:
```csharp
private const int SCAN_INTERVAL_MS = 1000; // Change to desired milliseconds
```

### Add/Remove from Whitelist
In `WindowScanner.cs`, modify:
```csharp
private static readonly HashSet<string> WhitelistedProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "teams",           // Add or remove application names here
    "chrome",
    "firefox",
    // ... more apps
};
```

## Log File Location

Logs are stored at:
```
%APPDATA%\OverlayDetector\overlay_detection.log
```

Example log entry:
```
[2026-06-01 14:23:45] Process: discord | Title: Discord | Class: Chrome_WidgetWin_0
[2026-06-01 14:24:12] Process: slack | Title: Slack | Class: Chrome_WidgetWin_0
```

## Usage Example

1. **Start the application**
   ```
   DoubleClick OverlayDetector.exe
   ```

2. **Click "Start Monitoring"** button
   - Status changes to "Monitoring..."
   - Application begins scanning every second

3. **When an overlay is detected**:
   - Warning popup appears with app name and timestamp
   - Entry added to real-time list
   - Detection logged to file

4. **View detections**:
   - Check real-time list in DataGrid
   - Click "View Logs" to see full history

5. **Stop monitoring**:
   - Click "Stop Monitoring" button
   - Click "Clear List" to reset detections

## Security Considerations

⚠️ **Important Notes**:
- This application monitors **all** windows with TOPMOST style, including legitimate ones
- Some applications use TOPMOST for legitimate purposes (notifications, reminders)
- The whitelist can be customized based on your needs
- Run with appropriate permissions (standard user is fine for most cases)

## Troubleshooting

### Application won't start
- Ensure .NET 6.0+ is installed: `dotnet --version`
- Try: `dotnet tool update -g dotnet-tool`

### No overlays detected
- Overlays must have WS_EX_TOPMOST style set
- Most applications don't use this flag
- Try opening floating dialogs in Discord, Slack, etc.

### Permission denied errors
- May need to run as administrator for some system windows
- Right-click → Run as administrator

### High CPU usage
- Reduce SCAN_INTERVAL_MS value
- Optimize whitelist to skip more windows

## Building from Source

### Requirements
- Visual Studio 2022 Community (free) or newer
- .NET 6.0 Windows Desktop SDK

### Build Steps
```powershell
# Restore dependencies
dotnet restore

# Build debug version
dotnet build

# Build release version  
dotnet build -c Release

# Run the application
dotnet run

# Create standalone executable
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## Architecture Highlights

### Modular Design
- **WindowScanner**: Encapsulates all detection logic
- **Win32NativeMethods**: Isolated P/Invoke declarations
- **LoggingService**: Separate logging concern
- **WarningWindow**: Reusable warning component

### Thread Safety
- Uses Dispatcher.Invoke() for UI updates from background thread
- Lock used for file I/O in LoggingService
- CancellationToken for graceful shutdown

### Performance
- Async monitoring loop (non-blocking)
- Efficient whitelist using HashSet<string>
- Only-on-new events (tracks previously detected handles)
- Single scan per interval

### Error Handling
- Try-catch blocks for Win32 API calls
- Graceful fallback when process enumeration fails
- User-friendly error messages

## Code Examples

### Detecting Overlays
```csharp
// WindowScanner.ScanForOverlays() - Core detection
Win32NativeMethods.EnumWindows((hWnd, lParam) =>
{
    if (IsRelevantWindow(hWnd))
    {
        if (Win32NativeMethods.IsTopMostWindow(hWnd) && !IsWhitelisted(hWnd))
        {
            // Overlay detected!
        }
    }
    return true;
}, IntPtr.Zero);
```

### Event Subscription
```csharp
// In MainWindow constructor
_windowScanner.OverlayDetected += OnOverlayDetected;
_windowScanner.OverlaysUpdated += OnOverlaysUpdated;
```

### Async Monitoring
```csharp
private async Task MonitoringTask(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        _windowScanner.ScanForOverlays();
        await Task.Delay(SCAN_INTERVAL_MS, cancellationToken);
    }
}
```

## System Requirements

- **OS**: Windows 10 or later (32-bit or 64-bit)
- **RAM**: 100 MB minimum
- **.NET Runtime**: 6.0+ (included with Visual Studio or downloadable separately)
- **Permissions**: Standard user (admin for some advanced features optional)

## Future Enhancements

- 🔄 Settings panel for custom whitelists
- 📊 Statistics dashboard (overlays per app, etc.)
- 🔊 Audio alerts for overlay detection
- 📱 Notification system integration
- 🎯 Process blocking/killing functionality
- 🌐 Remote monitoring across multiple machines
- 🔐 Windows Defender/SmartScreen integration

## License

This is a personal/educational project. Modify and distribute freely.

## Support

For issues or questions, review the troubleshooting section above or examine the source code - it's well-commented throughout.

---

**Author**: Overlay Detection System  
**Version**: 1.0  
**Last Updated**: June 1, 2026  
**Platform**: Windows (.NET 6.0+)
