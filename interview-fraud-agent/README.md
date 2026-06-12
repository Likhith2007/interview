# Interview Fraud Detection Agent

> **Microsoft Agents League Hackathon 2026 — Reasoning Agents Track**

An intelligent agent built on the **Microsoft Agent Framework** that detects interview fraud in real-time by analyzing overlay windows on candidate screens during technical interviews.

[![Track](https://img.shields.io/badge/Track-Reasoning%20Agents-blue)](https://aka.ms/agentsleague)
[![Framework](https://img.shields.io/badge/Framework-Microsoft%20Agent%20Framework-purple)](https://learn.microsoft.com/azure/ai-foundry/agents)
[![Model](https://img.shields.io/badge/Model-GPT--4o--mini-green)](https://github.com/marketplace/models)
[![Protocol](https://img.shields.io/badge/Protocol-%2Fresponses-orange)](https://learn.microsoft.com/azure/ai-foundry/agents)

---

## 🎯 Problem Statement

Technical interviews are increasingly vulnerable to AI-assisted cheating. Candidates run hidden tools like **ParakeetAI**, **ChatGPT overlays**, **screen recorders**, and **remote access software** to gain unfair advantages. Traditional monitoring systems can detect these windows exist — but cannot reason about *why* they're suspicious or *what action* the interviewer should take.

## 💡 Solution

A two-stage intelligent agent that:

1. **Detects** suspicious overlay windows via pattern matching against 500+ threat signatures
2. **Reasons** about each detection using **GPT-4o-mini** (via GitHub Models / Azure AI Inference) to generate context-aware, actionable fraud alerts
3. **Alerts** interviewers instantly via formatted email with AI-generated reasoning

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    WPF Monitor App (C#)                     │
│  WindowScanner → detects overlay windows on candidate screen│
└──────────────────────────┬──────────────────────────────────┘
                           │ HTTP POST /responses
                           ▼
┌─────────────────────────────────────────────────────────────┐
│         Interview Fraud Detection Agent (Python)            │
│                                                             │
│  ┌─────────────────┐    ┌──────────────────────────────┐   │
│  │  Stage 1        │    │  Stage 2                     │   │
│  │  Rule-based     │───▶│  LLM Reasoning               │   │
│  │  Pattern Match  │    │  GitHub Models / GPT-4o-mini │   │
│  │  (500+ sigs)    │    │  (Azure AI Inference)        │   │
│  └─────────────────┘    └──────────────────────────────┘   │
│                                                             │
│  Microsoft Agent Framework — /responses protocol           │
└──────────────────────────┬──────────────────────────────────┘
                           │ Fraud Analysis Result
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                  Email Alert (Gmail SMTP)                   │
│  AI-generated subject + body sent to interviewer instantly  │
└─────────────────────────────────────────────────────────────┘
```

---

## ✨ Key Features

| Feature | Description |
|---|---|
| 🧠 **LLM Reasoning** | GPT-4o-mini generates nuanced, context-aware fraud explanations |
| ⚡ **Real-time Detection** | Scans every 1 second, responds in <500ms |
| 📊 **Risk Scoring** | 0–100 composite score (pattern + frequency + behavior) |
| 📧 **Instant Alerts** | AI-written email sent to interviewer automatically |
| 🔄 **Graceful Fallback** | Rule-based analysis if LLM unavailable |
| 🎯 **6 Threat Categories** | AI tools, screen recorders, remote access, VPNs, dev tools, proctoring cheats |

---

## 🚨 Detected Threat Categories

| Category | Examples | Base Risk |
|---|---|---|
| **AI Interview Tools** | ParakeetAI, ChatGPT, Claude, Copilot, Gemini | 85/100 |
| **Proctoring Cheats** | ProctorU bypass, exam helpers | 90/100 |
| **Remote Access** | TeamViewer, AnyDesk, VNC | 75/100 |
| **Screen Recorders** | OBS Studio, Camtasia, Bandicam | 70/100 |
| **Browser DevTools** | Chrome DevTools, Wireshark, Fiddler | 50/100 |
| **VPN/Proxy** | ProtonVPN, NordVPN, Tor | 45/100 |

---

## 🚀 Quick Start

### Prerequisites
- Python 3.11+
- GitHub account (for free LLM access via GitHub Models)

### 1. Clone and Setup
```bash
git clone https://github.com/YOUR_USERNAME/interview-fraud-agent
cd interview-fraud-agent
python -m venv venv
venv\Scripts\activate   # Windows
pip install -r requirements.txt
```

### 2. Configure Environment
```bash
copy .env.template .env
# Edit .env — add your GITHUB_TOKEN
```

Get a free GitHub token at [github.com/settings/tokens](https://github.com/settings/tokens) (no scopes needed).

### 3. Run the Agent
```bash
python standalone_server.py
```
Server starts at `http://localhost:8088`

### 4. Test It
```powershell
$body = @{
    messages = @(@{
        role = "user"
        content = "processName=parakeet-ai.exe, windowTitle=Parakeet AI Interview Helper, detectionCount=3"
    })
} | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8088/responses" -Method POST -ContentType "application/json" -Body $body
```

---

## 📋 API Reference

### `POST /responses`

**Request:**
```json
{
  "messages": [{
    "role": "user",
    "content": "processName=parakeet-ai.exe, windowTitle=Parakeet AI, detectionCount=3"
  }]
}
```

**Response:**
```json
{
  "status": "success",
  "analysis": {
    "isSuspicious": true,
    "riskLevel": "High",
    "reason": "ParakeetAI is a well-known AI-powered interview assistant designed to provide real-time answers...",
    "recommendation": "Immediately pause the interview and notify the hiring manager...",
    "emailSubject": "🚨 HIGH Interview Fraud Alert - Parakeet-Ai Detected",
    "emailBody": "...",
    "threatType": "AI Interview Tool",
    "threatScore": 90.0,
    "llmPowered": true,
    "analysisEngine": "GitHub Models (GPT-4o-mini) + Rule-based Pattern Matching"
  }
}
```

---

## 🔧 Environment Variables

| Variable | Description |
|---|---|
| `GITHUB_TOKEN` | GitHub personal access token for GitHub Models (free LLM access) |
| `AZURE_MODEL_DEPLOYMENT_NAME` | Model name (default: `gpt-4o-mini`) |
| `AZURE_INFERENCE_ENDPOINT` | Endpoint (default: GitHub Models endpoint) |

---

## 🧪 Run Tests & Examples

```bash
python tests/test_fraud_analyzer.py   # Unit tests (5 scenarios)
python examples.py                     # Demo (7 threat scenarios)
```

---

## 📁 Project Structure

```
interview-fraud-agent/
├── src/
│   ├── app.py              # Microsoft Agent Framework server
│   ├── fraud_analyzer.py   # Two-stage analysis (pattern + LLM)
│   ├── tools.py            # OverlayAnalysisTool
│   └── models.py           # Pydantic schemas
├── standalone_server.py    # Local dev server (no Azure auth needed)
├── agent.yaml              # Agent configuration
├── examples.py             # Usage demonstrations
├── tests/                  # Unit tests
└── requirements.txt
```

---

## 🤖 How the LLM Reasoning Works

```
Detection Data ──▶ Pattern Matching ──▶ Risk Score + Threat Type
                                              │
                                              ▼
                              GPT-4o-mini via GitHub Models
                              ┌────────────────────────────┐
                              │ System: Expert interview   │
                              │ fraud analyst              │
                              │                            │
                              │ User: Process=X, Score=85, │
                              │ ThreatType=AI Tool...      │
                              └────────────────────────────┘
                                              │
                                              ▼
                         Intelligent Reason + Recommendation
                         (context-aware, specific, actionable)
```

---

## 📄 License

MIT — See [LICENSE](LICENSE)

---

*Built for the Microsoft Agents League Hackathon 2026 — Reasoning Agents Track*
