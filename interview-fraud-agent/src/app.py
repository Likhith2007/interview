"""
Interview Fraud Detection Agent - Main Application
Implements Microsoft Agent Framework with overlay detection analysis
"""

import os
import json
import logging
from typing import Any

from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import AgentEvent, MessageDeltaEvent
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

from src.models import OverlayDetectionInput, FraudAnalysisOutput
from src.tools import get_overlay_analysis_tool


# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Load environment variables
load_dotenv()


class InterviewFraudDetectionAgent:
    """Interview Fraud Detection Agent using Microsoft Agent Framework"""
    
    def __init__(self):
        """Initialize the agent with Azure Foundry client"""
        
        # Initialize Azure client
        self.project_client = AIProjectClient.from_config(
            credential=DefaultAzureCredential(),
        )
        
        # Get the analysis tool
        self.analysis_tool = get_overlay_analysis_tool()
        
        # Agent configuration
        self.agent_name = "interview-fraud-detector"
        self.model_name = os.getenv("AZURE_MODEL_DEPLOYMENT_NAME", "gpt-4")
        
        logger.info(f"Agent initialized: {self.agent_name}")
        logger.info(f"Model: {self.model_name}")
    
    def process_overlay_detection(
        self,
        process_name: str,
        window_title: str,
        timestamp: str,
        detection_count: int,
    ) -> dict[str, Any]:
        """
        Process overlay detection and perform fraud analysis
        
        Args:
            process_name: Executable process name
            window_title: Window title text
            timestamp: ISO 8601 datetime
            detection_count: Number of detections
            
        Returns:
            Fraud analysis result
        """
        logger.info(f"Processing overlay: {process_name} - {window_title} (count: {detection_count})")
        
        try:
            # Validate input
            overlay_input = OverlayDetectionInput(
                process_name=process_name,
                window_title=window_title,
                timestamp=timestamp,
                detection_count=detection_count,
            )
            
            # Perform fraud analysis
            result = self.analysis_tool.analyze_overlay_risk(
                process_name=overlay_input.process_name,
                window_title=overlay_input.window_title,
                detection_count=overlay_input.detection_count,
                timestamp=overlay_input.timestamp.isoformat(),
            )
            
            logger.info(f"Analysis complete: suspicious={result['isSuspicious']}, risk={result['riskLevel']}")
            
            return result
            
        except Exception as e:
            logger.error(f"Error processing overlay: {str(e)}", exc_info=True)
            raise


def create_agent() -> InterviewFraudDetectionAgent:
    """Factory function to create the agent"""
    return InterviewFraudDetectionAgent()


# Global agent instance
_agent = None


def get_agent() -> InterviewFraudDetectionAgent:
    """Get or create the global agent instance"""
    global _agent
    if _agent is None:
        _agent = create_agent()
    return _agent


def parse_overlay_input(message: str) -> dict[str, Any]:
    """
    Parse overlay detection input from user message
    Supports multiple formats:
    - JSON: {"processName": "...", "windowTitle": "...", ...}
    - Key=value: processName=xxx, windowTitle=yyy, ...
    - Natural language with key=value pairs
    """
    
    # Try JSON parsing first
    try:
        data = json.loads(message)
        if isinstance(data, dict):
            return data
    except (json.JSONDecodeError, ValueError):
        pass
    
    # Parse key=value format
    result = {}
    pairs = message.split(",")
    
    for pair in pairs:
        if "=" in pair:
            key, value = pair.split("=", 1)
            key = key.strip().lower().replace("process name", "process_name").replace("window title", "window_title")
            value = value.strip().strip("'\"")
            result[key] = value
    
    # Map snake_case to camelCase for compatibility
    if "process_name" in result:
        result["processName"] = result.pop("process_name")
    if "window_title" in result:
        result["windowTitle"] = result.pop("window_title")
    if "detection_count" in result:
        result["detectionCount"] = result.pop("detection_count")
    
    return result


