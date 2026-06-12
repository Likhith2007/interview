using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OverlayDetector
{
    /// <summary>
    /// Service for analyzing overlay detections using the Foundry AI fraud detection agent.
    /// </summary>
    public class FraudAnalysisService
    {
        private readonly string _agentEndpoint;
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public class FraudAnalysisResult
        {
            public bool IsSuspicious { get; set; }
            public string RiskLevel { get; set; } = "Low"; // Low, Medium, High
            public string Reason { get; set; } = "";
            public string Recommendation { get; set; } = "";
            public string EmailSubject { get; set; } = "";
            public string EmailBody { get; set; } = "";
            public string ThreatType { get; set; } = "";
            public double ThreatScore { get; set; } = 0;
        }

        public FraudAnalysisService(string agentEndpoint = "http://localhost:8088")
        {
            _agentEndpoint = agentEndpoint;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Analyzes an overlay detection using the Foundry fraud detection agent.
        /// </summary>
        public async Task<FraudAnalysisResult> AnalyzeOverlayAsync(OverlayWindow overlay, int detectionCount = 1)
        {
            try
            {
                // Prepare the request to the agent
                var requestPayload = new
                {
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = $"processName={overlay.ProcessName}, windowTitle={overlay.WindowTitle}, timestamp={overlay.DetectedAt:O}, detectionCount={detectionCount}"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Call the Foundry agent
                var response = await _httpClient.PostAsync($"{_agentEndpoint}/responses", content);

                if (!response.IsSuccessStatusCode)
                {
                    // If agent is unavailable, return a basic analysis
                    return CreateBasicAnalysis(overlay, detectionCount);
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var analysisResult = ParseAgentResponse(responseContent);

                return analysisResult;
            }
            catch (HttpRequestException)
            {
                // If agent is unreachable, fallback to basic analysis
                return CreateBasicAnalysis(overlay, detectionCount);
            }
            catch (Exception ex)
            {
                // Log error and return basic analysis
                System.Diagnostics.Debug.WriteLine($"Error analyzing overlay with Foundry agent: {ex.Message}");
                return CreateBasicAnalysis(overlay, detectionCount);
            }
        }

        /// <summary>
        /// Parses the standalone server response.
        /// Expected format: { "status": "success", "analysis": { isSuspicious, riskLevel, ... } }
        /// Falls back to the legacy { "messages": [...] } format if needed.
        /// </summary>
        private static FraudAnalysisResult ParseAgentResponse(string responseJson)
        {
            try
            {
                using var document = JsonDocument.Parse(responseJson);
                var root = document.RootElement;

                var result = new FraudAnalysisResult();

                // ── Standalone server format: { "status": "success", "analysis": { ... } } ──
                if (root.TryGetProperty("analysis", out var analysis))
                {
                    result.IsSuspicious   = GetBoolProperty  (analysis, "isSuspicious",   true);
                    result.RiskLevel      = GetStringProperty (analysis, "riskLevel",       "Medium");
                    result.Reason         = GetStringProperty (analysis, "reason",          "Suspicious overlay detected");
                    result.Recommendation = GetStringProperty (analysis, "recommendation",  "Review the overlay");
                    result.EmailSubject   = GetStringProperty (analysis, "emailSubject",    "🚨 Suspicious Overlay Detected - Interview Integrity Alert");
                    result.EmailBody      = GetStringProperty (analysis, "emailBody",       "A suspicious overlay has been detected.");
                    result.ThreatType     = GetStringProperty (analysis, "threatType",      "Unknown");
                    result.ThreatScore    = GetDoubleProperty (analysis, "threatScore",     50);
                    return result;
                }

                // ── Legacy Azure Foundry format: { "messages": [{ "content": "<json>" }] } ──
                if (root.TryGetProperty("messages", out var messages) && messages.GetArrayLength() > 0)
                {
                    var message = messages[0];
                    if (message.TryGetProperty("content", out var content))
                    {
                        var contentStr = content.GetString() ?? "";
                        try
                        {
                            var analysisJson = JsonDocument.Parse(contentStr);
                            var inner = analysisJson.RootElement;

                            result.IsSuspicious   = GetBoolProperty  (inner, "isSuspicious",   true);
                            result.RiskLevel      = GetStringProperty (inner, "riskLevel",       "Medium");
                            result.Reason         = GetStringProperty (inner, "reason",          "Suspicious overlay detected");
                            result.Recommendation = GetStringProperty (inner, "recommendation",  "Review the overlay");
                            result.EmailSubject   = GetStringProperty (inner, "emailSubject",    "🚨 Suspicious Overlay Detected - Interview Integrity Alert");
                            result.EmailBody      = GetStringProperty (inner, "emailBody",       "A suspicious overlay has been detected.");
                            result.ThreatType     = GetStringProperty (inner, "threatType",      "Unknown");
                            result.ThreatScore    = GetDoubleProperty (inner, "threatScore",     50);
                        }
                        catch
                        {
                            result.IsSuspicious = true;
                            result.RiskLevel    = "Medium";
                            result.Reason       = contentStr;
                            result.EmailSubject = "🚨 Suspicious Overlay Detected - Interview Integrity Alert";
                            result.EmailBody    = contentStr;
                        }
                    }
                    return result;
                }

                // Unrecognised response shape — return safe default
                return new FraudAnalysisResult
                {
                    IsSuspicious  = true,
                    RiskLevel     = "Medium",
                    Reason        = "Overlay detected during interview",
                    Recommendation = "Review the overlay details",
                    EmailSubject  = "🚨 Suspicious Overlay Detected - Interview Integrity Alert",
                    EmailBody     = "An overlay window was detected during the interview. Please review the details."
                };
            }
            catch
            {
                return new FraudAnalysisResult
                {
                    IsSuspicious  = true,
                    RiskLevel     = "Medium",
                    Reason        = "Overlay detected during interview",
                    Recommendation = "Review the overlay details",
                    EmailSubject  = "🚨 Suspicious Overlay Detected - Interview Integrity Alert",
                    EmailBody     = "An overlay window was detected during the interview. Please review the details."
                };
            }
        }

        /// <summary>
        /// Creates a basic analysis when the Foundry agent is unavailable.
        /// </summary>
        private static FraudAnalysisResult CreateBasicAnalysis(OverlayWindow overlay, int detectionCount)
        {
            // Check for known suspicious patterns locally
            string lowerProcessName = overlay.ProcessName.ToLower();
            string lowerWindowTitle = overlay.WindowTitle.ToLower();

            bool isSuspicious = IsKnownSuspiciousApp(lowerProcessName, lowerWindowTitle);
            string threatType = DetermineThreatType(lowerProcessName, lowerWindowTitle);
            double threatScore = CalculateThreatScore(threatType, detectionCount);
            string riskLevel = threatScore >= 75 ? "High" : threatScore >= 50 ? "Medium" : "Low";

            string reason = isSuspicious 
                ? $"Detected {threatType}: {overlay.ProcessName} - {overlay.WindowTitle}"
                : $"Unknown overlay detected: {overlay.ProcessName}";

            string recommendation = riskLevel == "High"
                ? "Immediate action required - Terminate interview and investigate"
                : riskLevel == "Medium"
                ? "Review and verify this application is authorized"
                : "Monitor for further occurrences";

            string subject = riskLevel == "High"
                ? $"🚨 HIGH RISK - Interview Fraud Detected: {threatType}"
                : $"⚠️ Interview Integrity Alert: Suspicious overlay detected";

            string body = $@"Interview Fraud Detection Alert
================================

Risk Level: {riskLevel}
Threat Type: {threatType}
Threat Score: {threatScore:F1}/100
Detection Count: {detectionCount}x

Detected Application:
  Process: {overlay.ProcessName}
  Window: {overlay.WindowTitle}
  Time: {overlay.DetectedAt:yyyy-MM-dd HH:mm:ss}

Analysis:
{reason}

Recommendation:
{recommendation}

If this is a legitimate application, you can whitelist it in the settings.";

            return new FraudAnalysisResult
            {
                IsSuspicious = isSuspicious,
                RiskLevel = riskLevel,
                Reason = reason,
                Recommendation = recommendation,
                EmailSubject = subject,
                EmailBody = body,
                ThreatType = threatType,
                ThreatScore = threatScore
            };
        }

        /// <summary>
        /// Checks if the app is in the known suspicious apps list.
        /// </summary>
        private static bool IsKnownSuspiciousApp(string processName, string windowTitle)
        {
            // AI Interview Tools
            if (processName.Contains("parakeet") || windowTitle.Contains("parakeet")) return true;
            if (processName.Contains("chatgpt") || windowTitle.Contains("chatgpt")) return true;
            if (processName.Contains("claude") || windowTitle.Contains("claude")) return true;
            if (processName.Contains("copilot") || windowTitle.Contains("copilot")) return true;
            if (processName.Contains("gemini") || windowTitle.Contains("gemini")) return true;

            // Screen Recorders
            if (processName.Contains("obs") || windowTitle.Contains("obs studio")) return true;
            if (processName.Contains("camtasia") || windowTitle.Contains("camtasia")) return true;
            if (processName.Contains("screenflow")) return true;

            // Remote Access
            if (processName.Contains("teamviewer")) return true;
            if (processName.Contains("anydesk")) return true;
            if (processName.Contains("chrome remote")) return true;

            // VPN
            if (processName.Contains("protonvpn") || processName.Contains("windscribe")) return true;

            return false;
        }

        /// <summary>
        /// Determines the threat type based on app signatures.
        /// </summary>
        private static string DetermineThreatType(string processName, string windowTitle)
        {
            if (processName.Contains("parakeet") || windowTitle.Contains("parakeet")) return "AI Interview Tool (ParakeetAI)";
            if (processName.Contains("chatgpt")) return "AI Tool (ChatGPT)";
            if (processName.Contains("claude")) return "AI Tool (Claude)";
            if (processName.Contains("copilot")) return "AI Tool (Copilot)";
            if (processName.Contains("obs")) return "Screen Recorder (OBS)";
            if (processName.Contains("camtasia")) return "Screen Recorder (Camtasia)";
            if (processName.Contains("teamviewer")) return "Remote Access (TeamViewer)";
            if (processName.Contains("anydesk")) return "Remote Access (AnyDesk)";

            return "Suspicious Application";
        }

        /// <summary>
        /// Calculates threat score based on threat type and frequency.
        /// </summary>
        private static double CalculateThreatScore(string threatType, int detectionCount)
        {
            double baseScore = threatType switch
            {
                _ when threatType.Contains("AI") => 85.0,
                _ when threatType.Contains("Remote Access") => 75.0,
                _ when threatType.Contains("Screen Recorder") => 70.0,
                _ => 50.0
            };

            // Increase score based on detection count (escalation)
            double escalation = Math.Min(detectionCount * 5, 15); // Cap at +15

            return Math.Min(baseScore + escalation, 100);
        }

        private static bool GetBoolProperty(JsonElement element, string propertyName, bool defaultValue)
        {
            return element.TryGetProperty(propertyName, out var value) && value.GetBoolean() 
                ? true 
                : defaultValue;
        }

        private static string GetStringProperty(JsonElement element, string propertyName, string defaultValue)
        {
            return element.TryGetProperty(propertyName, out var value) 
                ? value.GetString() ?? defaultValue 
                : defaultValue;
        }

        private static double GetDoubleProperty(JsonElement element, string propertyName, double defaultValue)
        {
            return element.TryGetProperty(propertyName, out var value) && value.TryGetDouble(out var doubleValue)
                ? doubleValue
                : defaultValue;
        }
    }
}
