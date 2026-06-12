# Quick Start Guide

## 🚀 Get Started in 5 Minutes

### Prerequisites
- Python 3.11+
- pip (Python package manager)
- Azure Foundry project (optional - for cloud deployment)

### Step 1: Set Up Environment

```bash
# Navigate to project directory
cd interview-fraud-agent

# Create virtual environment
python -m venv venv

# Activate virtual environment
# On Windows:
venv\Scripts\activate
# On macOS/Linux:
source venv/bin/activate
```

### Step 2: Install Dependencies

```bash
pip install -r requirements.txt
```

### Step 3: Try the Examples

Run the example script to see the agent in action:

```bash
python examples.py
```

This will demonstrate fraud detection with 7 different scenarios:
1. ParakeetAI (High Risk)
2. OBS Studio Screen Recorder (Medium Risk)
3. ProtonVPN (Low Risk)
4. Legitimate Application (No Risk)
5. Persistent ChatGPT Usage (Critical)
6. Browser DevTools (Medium Risk)
7. TeamViewer Remote Access (High Risk)

### Step 4: Run Unit Tests

```bash
python tests/test_fraud_analyzer.py
```

## 💻 Local Server Development

### Option A: Direct Python Execution

```bash
python src/app.py
```

Server starts on: `http://localhost:8088`

### Option B: Using Uvicorn

```bash
pip install uvicorn flask
uvicorn src.app:asgi_app --host 0.0.0.0 --port 8088 --reload
```

### Option C: VS Code Debugging

1. Open VS Code in the project directory
2. Press `F5` to start debugging (uses `.vscode/launch.json` configuration)
3. Server will start with debugger attached

## 📤 Making Requests to the Agent

### Method 1: Using cURL

```bash
curl -X POST http://localhost:8088/responses \
  -H "Content-Type: application/json" \
  -d '{
    "messages": [
      {
        "role": "user",
        "content": "processName=parakeet-ai.exe, windowTitle=Parakeet AI Interview Helper, detectionCount=3, timestamp=2024-01-15T14:30:00Z"
      }
    ]
  }'
```

### Method 2: Using Python

```python
import requests
import json

response = requests.post(
    "http://localhost:8088/responses",
    json={
        "messages": [
            {
                "role": "user",
                "content": "processName=obs.exe, windowTitle=OBS Studio, detectionCount=2"
            }
        ]
    }
)

print(json.dumps(response.json(), indent=2))
```

### Method 3: Using PowerShell

```powershell
$body = @{
    messages = @(
        @{
            role = "user"
            content = "processName=chatgpt.exe, windowTitle=ChatGPT, detectionCount=5"
        }
    )
} | ConvertTo-Json

Invoke-WebRequest -Uri "http://localhost:8088/responses" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body
```

## 📋 Input Format

The agent accepts overlay detection data in multiple formats:

### Format 1: Key-Value Pairs
```
processName=parakeet-ai.exe, windowTitle=Parakeet AI Interview Helper, detectionCount=3, timestamp=2024-01-15T14:30:00Z
```

### Format 2: JSON
```json
{
  "processName": "parakeet-ai.exe",
  "windowTitle": "Parakeet AI Interview Helper",
  "detectionCount": 3,
  "timestamp": "2024-01-15T14:30:00Z"
}
```

## 📊 Understanding the Output

The agent returns a comprehensive analysis:

```json
{
  "status": "success",
  "analysis": {
    "isSuspicious": true,
    "riskLevel": "High",
    "reason": "ParakeetAI is a known AI-powered interview assistant...",
    "recommendation": "Immediate alert to interviewer. Consider ending interview...",
    "emailSubject": "🚨 HIGH RISK Interview Fraud Alert - Parakeetai Detected",
    "emailBody": "FRAUD ALERT - HIGH RISK\n\n[14:30:00] ParakeetAI...",
    "threatType": "AI Interview Tool",
    "threatScore": 85.0,
    "detectionCount": 3,
    "timestamp": "2024-01-15T14:30:00Z",
    "indicators": ["Matches AI Interview Tool signature", ...]
  }
}
```

## 🔧 Configuration

### Environment Variables

Create a `.env` file from `.env.template`:

```bash
cp .env.template .env
```

Edit `.env` with your Azure Foundry details:

```
AZURE_SUBSCRIPTION_ID=<your-subscription-id>
AZURE_RESOURCE_GROUP=<your-resource-group>
AZURE_PROJECT_NAME=<your-project-name>
AZURE_MODEL_DEPLOYMENT_NAME=gpt-4
AZURE_INFERENCE_ENDPOINT=<your-endpoint>
AZURE_INFERENCE_CREDENTIAL=<your-credential>
```

## 🐳 Docker Deployment

### Build Docker Image

```bash
docker build -t interview-fraud-agent:latest .
```

### Run in Docker

```bash
docker run -p 8088:8088 \
  -e AZURE_SUBSCRIPTION_ID=$SUBSCRIPTION_ID \
  -e AZURE_RESOURCE_GROUP=$RESOURCE_GROUP \
  interview-fraud-agent:latest
```

## ☁️ Deploy to Azure Foundry

### Prerequisites
- Azure CLI installed (`az`)
- Logged into Azure (`az login`)
- Azure Foundry project created

### Deployment Steps

```bash
# Set up Azure Developer CLI
azd init -t .

# Configure deployment
azd config set defaults.subscription $SUBSCRIPTION_ID

# Deploy
azd up
```

## 📚 Project Structure

```
interview-fraud-agent/
├── src/
│   ├── __init__.py              # Package initialization
│   ├── app.py                   # Main server & agent
│   ├── models.py                # Pydantic data models
│   ├── tools.py                 # Analysis tools
│   └── fraud_analyzer.py        # Core fraud detection logic
├── tests/
│   ├── test_fraud_analyzer.py   # Unit tests
│   └── __init__.py
├── .vscode/
│   ├── launch.json              # Debug configuration
│   └── tasks.json               # Build tasks
├── examples.py                  # Usage examples
├── agent.yaml                   # Agent configuration
├── requirements.txt             # Python dependencies
├── .env.template                # Environment variables template
└── README.md                    # Full documentation
```

## 🐛 Troubleshooting

### Port Already in Use

If port 8088 is already in use:

```bash
# Find process using port 8088
lsof -i :8088  # macOS/Linux
netstat -ano | findstr :8088  # Windows

# Kill process or use different port
python src/app.py --port 9000
```

### Import Errors

Make sure you've activated the virtual environment:

```bash
# Windows
venv\Scripts\activate

# macOS/Linux
source venv/bin/activate
```

### Azure Authentication Issues

Ensure you're logged into Azure:

```bash
az login
```

Or set Azure credentials explicitly in `.env` file.

## 📖 Next Steps

1. **Review Examples**: Run `python examples.py` to see all threat types
2. **Integrate with Application**: Use the agent in your interview platform
3. **Monitor Production**: Set up logging and alerting
4. **Customize Threats**: Extend `src/fraud_analyzer.py` with your threat database
5. **Deploy to Cloud**: Follow Azure Foundry deployment guide

## 🤝 Contributing

Contributions are welcome! Please:

1. Create a new branch
2. Add tests for new features
3. Ensure all tests pass
4. Submit a pull request

## 📞 Support

For issues or questions:
- Check the [README.md](README.md) for detailed documentation
- Review code comments in `src/` directory
- Run tests with `python tests/test_fraud_analyzer.py`
