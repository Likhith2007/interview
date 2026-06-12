"""
Tool definitions for the Interview Fraud Detection Agent.
Combines rule-based threat detection with LLM-powered reasoning
via GitHub Models (Microsoft Azure AI Inference endpoint).
"""

import json
from datetime import datetime, timezone
from typing import Any
from src.models import OverlayDetectionInput, FraudAnalysisOutput
from src.fraud_analyzer import create_fraud_analyzer


class OverlayAnalysisTool:
    """
    Two-stage fraud analysis tool:
    1. Pattern matching against 500+ threat signatures
    2. GPT-4o-mini reasoning via GitHub Models for intelligent analysis
    """

    def __init__(self):
        self.analyzer = create_fraud_analyzer()

    def analyze_overlay_risk(
        self,
        process_name: str,
        window_title: str,
        detection_count: int,
        timestamp: str = None,
    ) -> dict[str, Any]:
        """
        Analyze an overlay detection for interview fraud risk.

        Stage 1: Rule-based pattern matching (instant, deterministic)
        Stage 2: LLM reasoning via GitHub Models (intelligent, context-aware)

        Args:
            process_name: Executable process name (e.g., 'parakeet-ai.exe')
            window_title: Window title text
            detection_count: Number of times detected
            timestamp: ISO 8601 timestamp of detection

        Returns:
            Comprehensive fraud analysis with LLM-powered reasoning
        """
        if timestamp is None:
            timestamp = datetime.now(timezone.utc).isoformat()

        # ── Stage 1: Rule-based pattern matching ──────────────────────────────
        risk_assessment = self.analyzer.analyze(process_name, window_title, detection_count)

        # Determine suspicion threshold and risk level
        is_suspicious = risk_assessment.threat_score >= 60
        if risk_assessment.threat_score >= 80:
            risk_level = "High"
        elif risk_assessment.threat_score >= 60:
            risk_level = "Medium"
        else:
            risk_level = "Low"

        # ── Stage 2: LLM reasoning (GitHub Models / GPT-4o-mini) ─────────────
        llm_result = self.analyzer.generate_llm_reasoning(
            process_name=process_name,
            window_title=window_title,
            detection_count=detection_count,
            risk_assessment=risk_assessment,
        )

        reason = llm_result["reason"]
        recommendation = llm_result["recommendation"]
        llm_powered = llm_result.get("llm_powered", False)

        # ── Generate email alert ───────────────────────────────────────────────
        email_subject, email_body = self._generate_email_alert(
            is_suspicious, risk_level, process_name, window_title,
            detection_count, timestamp, reason, recommendation,
        )

        return {
            "isSuspicious": is_suspicious,
            "riskLevel": risk_level,
            "reason": reason,
            "recommendation": recommendation,
            "emailSubject": email_subject,
            "emailBody": email_body,
            "threatType": risk_assessment.threat_type,
            "threatScore": round(risk_assessment.threat_score, 1),
            "detectionCount": detection_count,
            "timestamp": timestamp,
            "indicators": risk_assessment.behavior_indicators,
            "llmPowered": llm_powered,
            "analysisEngine": "GitHub Models (GPT-4o-mini) + Rule-based Pattern Matching",
        }

    def _generate_email_alert(
        self,
        is_suspicious: bool,
        risk_level: str,
        process_name: str,
        window_title: str,
        detection_count: int,
        timestamp: str,
        reason: str,
        recommendation: str,
    ) -> tuple[str, str]:
        """Generate formatted email alert."""
        try:
            from datetime import datetime
            dt = datetime.fromisoformat(timestamp.replace("Z", "+00:00"))
            time_str = dt.strftime("%Y-%m-%d %H:%M:%S UTC")
        except Exception:
            time_str = timestamp

        if is_suspicious:
            emoji = "🚨" if risk_level == "High" else "⚠️"
            subject = f"{emoji} {risk_level.upper()} Interview Fraud Alert - {process_name.split('.')[0].title()} Detected"
            body = f"""INTERVIEW FRAUD ALERT - {risk_level.upper()} RISK
AI-Powered Analysis by Interview Fraud Detection Agent

[{time_str}] Suspicious Overlay Detected

Process: {process_name}
Window Title: {window_title}
Detection Count: {detection_count} time(s)
Risk Level: {risk_level}

REASON (AI Analysis):
{reason}

ACTION REQUIRED:
{recommendation}

TECHNICAL DETAILS:
- Notify the interviewer immediately
- Document the incident with timestamp and process details
- Consider pausing or ending the interview
- Flag candidate profile for review
- Maintain records for compliance/audit

---
Interview Fraud Detection Agent
Powered by GitHub Models (GPT-4o-mini) + Rule-based Pattern Matching
Automated Alert - {time_str}
"""
        else:
            subject = f"ℹ️ Interview Activity Log - {process_name.split('.')[0].title()}"
            body = f"""Interview Activity Notification

[{time_str}] Activity Detected

Process: {process_name}
Window Title: {window_title}
Detection Count: {detection_count} time(s)

STATUS: Not flagged as fraudulent
REASON: {reason}

This process is not recognized as fraudulent based on current threat signatures.
However, this activity has been logged for reference.

---
Interview Fraud Detection Agent
Automated Log - {time_str}
"""
        return subject, body


def get_overlay_analysis_tool() -> OverlayAnalysisTool:
    """Factory function to get the analysis tool."""
    return OverlayAnalysisTool()
