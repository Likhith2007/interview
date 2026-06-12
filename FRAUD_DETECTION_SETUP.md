# Interview Fraud Detection Integration Guide

## Overview
Your overlay detector now integrates with **Microsoft Foundry AI** to intelligently detect interview fraud attempts using AI tools like ParakeetAI.

## Architecture

```
Overlay Detected
    ↓
FraudAnalysisService (analyzes app)
    ↓
Foundry Agent (intelligent analysis)
    ├─→ Is Suspicious? (yes/no)
    ├─→ Risk Level (Low/Medium/High)
    ├─→ Threat Type (AI Tool, Screen Recorder, etc.)
    └─→ Generate Alert Email
    ↓
EmailService (sends via Gmail)
    ↓
Interviewer notified
```

## Components

### 1. FraudAnalysisService.cs
- Calls Foundry AI agent to analyze overlays
- Fallback to local pattern matching if Foundry unavailable
- Returns structured fraud analysis result

### 2. Enhanced EmailService.cs
- New `SendFraudAlertAsync()` method for intelligent alerts
- Uses AI-generated email subject/body

### 3. Updated MainWindow.xaml.cs
- Calls fraud analysis before sending emails
- Tracks detection frequency for escalation

## Setup Steps

### Step 1: Deploy Foundry Agent
```bash
cd c:\Users\ilikh\Downloads\interview-fraud-agent
pip install -r requirements.txt
python src/app.py
```

The agent will start on `http://localhost:8088/responses`

### Step 2: Configure in Your App

Edit your MainWindow.xaml.cs initialization:
```csharp
public MainWindow()
{
    InitializeComponent();
    // ... existing code ...
    
    // Initialize fraud analysis service
    _fraudAnalysisService = new FraudAnalysisService("http://localhost:8088");
}
```

### Step 3: Update Overlay Detection Handler

The system now:
1. Detects overlay
2. Analyzes with Foundry AI
3. If suspicious (Medium/High risk), sends intelligent email alert
4. If low risk, just logs it

## Examples

### Example 1: ParakeetAI Detected
```
Process: parakeet-ai.exe
Window: Parakeet AI Interview Helper
↓
Foundry Analysis:
- isSuspicious: true
- riskLevel: "High"
- threatType: "AI Interview Tool (ParakeetAI)"
- threatScore: 85.0
↓
Email Subject: 🚨 HIGH RISK - Interview Fraud Detected: AI Interview Tool (ParakeetAI)
Email Body: Detailed analysis + recommendation to terminate interview
```

### Example 2: Unknown Overlay
```
Process: unknown.exe
Window: Some App
↓
Local Analysis (Foundry unavailable):
- isSuspicious: false
- riskLevel: "Low"
- threatScore: 25.0
↓
Email NOT sent (low risk)
Just logged
```

## Threat Database

### High Risk (Score 75-100)
- ParakeetAI, ChatGPT, Claude, Copilot, Gemini (AI tools)
- ProctorU bypass tools
- Remote Access: TeamViewer, AnyDesk

### Medium Risk (Score 50-74)
- Screen Recorders: OBS, Camtasia, ScreenFlow
- VPN tools
- Browser DevTools

### Low Risk (Score 0-49)
- Unknown applications
- Applications appearing < 2x

## Configuration

### Optional: Whitelist Applications
Edit `SettingsService.cs` to add whitelisted apps:
```csharp
private readonly HashSet<string> _whitelistedApps = new()
{
    "notepad.exe",
    "calculator.exe"
};
```

### Optional: Custom Threat Database
Extend `DetermineThreatType()` in `FraudAnalysisService.cs` with your own rules.

## Email Alerts

### Sent When
- High Risk (score ≥75): Always
- Medium Risk (50-74): On first detection, then every 30 seconds
- Low Risk (<50): Never (just logged)

### Email Format

```
Interview Fraud Detection Alert
================================

Risk Level: [High/Medium/Low]
Threat Type: [AI Tool, Screen Recorder, etc.]
Threat Score: [0-100]
Detection Count: [N times detected]

Detected Application:
  Process: [process name]
  Window: [window title]
  Time: [timestamp]

Analysis:
[Detailed analysis from AI]

Recommendation:
[Action to take]
```

## Deployment Options

### Local Development
```bash
# Terminal 1: Start Foundry Agent
cd interview-fraud-agent && python src/app.py

# Terminal 2: Run your overlay detector
# In VS Code, press F5
```

### Production: Docker Deployment
```bash
cd interview-fraud-agent
docker-compose up -d

# Your app connects to http://agent-service:8088/responses
```

### Production: Azure Foundry Deployment
See `interview-fraud-agent/README.md` for Azure container deployment steps.

## Testing

### Test 1: Simulate ParakeetAI Detection
```csharp
// In MainWindow.xaml.cs
var testOverlay = new OverlayWindow 
{
    ProcessName = "parakeet-ai.exe",
    WindowTitle = "Parakeet AI Interview Helper",
    DetectedAt = DateTime.Now
};

var result = await _fraudAnalysisService.AnalyzeOverlayAsync(testOverlay, detectionCount: 3);
// Expected: isSuspicious=true, riskLevel="High", threatScore ~85
```

### Test 2: Check Fallback Analysis
```csharp
// Stop the Foundry agent
// Then trigger overlay detection
// System will use local pattern matching
```

## Troubleshooting

### Issue: Foundry Agent Not Responding
**Solution**: Start the agent from interview-fraud-agent folder:
```bash
cd c:\Users\ilikh\Downloads\interview-fraud-agent
python src/app.py
```

### Issue: Emails Not Being Sent
**Solution**: Check email settings:
```csharp
var settings = _settingsService.LoadSettings();
// Verify:
// - SenderEmail is configured
// - SenderAppPassword is a Gmail App Password (not your Gmail password)
// - InterviewerEmail is set
```

### Issue: Too Many Emails
**Solution**: The system already has deduplication:
- Prevents same overlay from showing multiple popups (30s cooldown)
- Emails only sent for Medium+ risk
- Same overlay won't email twice for 30 seconds

## Next Steps

1. ✅ Deploy Foundry agent
2. ✅ Update MainWindow initialization
3. ✅ Test with sample overlays
4. ✅ Configure email settings
5. ✅ Deploy to production

## Additional Resources

- [Foundry Agent Code](../interview-fraud-agent/)
- [FraudAnalysisService.cs](./FraudAnalysisService.cs)
- [EmailService.cs](./EmailService.cs)
- [MainWindow.xaml.cs](./MainWindow.xaml.cs)

---

**Questions?** Check the README in the interview-fraud-agent folder for detailed agent documentation.
