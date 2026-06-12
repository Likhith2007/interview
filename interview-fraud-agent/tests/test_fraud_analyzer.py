"""Tests for fraud analyzer"""

import sys
import os
from datetime import datetime

# Add src to path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

from src.fraud_analyzer import create_fraud_analyzer
from src.tools import get_overlay_analysis_tool


def test_parakeet_ai_detection():
    """Test detection of ParakeetAI"""
    tool = get_overlay_analysis_tool()
    
    result = tool.analyze_overlay_risk(
        process_name="parakeet-ai.exe",
        window_title="Parakeet AI Interview Helper",
        detection_count=3,
    )
    
    assert result["isSuspicious"] == True
    assert result["riskLevel"] == "High"
    assert "Parakeet" in result["reason"] or "AI" in result["reason"]
    print("✓ ParakeetAI detection test passed")
    print(f"  Risk Level: {result['riskLevel']}")
    print(f"  Reason: {result['reason']}")


def test_screen_recorder_detection():
    """Test detection of screen recorders"""
    tool = get_overlay_analysis_tool()
    
    result = tool.analyze_overlay_risk(
        process_name="obs.exe",
        window_title="OBS Studio - Stream / Recording",
        detection_count=2,
    )
    
    assert result["isSuspicious"] == True
    assert result["riskLevel"] in ["Medium", "High"]
    print("✓ Screen recorder detection test passed")
    print(f"  Risk Level: {result['riskLevel']}")


def test_legitimate_process():
    """Test legitimate process (should not be flagged)"""
    tool = get_overlay_analysis_tool()
    
    result = tool.analyze_overlay_risk(
        process_name="notepad.exe",
        window_title="Untitled - Notepad",
        detection_count=1,
    )
    
    assert result["isSuspicious"] == False
    assert result["riskLevel"] == "Low"
    print("✓ Legitimate process test passed")


def test_frequency_escalation():
    """Test that high detection count increases risk"""
    tool = get_overlay_analysis_tool()
    
    # Single detection
    result1 = tool.analyze_overlay_risk(
        process_name="protonvpn.exe",
        window_title="ProtonVPN",
        detection_count=1,
    )
    
    # Multiple detections
    result2 = tool.analyze_overlay_risk(
        process_name="protonvpn.exe",
        window_title="ProtonVPN",
        detection_count=15,
    )
    
    assert result2["threatScore"] > result1["threatScore"]
    print("✓ Frequency escalation test passed")
    print(f"  Single detection score: {result1['threatScore']}")
    print(f"  Multiple detection score: {result2['threatScore']}")


def test_email_alert_generation():
    """Test email alert formatting"""
    tool = get_overlay_analysis_tool()
    
    result = tool.analyze_overlay_risk(
        process_name="chatgpt.exe",
        window_title="ChatGPT - Ask anything",
        detection_count=2,
    )
    
    assert "emailSubject" in result
    assert "emailBody" in result
    assert len(result["emailSubject"]) > 0
    assert len(result["emailBody"]) > 0
    
    print("✓ Email alert generation test passed")
    print(f"  Subject: {result['emailSubject']}")


if __name__ == "__main__":
    print("Running Fraud Analyzer Tests...\n")
    
    test_parakeet_ai_detection()
    print()
    
    test_screen_recorder_detection()
    print()
    
    test_legitimate_process()
    print()
    
    test_frequency_escalation()
    print()
    
    test_email_alert_generation()
    print()
    
    print("All tests passed! ✓")
