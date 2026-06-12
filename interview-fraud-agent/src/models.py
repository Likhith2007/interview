"""
Data models for Interview Fraud Detection Agent
Defines input/output schemas for overlay analysis and alert generation
"""

from datetime import datetime
from typing import Optional
from pydantic import BaseModel, Field


class OverlayDetectionInput(BaseModel):
    """Input model for overlay detection data"""
    
    process_name: str = Field(..., description="Executable process name (e.g., 'parakeet-ai.exe')")
    window_title: str = Field(..., description="Window title text")
    timestamp: datetime = Field(..., description="ISO 8601 datetime of detection")
    detection_count: int = Field(..., ge=1, description="Number of times this overlay was detected")


class FraudAnalysisOutput(BaseModel):
    """Output model for fraud analysis results"""
    
    is_suspicious: bool = Field(..., description="Whether the overlay indicates suspicious activity")
    risk_level: str = Field(..., description="Risk level: Low, Medium, or High")
    reason: str = Field(..., description="Explanation of why it's suspicious (or not)")
    recommendation: str = Field(..., description="Recommended action to take")
    email_subject: str = Field(..., description="Formatted email subject line")
    email_body: str = Field(..., description="Formatted email body for alert")


class RiskAssessment(BaseModel):
    """Internal model for risk assessment details"""
    
    threat_type: Optional[str] = Field(None, description="Category of threat (AI Tool, Screen Recorder, etc.)")
    threat_score: float = Field(..., ge=0, le=100, description="Risk score 0-100")
    known_threat: bool = Field(..., description="Whether this is a known threat in database")
    behavior_indicators: list[str] = Field(default_factory=list, description="List of suspicious behaviors detected")
    frequency_risk: Optional[str] = Field(None, description="Risk based on detection frequency")
