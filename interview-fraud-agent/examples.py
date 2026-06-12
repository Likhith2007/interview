#!/usr/bin/env python
"""
Example script showing how to use the Interview Fraud Detection Agent
This demonstrates the core analysis capabilities without needing a server
"""

import sys
import os
from datetime import datetime

sys.path.insert(0, os.path.dirname(__file__))

from src.tools import get_overlay_analysis_tool


def print_section(title):
    """Print a formatted section title"""
    print(f"\n{'='*60}")
    print(f"  {title}")
    print(f"{'='*60}")


def print_result(result):
    """Pretty print analysis result"""
    print(f"\n📊 FRAUD ANALYSIS RESULT")
    print(f"  Suspicious: {result['isSuspicious']}")
    print(f"  Risk Level: {result['riskLevel']}")
    print(f"  Threat Type: {result['threatType']}")
    print(f"  Threat Score: {result['threatScore']}/100")
    print(f"  Detection Count: {result['detectionCount']}")
    
    print(f"\n📋 ANALYSIS DETAILS")
    print(f"  Reason: {result['reason']}")
    print(f"  Recommendation: {result['recommendation']}")
    
    if result['indicators']:
        print(f"\n⚠️  SUSPICIOUS INDICATORS")
        for i, indicator in enumerate(result['indicators'], 1):
            print(f"  {i}. {indicator}")
    
    print(f"\n✉️  EMAIL ALERT")
    print(f"  Subject: {result['emailSubject']}")
    print(f"\n  Body:")
    for line in result['emailBody'].split('\n'):
        print(f"  {line}")


def example_1_parakeet_ai():
    """Example 1: ParakeetAI Detection"""
    print_section("Example 1: ParakeetAI Detection (High Risk)")
    
    tool = get_overlay_analysis_tool()
    result = tool.analyze_overlay_risk(
        process_name="parakeet-ai.exe",
        window_title="Parakeet AI Interview Helper",
        detection_count=3,
    )
    
    print_result(result)


def example_2_obs_screen_recorder():
    """Example 2: OBS Screen Recorder"""
    print_section("Example 2: OBS Studio Detection (Medium Risk)")
    
    tool = get_overlay_analysis_tool()
    result = tool.analyze_overlay_risk(
        process_name="obs.exe",
        window_title="OBS Studio - Stream / Recording",
        detection_count=2,
    )
    
    print_result(result)


def example_3_protonvpn():
    """Example 3: ProtonVPN - Low Risk but Suspicious"""
    print_section("Example 3: ProtonVPN Detected (Low-Medium Risk)")
    
    tool = get_overlay_analysis_tool()
    result = tool.analyze_overlay_risk(
        process_name="protonvpn.exe",
        window_title="ProtonVPN",
        detection_count=1,
    )
    
    print_result(result)


def example_4_legitimate_app():
    """Example 4: Legitimate Application"""
    print_section("Example 4: Legitimate Application (No Risk)")
    
    tool = get_overlay_analysis_tool()
    result = tool.analyze_overlay_risk(
        process_name="notepad.exe",
        window_title="Document.txt - Notepad",
        detection_count=1,
    )
    
    print_result(result)


def example_5_persistent_threat():
    """Example 5: Persistent AI Tool Usage (Critical)"""
    print_section("Example 5: Persistent ChatGPT Usage (Critical Risk)")
    
    tool = get_overlay_analysis_tool()
    result = tool.analyze_overlay_risk(
        process_name="chatgpt.exe",
        window_title="ChatGPT - Ask anything",
        detection_count=12,
    )
    
    print_result(result)


def example_6_browser_devtools():
    """Example 6: Browser DevTools Detection"""
    print_section("Example 6: Chrome DevTools Detected (Medium Risk)")
    
    tool = get_overlay_analysis_tool()
    result = tool.analyze_overlay_risk(
        process_name="chrome.exe",
        window_title="Chrome DevTools - Network Inspector",
        detection_count=4,
    )
    
    print_result(result)


def example_7_remote_access():
    """Example 7: Remote Access Tool"""
    print_section("Example 7: TeamViewer Remote Access (High Risk)")
    
    tool = get_overlay_analysis_tool()
    result = tool.analyze_overlay_risk(
        process_name="teamviewer.exe",
        window_title="TeamViewer",
        detection_count=1,
    )
    
    print_result(result)


if __name__ == "__main__":
    print("\n🔍 INTERVIEW FRAUD DETECTION AGENT - USAGE EXAMPLES\n")
    
    example_1_parakeet_ai()
    example_2_obs_screen_recorder()
    example_3_protonvpn()
    example_4_legitimate_app()
    example_5_persistent_threat()
    example_6_browser_devtools()
    example_7_remote_access()
    
    print_section("Summary")
    print("""
The Interview Fraud Detection Agent analyzes overlay detection data to identify
suspicious applications that indicate interview fraud.

KEY FEATURES:
✓ AI-powered threat analysis and pattern matching
✓ Risk classification (Low/Medium/High)
✓ Detailed reasoning for suspicious activity
✓ Formatted email alerts for immediate notification
✓ Persistent usage detection and escalation
✓ Behavioral analysis for obfuscation attempts

SUPPORTED THREAT CATEGORIES:
• AI Interview Tools (ParakeetAI, ChatGPT, Claude, etc.)
• Screen Recorders (OBS, Camtasia, ScreenFlow)
• Proctoring Cheats (ProctorU bypass tools)
• Remote Access Tools (TeamViewer, AnyDesk)
• VPN/Proxy Tools (ProtonVPN, Windscribe)
• Browser DevTools and Network Analyzers

For more information, see README.md
    """)
