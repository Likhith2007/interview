# API Reference

## Overview

The Interview Fraud Detection Agent exposes the `/responses` endpoint following the Microsoft Agent Framework protocol.

## Endpoints

### POST /responses

Main endpoint for fraud analysis requests.

**URL**: `http://localhost:8088/responses`

**Method**: `POST`

**Content-Type**: `application/json`

#### Request Schema

```json
{
  "messages": [
    {
      "role": "user",
      "content": "string - overlay detection data"
    }
  ]
}
```

#### Response Schema

```json
{
  "status": "success" | "error",
  "analysis": {
    "isSuspicious": boolean,
    "riskLevel": "Low" | "Medium" | "High",
    "reason": "string - explanation of findings",
    "recommendation": "string - suggested action",
    "emailSubject": "string - formatted email subject",
    "emailBody": "string - formatted email body",
    "threatType": "string - category of threat",
    "threatScore": number,
    "detectionCount": number,
    "timestamp": "ISO 8601 timestamp",
    "indicators": [
      "string - behavioral indicator"
    ]
  },
  "message": "string - status message"
}
```

---

### GET /health

Health check endpoint.

**URL**: `http://localhost:8088/health`

**Method**: `GET`

**Response**:
```json
{
  "status": "healthy",
  "agent": "interview-fraud-detector"
}
```

---

### GET /

Root endpoint with service information.

**URL**: `http://localhost:8088/`

**Method**: `GET`

**Response**:
```json
{
  "name": "Interview Fraud Detection Agent",
  "version": "1.0.0",
  "endpoints": {
    "POST /responses": "Analyze overlay detection data",
    "GET /health": "Health check"
  }
}
```

---

## Input Formats

The agent supports multiple input formats for flexibility:

### Format 1: Key-Value Pairs

```
processName=parakeet-ai.exe, windowTitle=Parakeet AI Interview Helper, detectionCount=3, timestamp=2024-01-15T14:30:00Z
```

**Parsing Rules**:
- Comma-separated key=value pairs
- Whitespace is trimmed
- Keys are case-insensitive
- Snake_case converted to camelCase

### Format 2: JSON Object

```json
{
  "processName": "parakeet-ai.exe",
  "windowTitle": "Parakeet AI Interview Helper",
  "detectionCount": 3,
  "timestamp": "2024-01-15T14:30:00Z"
}
```

### Format 3: Mixed Natural Language

```
Process is parakeet-ai.exe, window title is "Parakeet AI Interview Helper", detected 3 times
```

---

## Output Fields Reference

### isSuspicious: boolean
Whether the overlay indicates fraudulent activity. Determined by threat score threshold (60+).

### riskLevel: string
Risk classification:
- **Low**: Score < 60 - Monitor but not urgent
- **Medium**: Score 60-79 - Alert interviewer
- **High**: Score 80+ - Immediate action required

### reason: string
Explanation of the risk assessment, including:
- Threat type identification
- Database match confirmation
- Behavioral indicators
- Frequency analysis

### recommendation: string
Suggested action based on risk level:
- Low: "Log incident for reference"
- Medium: "Alert interviewer to suspicious activity"
- High: "IMMEDIATE ACTION: Notify interviewer, consider ending interview"

### emailSubject: string
Pre-formatted email subject line with emoji and risk indicator:
- High: "🚨 HIGH RISK Interview Fraud Alert"
- Medium: "⚠️ MEDIUM RISK Interview Activity Alert"
- Low: "ℹ️ Interview Activity Log"

### emailBody: string
Formatted email body with:
- Timestamp
- Process and window details
- Threat classification
- Detection count
- Recommended actions
- Compliance notes

### threatType: string
Category of detected threat:
- "AI Interview Tool"
- "Screen Recorder"
- "Proctoring Cheat"
- "Remote Access Tool"
- "VPN/Proxy Tool"
- "Browser DevTools/Analyzer"
- null (if not known)

### threatScore: number
Risk score from 0-100:
- 0-30: Minimal risk
- 31-60: Low to medium risk
- 61-79: Medium to high risk
- 80-100: High to critical risk

### detectionCount: number
Number of times this overlay was detected in the current session.

### timestamp: string
ISO 8601 formatted detection timestamp with timezone.

### indicators: array[string]
List of suspicious behavioral indicators:
- "Attempts to hide or run in background"
- "Running from suspicious directory"
- "Obfuscated process name"
- "Possible process spoofing"
- "Matches [threat type] signature"

---

## Known Threats

### AI Interview Tools (Risk: 85)
- ParakeetAI, InterviewAI, ChatGPT, Claude, Copilot, Gemini, Grok, Perplexity

### Screen Recorders (Risk: 70)
- OBS Studio, Camtasia, ScreenFlow, Bandicam, Fraps, Dxtory, NVIDIA ShadowPlay