async def handle_response_protocol(messages: list[dict]) -> dict[str, Any]:
    """
    Handle /responses protocol for conversation-based interaction
    
    Args:
        messages: List of message dictionaries with role and content
        
    Returns:
        Response dictionary with analysis results
    """
    
    agent = get_agent()
    
    # Extract user message
    user_message = None
    for message in messages:
        if message.get("role") == "user":
            user_message = message.get("content", "")
            break
    
    if not user_message:
        return {
            "error": "No user message found",
            "message": "Please provide overlay detection data.",
        }
    
    try:
        # Parse overlay input from message
        input_data = parse_overlay_input(user_message)
        
        # Extract required fields
        process_name = input_data.get("processName") or input_data.get("process_name")
        window_title = input_data.get("windowTitle") or input_data.get("window_title")
        detection_count = input_data.get("detectionCount") or input_data.get("detection_count")
        timestamp = input_data.get("timestamp")
        
        if not all([process_name, window_title, detection_count]):
            return {
                "error": "Missing required fields",
                "message": "Please provide: processName, windowTitle, detectionCount",
                "example": "processName=parakeet-ai.exe, windowTitle=Parakeet AI Interview Helper, detectionCount=3, timestamp=2024-01-15T14:30:00Z",
            }
        
        # Convert detection_count to int
        detection_count = int(detection_count)
        
        # Process overlay detection
        result = agent.process_overlay_detection(
            process_name=process_name,
            window_title=window_title,
            timestamp=timestamp or None,
            detection_count=detection_count,
        )
        
        return {
            "status": "success",
            "analysis": result,
            "message": f"Fraud analysis complete. Risk level: {result['riskLevel']}",
        }
        
    except ValueError as e:
        return {
            "error": "Invalid input",
            "message": f"Error parsing input: {str(e)}",
        }
    except Exception as e:
        logger.error(f"Error in response handler: {str(e)}", exc_info=True)
        return {
            "error": "Internal error",
            "message": f"Error processing request: {str(e)}",
        }


# Flask/FastAPI app setup (for local testing)
def create_app():
    """Create Flask application for local testing"""
    try:
        from flask import Flask, request, jsonify
    except ImportError:
        logger.warning("Flask not installed. Using minimal WSGI server.")
        return None
    
    app = Flask(__name__)
    
    @app.route("/responses", methods=["POST"])
    def responses_handler():
        """Handle /responses protocol requests"""
        data = request.get_json()
        messages = data.get("messages", [])
        
        # Process asynchronously
        import asyncio
        result = asyncio.run(handle_response_protocol(messages))
        
        return jsonify(result)
    
    @app.route("/health", methods=["GET"])
    def health():
        """Health check endpoint"""
        return jsonify({"status": "healthy", "agent": "interview-fraud-detector"})
    
    @app.route("/", methods=["GET"])
    def index():
        """Root endpoint with documentation"""
        return jsonify({
            "name": "Interview Fraud Detection Agent",
            "version": "1.0.0",
            "endpoints": {
                "POST /responses": "Analyze overlay detection data",
                "GET /health": "Health check",
            },
            "example_request": {
                "messages": [{
                    "role": "user",
                    "content": "processName=parakeet-ai.exe, windowTitle=Parakeet AI Interview Helper, detectionCount=3, timestamp=2024-01-15T14:30:00Z"
                }]
            },
        })
    
    return app


# Uvicorn app (async support)
async def asgi_app(scope, receive, send):
    """ASGI application for uvicorn"""
    if scope["type"] != "http":
        return
    
    path = scope["path"]
    method = scope["method"]
    
    if path == "/responses" and method == "POST":
        # Read request body
        body = b""
        while True:
            message = await receive()
            body += message.get("body", b"")
            if not message.get("more_body"):
                break
        
        # Parse JSON
        data = json.loads(body.decode())
        messages = data.get("messages", [])
        
        # Process request
        result = await handle_response_protocol(messages)
        
        # Send response
        response_body = json.dumps(result).encode()
        await send({
            "type": "http.response.start",
            "status": 200,
            "headers": [[b"content-type", b"application/json"]],
        })
        await send({
            "type": "http.response.body",
            "body": response_body,
        })
    
    elif path == "/health" and method == "GET":
        response_body = json.dumps({"status": "healthy"}).encode()
        await send({
            "type": "http.response.start",
            "status": 200,
            "headers": [[b"content-type", b"application/json"]],
        })
        await send({
            "type": "http.response.body",
            "body": response_body,
        })
    
    elif path == "/" and method == "GET":
        response_body = json.dumps({
            "name": "Interview Fraud Detection Agent",
            "version": "1.0.0",
        }).encode()
        await send({
            "type": "http.response.start",
            "status": 200,
            "headers": [[b"content-type", b"application/json"]],
        })
        await send({
            "type": "http.response.body",
            "body": response_body,
        })
    
    else:
        await send({
            "type": "http.response.start",
            "status": 404,
            "headers": [[b"content-type", b"application/json"]],
        })
        await send({
            "type": "http.response.body",
            "body": b'{"error": "Not found"}',
        })


if __name__ == "__main__":
    # Local development server
    import uvicorn
    
    logger.info("Starting Interview Fraud Detection Agent...")
    logger.info("Server: http://localhost:8088")
    logger.info("Endpoint: POST http://localhost:8088/responses")
    
    # Try Flask first, fall back to uvicorn
    app = create_app()
    if app:
        app.run(host="0.0.0.0", port=8088, debug=False)
    else:
        uvicorn.run(asgi_app, host="0.0.0.0", port=8088)
