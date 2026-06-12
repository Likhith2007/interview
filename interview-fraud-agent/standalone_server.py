#!/usr/bin/env python
"""
Standalone Interview Fraud Detection Server
Runs the fraud analysis engine as a local HTTP server — no Azure required.

Usage:
    python standalone_server.py            # default port 8088
    python standalone_server.py --port 9000

Endpoints:
    POST /responses   — Analyze overlay detection data
    GET  /health      — Health check
    GET  /            — Usage info
"""

import sys
import os
import json
import asyncio
import logging
import argparse
from datetime import datetime, timezone

# Make src importable from this file's directory
sys.path.insert(0, os.path.dirname(__file__))

from src.tools import get_overlay_analysis_tool

# ── Logging ──────────────────────────────────────────────────────────────────
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s  %(levelname)-8s  %(message)s",
    datefmt="%H:%M:%S",
)
logger = logging.getLogger("fraud-server")

# ── Shared tool instance (created once at startup) ────────────────────────────
_tool = None


def get_tool():
    global _tool
    if _tool is None:
        _tool = get_overlay_analysis_tool()
        logger.info("FraudAnalyzer tool initialised")
    return _tool


# ── Input parsing ─────────────────────────────────────────────────────────────

def parse_overlay_input(message: str) -> dict:
    """
    Parse overlay data from a user message.
    Supports:
      • JSON string: {"processName": "...", "windowTitle": "...", ...}
      • Key=value pairs: processName=foo.exe, windowTitle=Bar, detectionCount=3
    """
    # Try JSON first
    try:
        data = json.loads(message)
        if isinstance(data, dict):
            return data
    except (json.JSONDecodeError, ValueError):
        pass

    # Fall back to key=value parsing
    result = {}
    for pair in message.split(","):
        if "=" in pair:
            key, _, value = pair.partition("=")
            key = key.strip().lower()
            value = value.strip().strip("'\"")
            result[key] = value

    # Normalise keys to camelCase
    key_map = {
        "process_name":   "processName",
        "processname":    "processName",
        "window_title":   "windowTitle",
        "windowtitle":    "windowTitle",
        "detection_count":"detectionCount",
        "detectioncount": "detectionCount",
    }
    for snake, camel in key_map.items():
        if snake in result:
            result[camel] = result.pop(snake)

    return result


# ── Core request handler ──────────────────────────────────────────────────────

async def handle_response_protocol(messages: list) -> dict:
    """Process a list of chat messages and return fraud analysis."""

    # Extract the latest user message
    user_message = next(
        (m.get("content", "") for m in reversed(messages) if m.get("role") == "user"),
        None,
    )

    if not user_message:
        return {
            "error": "No user message found",
            "message": "Please provide overlay detection data.",
        }

    try:
        data = parse_overlay_input(user_message)

        process_name    = data.get("processName") or data.get("process_name")
        window_title    = data.get("windowTitle")  or data.get("window_title")
        detection_count = data.get("detectionCount") or data.get("detection_count")
        timestamp       = data.get("timestamp") or datetime.now(timezone.utc).isoformat()

        if not all([process_name, window_title, detection_count]):
            return {
                "error": "Missing required fields",
                "message": "Please provide: processName, windowTitle, detectionCount",
                "example": (
                    "processName=parakeet-ai.exe, "
                    "windowTitle=Parakeet AI Interview Helper, "
                    "detectionCount=3, "
                    "timestamp=2024-01-15T14:30:00Z"
                ),
            }

        detection_count = int(detection_count)

        logger.info(
            "Analysing overlay: %s | %s | count=%d",
            process_name, window_title, detection_count,
        )

        result = get_tool().analyze_overlay_risk(
            process_name=process_name,
            window_title=window_title,
            detection_count=detection_count,
            timestamp=timestamp,
        )

        logger.info(
            "Result: suspicious=%s  risk=%s  score=%.1f",
            result["isSuspicious"], result["riskLevel"], result["threatScore"],
        )

        return {
            "status": "success",
            "analysis": result,
            "message": f"Fraud analysis complete. Risk level: {result['riskLevel']}",
        }

    except ValueError as exc:
        return {"error": "Invalid input", "message": str(exc)}
    except Exception as exc:
        logger.exception("Unexpected error")
        return {"error": "Internal error", "message": str(exc)}


# ── ASGI application (uvicorn-compatible) ─────────────────────────────────────

async def app(scope, receive, send):
    if scope["type"] != "http":
        return

    path   = scope.get("path", "/")
    method = scope.get("method", "GET")

    async def respond(status: int, body: dict | str):
        if isinstance(body, dict):
            raw = json.dumps(body, ensure_ascii=False).encode()
        else:
            raw = body.encode() if isinstance(body, str) else body
        await send({
            "type": "http.response.start",
            "status": status,
            "headers": [
                [b"content-type", b"application/json"],
                [b"access-control-allow-origin", b"*"],
            ],
        })
        await send({"type": "http.response.body", "body": raw})

    # ── POST /responses ───────────────────────────────────────────────────────
    if path == "/responses" and method == "POST":
        body = b""
        while True:
            msg = await receive()
            body += msg.get("body", b"")
            if not msg.get("more_body"):
                break
        try:
            data     = json.loads(body.decode())
            messages = data.get("messages", [])
        except json.JSONDecodeError:
            await respond(400, {"error": "Invalid JSON body"})
            return

        result = await handle_response_protocol(messages)
        await respond(200, result)

    # ── GET /health ───────────────────────────────────────────────────────────
    elif path == "/health" and method == "GET":
        await respond(200, {
            "status": "healthy",
            "agent": "interview-fraud-detector-standalone",
            "timestamp": datetime.now(timezone.utc).isoformat(),
        })

    # ── GET / ─────────────────────────────────────────────────────────────────
    elif path == "/" and method == "GET":
        await respond(200, {
            "name": "Interview Fraud Detection Server (Standalone)",
            "version": "1.0.0",
            "azure_required": False,
            "endpoints": {
                "POST /responses": "Analyse overlay detection data",
                "GET  /health":    "Health check",
            },
            "example_request": {
                "messages": [{
                    "role": "user",
                    "content": (
                        "processName=parakeet-ai.exe, "
                        "windowTitle=Parakeet AI Interview Helper, "
                        "detectionCount=3"
                    ),
                }],
            },
        })

    # ── 404 ───────────────────────────────────────────────────────────────────
    else:
        await respond(404, {"error": "Not found"})


# ── Entry point ───────────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser(description="Standalone Interview Fraud Detection Server")
    parser.add_argument("--host", default="0.0.0.0", help="Bind host (default: 0.0.0.0)")
    parser.add_argument("--port", type=int, default=8088, help="Bind port (default: 8088)")
    args = parser.parse_args()

    try:
        import uvicorn
    except ImportError:
        print("uvicorn is not installed. Run: pip install uvicorn")
        sys.exit(1)

    # Warm up the tool before accepting requests
    get_tool()

    logger.info("=" * 55)
    logger.info("  Interview Fraud Detection Server  (standalone)")
    logger.info("  No Azure credentials required")
    logger.info("=" * 55)
    logger.info("  Listening : http://%s:%d", args.host, args.port)
    logger.info("  Endpoint  : POST http://localhost:%d/responses", args.port)
    logger.info("  Health    : GET  http://localhost:%d/health", args.port)
    logger.info("=" * 55)

    uvicorn.run(app, host=args.host, port=args.port, log_level="warning")


if __name__ == "__main__":
    main()