### Proctoring Cheats (Risk: 90)
- ProctorU bypass tools, Blackboard cheats, Canvas bypass, Test helper tools

### Remote Access (Risk: 75)
- TeamViewer, AnyDesk, Chrome Remote Desktop, Microsoft Remote Desktop, VNC

### VPN/Proxy Tools (Risk: 45)
- ProtonVPN, Windscribe, ExpressVPN, NordVPN, Tor, General proxy tools

### Browser DevTools (Risk: 50)
- Chrome DevTools, Firefox Inspector, Edge Developer Tools, Network analyzers

---

## Error Responses

### Missing Required Fields

```json
{
  "error": "Missing required fields",
  "message": "Please provide: processName, windowTitle, detectionCount",
  "example": "processName=parakeet-ai.exe, windowTitle=Parakeet AI, detectionCount=3"
}
```

### Invalid Input

```json
{
  "error": "Invalid input",
  "message": "Error parsing input: [error details]"
}
```

### Internal Error

```json
{
  "error": "Internal error",
  "message": "Error processing request: [error details]"
}
```

---

## Examples

### Example 1: ParakeetAI Detection

**Request**:
```bash
curl -X POST http://localhost:8088/responses \
  -H "Content-Type: application/json" \
  -d '{
    "messages": [
      {
        "role": "user",
        "content": "processName=parakeet-ai.exe, windowTitle=Parakeet AI Interview Helper, detectionCount=3"
      }
    ]
  }'
```

**Response**:
```json
{
  "status": "success",
  "analysis": {
    "isSuspicious": true,
    "riskLevel": "High",
    "reason": "AI Interview Tool detected. Matches known threat signature. Repeated detection pattern suggests persistent use.",
    "recommendation": "IMMEDIATE ACTION REQUIRED: Notify interviewer immediately. Consider pausing/ending interview.",
    "emailSubject": "🚨 HIGH RISK Interview Fraud Alert - Parakeetai Detected",
    "emailBody": "FRAUD ALERT - HIGH RISK\n\nProcess: parakeet-ai.exe\nWindow Title: Parakeet AI Interview Helper\nDetection Count: 3 times\n...",
    "threatType": "AI Interview Tool",
    "threatScore": 85.0,
    "detectionCount": 3,
    "timestamp": "2024-01-15T14:30:00Z",
    "indicators": ["Matches AI Interview Tool signature"]
  },
  "message": "Fraud analysis complete. Risk level: High"
}
```

### Example 2: Legitimate Application

**Request**:
```bash
curl -X POST http://localhost:8088/responses \
  -H "Content-Type: application/json" \
  -d '{
    "messages": [
      {
        "role": "user",
        "content": "processName=notepad.exe, windowTitle=Document - Notepad, detectionCount=1"
      }
    ]
  }'
```

**Response**:
```json
{
  "status": "success",
  "analysis": {
    "isSuspicious": false,
    "riskLevel": "Low",
    "reason": "Process and window title do not match known fraud patterns.",
    "recommendation": "Log incident for reference. No immediate action required.",
    "emailSubject": "ℹ️ Interview Activity Log - Notepad",
    "emailBody": "Interview Activity Notification\n\nProcess: notepad.exe\n...",
    "threatType": null,
    "threatScore": 0.0,
    "detectionCount": 1,
    "timestamp": "2024-01-15T14:31:00Z",
    "indicators": []
  },
  "message": "Fraud analysis complete. Risk level: Low"
}
```

---

## Rate Limiting & Performance

- **Max requests/sec**: Unlimited (local server)
- **Average response time**: < 100ms
- **Timeout**: 30 seconds per request

---

## Integration Examples

### Python Integration

```python
import requests
import json

def analyze_overlay(process_name, window_title, detection_count):
    response = requests.post(
        "http://localhost:8088/responses",
        json={
            "messages": [{
                "role": "user",
                "content": f"processName={process_name}, windowTitle={window_title}, detectionCount={detection_count}"
            }]
        }
    )
    return response.json()

# Usage
result = analyze_overlay("parakeet-ai.exe", "Parakeet AI", 3)
if result["analysis"]["isSuspicious"]:
    print(f"Alert: {result['analysis']['recommendation']}")
```

### C# Integration

```csharp
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

var client = new HttpClient();
var request = new
{
    messages = new[]
    {
        new
        {
            role = "user",
            content = "processName=obs.exe, windowTitle=OBS Studio, detectionCount=2"
        }
    }
};

var json = JsonConvert.SerializeObject(request);
var content = new StringContent(json, Encoding.UTF8, "application/json");
var response = await client.PostAsync("http://localhost:8088/responses", content);
var result = await response.Content.ReadAsStringAsync();
```

---

## Versioning

**API Version**: 1.0.0  
**Release Date**: 2024  
**Status**: Stable
