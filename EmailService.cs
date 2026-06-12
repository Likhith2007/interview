using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace OverlayDetector
{
    public static class EmailService
    {
        /// <summary>
        /// Sends an intelligent fraud alert email using Foundry AI analysis.
        /// </summary>
        public static async Task SendFraudAlertAsync(AppSettings settings, FraudAnalysisService.FraudAnalysisResult analysis)
        {
            if (string.IsNullOrWhiteSpace(settings.SenderEmail) || 
                string.IsNullOrWhiteSpace(settings.SenderAppPassword) || 
                string.IsNullOrWhiteSpace(settings.InterviewerEmail))
            {
                throw new InvalidOperationException("Email settings are not fully configured.");
            }

            await SendEmailInternalAsync(
                settings.SenderEmail,
                settings.SenderAppPassword,
                settings.InterviewerEmail,
                analysis.EmailSubject,
                analysis.EmailBody
            );
        }

        /// <summary>
        /// Sends a basic overlay alert email (fallback if fraud analysis is not available).
        /// </summary>
        public static async Task SendOverlayAlertAsync(AppSettings settings, OverlayWindow overlay)
        {
            if (string.IsNullOrWhiteSpace(settings.SenderEmail) || 
                string.IsNullOrWhiteSpace(settings.SenderAppPassword) || 
                string.IsNullOrWhiteSpace(settings.InterviewerEmail))
            {
                throw new InvalidOperationException("Email settings are not fully configured.");
            }

            string subject = $"[ALERT] Unauthorized Overlay Detected - Interview Monitor";
            string body = $@"Hello,

An unauthorized overlay window was detected on the candidate's screen during the interview.

Detection Details:
---------------------------------------------
Timestamp:    {overlay.DetectedAt:yyyy-MM-dd HH:mm:ss}
Process Name: {overlay.ProcessName}
Window Title: {overlay.WindowTitle}
Window Class: {overlay.WindowClass}
Handle:       {overlay.Handle}
---------------------------------------------

Please verify if this application is authorized or if the candidate is violating interview guidelines.

Regards,
Overlay Detector System";

            await SendEmailInternalAsync(
                settings.SenderEmail,
                settings.SenderAppPassword,
                settings.InterviewerEmail,
                subject,
                body
            );
        }

        public static async Task SendTestEmailAsync(AppSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.SenderEmail) || 
                string.IsNullOrWhiteSpace(settings.SenderAppPassword) || 
                string.IsNullOrWhiteSpace(settings.InterviewerEmail))
            {
                throw new InvalidOperationException("Email settings are not fully configured.");
            }

            string subject = $"[TEST] Overlay Detector Email Test";
            string body = $@"Hello,

This is a test email sent from the Overlay Detector application to verify that your email notifications are correctly configured.

If you received this message, the SMTP connection and credentials are working properly.

Regards,
Overlay Detector System";

            await SendEmailInternalAsync(
                settings.SenderEmail,
                settings.SenderAppPassword,
                settings.InterviewerEmail,
                subject,
                body
            );
        }

        private static async Task SendEmailInternalAsync(string senderEmail, string senderPassword, string recipientEmail, string subject, string body)
        {
            // Send SMTP mail in a background task to keep it non-blocking
            await Task.Run(() =>
            {
                using (var smtpClient = new SmtpClient("smtp.gmail.com"))
                {
                    smtpClient.Port = 587;
                    smtpClient.Credentials = new NetworkCredential(senderEmail, senderPassword);
                    smtpClient.EnableSsl = true;

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(senderEmail);
                        mailMessage.Subject = subject;
                        mailMessage.Body = body;
                        mailMessage.IsBodyHtml = false;
                        mailMessage.To.Add(recipientEmail);

                        smtpClient.Send(mailMessage);
                    }
                }
            });
        }
    }
}
