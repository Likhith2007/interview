"""
Fraud analyzer with AI-powered threat detection and risk assessment.
Uses GitHub Models (Microsoft-hosted) via OpenAI SDK for intelligent reasoning.
"""

import os
import re
import logging
from datetime import datetime
from typing import Optional
from dotenv import load_dotenv
from src.models import RiskAssessment

load_dotenv()
logger = logging.getLogger(__name__)


def _get_llm_client():
    """Create OpenAI client pointing to GitHub Models (Microsoft-hosted endpoint)."""
    try:
        from openai import OpenAI
        token = os.environ.get("GITHUB_TOKEN")
        if not token:
            return None
        return OpenAI(
            base_url="https://models.inference.ai.azure.com",
            api_key=token,
        )
    except ImportError:
        return None


class FraudAnalyzer:
    """
    Core fraud detection with two-stage analysis:
    1. Fast rule-based pattern matching (threat database)
    2. Deep LLM reasoning via GitHub Models / Azure AI Inference
    """

    # Known suspicious applications and patterns
    THREAT_DATABASE = {
        "ai_tools": {
            "patterns": [
                r"parakeet|paraket",
                r"interviewai",
                r"chatgpt|chat-gpt",
                r"claude|anthropic",
                r"copilot|co-pilot",
                r"gemini",
                r"grok",
                r"perplexity",
                r"ai.*assistant|assistant.*ai",
                r"openai",
            ],
            "keywords": ["interview", "helper", "assistant", "ai", "chatbot"],
            "risk_score": 85,
            "threat_type": "AI Interview Tool",
        },
        "screen_recorders": {
            "patterns": [
                r"obs|obs\.exe|obs-studio",
                r"camtasia",
                r"screenflow",
                r"bandicam",
                r"fraps",
                r"dxtory",
                r"nvidia.*shadow|shadowplay",
                r"screen.*record|record.*screen",
                r"streamlabs",
                r"xsplit",
                r"recordmydesktop",
            ],
            "keywords": ["record", "capture", "stream", "broadcast", "screen"],
            "risk_score": 70,
            "threat_type": "Screen Recorder",
        },
        "proctoring_cheats": {
            "patterns": [
                r"proctor|procteru|proctor-u",
                r"blackboard.*cheat|cheat.*blackboard",
                r"canvas.*bypass|bypass.*canvas",
                r"exam.*helper|helper.*exam",
                r"test.*cheat|cheat.*test",
                r"answer.*generator|generator.*answer",
            ],
            "keywords": ["proctor", "cheat", "exam", "bypass", "answer"],
            "risk_score": 90,
            "threat_type": "Proctoring Cheat",
        },
        "remote_access": {
            "patterns": [
                r"teamviewer",
                r"anydesk",
                r"chrome.*remote|remote.*desktop",
                r"microsoft.*remote|mstsc",
                r"vnc|remote-desktop",
                r"splashtop",
                r"zoho.*assist",
            ],
            "keywords": ["remote", "control", "access", "desktop", "teamviewer"],
            "risk_score": 75,
            "threat_type": "Remote Access Tool",
        },
        "vpn_proxy": {
            "patterns": [
                r"protonvpn|proton.*vpn",
                r"windscribe",
                r"expressvpn",
                r"nordvpn",
                r"private.*internet|pia.*vpn",
                r"tunnelbear",
                r"proxy|proxy.*server",
                r"tor.*browser|torproject",
            ],
            "keywords": ["vpn", "proxy", "tunnel", "anonymous"],
            "risk_score": 45,
            "threat_type": "VPN/Proxy Tool",
        },
        "dev_tools": {
            "patterns": [
                r"chrome.*dev|devtools",
                r"firefox.*inspector",
                r"edge.*dev",
                r"network.*analyzer|analyzer.*network",
                r"wireshark",
                r"fiddler",
                r"charles.*proxy",
            ],
            "keywords": ["developer", "inspector", "console", "network", "debugger"],
            "risk_score": 50,
            "threat_type": "Browser DevTools/Analyzer",
        },
    }

    def __init__(self):
        self._llm_client = _get_llm_client()
        self._model = os.environ.get("AZURE_MODEL_DEPLOYMENT_NAME", "gpt-4o-mini")
        if self._llm_client:
            logger.info("LLM reasoning enabled: GitHub Models / %s", self._model)
        else:
            logger.warning("LLM reasoning disabled (no GITHUB_TOKEN). Using rule-based analysis only.")

    def analyze(self, process_name: str, window_title: str, detection_count: int) -> RiskAssessment:
        """
        Two-stage analysis:
        Stage 1: Pattern matching for fast threat classification.
        Stage 2: LLM reasoning for intelligent, context-aware analysis.
        """
        # Stage 1: Rule-based pattern matching
        threat_info = self._match_threat_database(process_name, window_title)
        frequency_risk = self._assess_frequency(detection_count)
        base_score = threat_info.get("risk_score", 0) if threat_info else 0
        frequency_bonus = self._get_frequency_bonus(detection_count)
        final_score = min(100, base_score + frequency_bonus)
        indicators = self._identify_behaviors(process_name, window_title)
        if threat_info:
            indicators.append(f"Matches {threat_info['threat_type']} signature")

        return RiskAssessment(
            threat_type=threat_info.get("threat_type") if threat_info else None,
            threat_score=final_score,
            known_threat=threat_info is not None,
            behavior_indicators=indicators,
            frequency_risk=frequency_risk,
        )

    def generate_llm_reasoning(
        self,
        process_name: str,
        window_title: str,
        detection_count: int,
        risk_assessment: RiskAssessment,
    ) -> dict:
        """
        Stage 2: Call GitHub Models (GPT-4o-mini via Microsoft Azure AI Inference)
        to generate intelligent, context-aware fraud reasoning.

        Falls back gracefully to rule-based reasoning if LLM is unavailable.
        """
        if not self._llm_client:
            return self._fallback_reasoning(risk_assessment, detection_count)

        try:
            threat_context = (
                f"Pattern match: {risk_assessment.threat_type}"
                if risk_assessment.known_threat
                else "No known threat pattern matched"
            )
            behavioral = (
                ", ".join(risk_assessment.behavior_indicators)
                if risk_assessment.behavior_indicators
                else "none detected"
            )

            prompt = f"""You are an expert interview integrity analyst. Analyze this overlay detection:

Process Name: {process_name}
Window Title: {window_title}
Detection Count: {detection_count} time(s)
Risk Score: {risk_assessment.threat_score:.0f}/100
{threat_context}
Behavioral indicators: {behavioral}
Frequency pattern: {risk_assessment.frequency_risk}

Provide a JSON response with exactly these fields:
{{
  "reason": "2-3 sentence explanation of why this is/isn't suspicious, mentioning the specific tool/behavior",
  "recommendation": "Specific actionable recommendation for the interviewer (1-2 sentences)"
}}

Be professional, specific, and decisive. If risk score >= 80, be urgent."""

            response = self._llm_client.chat.completions.create(
                model=self._model,
                messages=[
                    {
                        "role": "system",
                        "content": (
                            "You are an expert interview fraud detection analyst for a proctoring system. "
                            "You analyze overlay windows detected on candidate screens during technical interviews. "
                            "Always respond with valid JSON only."
                        ),
                    },
                    {"role": "user", "content": prompt},
                ],
                max_tokens=300,
                temperature=0.2,
                response_format={"type": "json_object"},
            )

            import json
            content = response.choices[0].message.content
            result = json.loads(content)
            logger.info("LLM reasoning generated successfully")
            return {
                "reason": result.get("reason", "Suspicious activity detected"),
                "recommendation": result.get("recommendation", "Review the overlay"),
                "llm_powered": True,
            }

        except Exception as ex:
            logger.warning("LLM reasoning failed (%s), using fallback", str(ex))
            return self._fallback_reasoning(risk_assessment, detection_count)

    def _fallback_reasoning(self, assessment: RiskAssessment, detection_count: int) -> dict:
        """Rule-based fallback when LLM is unavailable."""
        reasons = []
        if assessment.threat_type:
            reasons.append(f"{assessment.threat_type} detected")
        if assessment.known_threat:
            reasons.append("Matches known threat signature in database")
        if assessment.behavior_indicators:
            reasons.append(f"Suspicious behaviors: {', '.join(assessment.behavior_indicators[:2])}")
        if assessment.frequency_risk and "High" in assessment.frequency_risk:
            reasons.append("Repeated detection pattern suggests persistent use")

        reason = ". ".join(reasons) if reasons else "Suspicious activity detected during interview"

        if assessment.threat_score >= 80:
            rec = "IMMEDIATE ACTION REQUIRED: Notify interviewer immediately. Consider pausing/ending interview and flagging candidate account."
        elif assessment.threat_score >= 60:
            rec = "Alert interviewer to suspicious activity. Monitor candidate closely for additional violations."
        else:
            rec = "Log incident for reference. No immediate action required, but remain vigilant."

        return {"reason": reason, "recommendation": rec, "llm_powered": False}

    def _match_threat_database(self, process_name: str, window_title: str) -> Optional[dict]:
        combined_text = f"{process_name} {window_title}".lower()
        matches = []
        for category, threat_info in self.THREAT_DATABASE.items():
            for pattern in threat_info["patterns"]:
                if re.search(pattern, combined_text, re.IGNORECASE):
                    matches.append(threat_info.copy())
                    break
        return max(matches, key=lambda x: x["risk_score"]) if matches else None

    def _assess_frequency(self, detection_count: int) -> str:
        if detection_count == 1:
            return "Low - Single detection"
        elif detection_count <= 3:
            return "Medium - Multiple detections (2-3)"
        elif detection_count <= 10:
            return "High - Repeated detections (4-10)"
        else:
            return "Critical - Persistent activity (10+)"

    def _get_frequency_bonus(self, detection_count: int) -> float:
        if detection_count == 1:
            return 0
        elif detection_count <= 3:
            return 5
        elif detection_count <= 10:
            return 15
        else:
            return 25

    def _identify_behaviors(self, process_name: str, window_title: str) -> list[str]:
        behaviors = []
        combined = f"{process_name} {window_title}".lower()
        if any(x in combined for x in ["hidden", "minimized", "background", "system"]):
            behaviors.append("Attempts to hide or run in background")
        if combined.count("\\") > 1 or "temp" in combined or "appdata" in combined:
            behaviors.append("Running from suspicious directory")
        if re.search(r"[A-Z]{5,}|process\d+|service\d+", process_name):
            behaviors.append("Obfuscated process name")
        if any(x in combined for x in ["system", "windows", "microsoft", "chrome"]):
            if not any(y in combined for y in ["windows\\system", "chrome browser"]):
                behaviors.append("Possible process spoofing")
        return behaviors


def create_fraud_analyzer() -> FraudAnalyzer:
    return FraudAnalyzer()
