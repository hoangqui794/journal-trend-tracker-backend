using System;
using System.Net;
using System.Net.Mail;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IdentityService.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private static readonly HttpClient _httpClient = new HttpClient();

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "";
                var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "";
                var senderName = _configuration["EmailSettings:SenderName"] ?? "Journal Trend Tracker";
                var password = _configuration["EmailSettings:Password"] ?? "";

                if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("Email sender credentials are not configured. Email will not be sent, but process will proceed. Logged email details: To={ToEmail}, Subject={Subject}", toEmail, subject);
                    return;
                }

                // If using SendGrid, bypass Render's SMTP port blocking by sending via SendGrid HTTP Web API instead of SMTP
                if (smtpServer.Contains("sendgrid", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("SendGrid detected. Sending email to {ToEmail} using HTTP Web API (bypassing SMTP ports)...", toEmail);
                    await SendViaSendGridWebApiAsync(toEmail, subject, htmlMessage, senderEmail, senderName, password);
                    return;
                }

                // Otherwise fallback to standard SMTP
                _logger.LogInformation("SMTP detected. Sending email to {ToEmail} using SMTP client...", toEmail);
                await SendViaSmtpAsync(toEmail, subject, htmlMessage, smtpServer, senderEmail, senderName, password);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
                throw new Exception($"Failed to send email: {ex.Message}", ex);
            }
        }

        private async Task SendViaSendGridWebApiAsync(string toEmail, string subject, string htmlMessage, string senderEmail, string senderName, string apiKey)
        {
            var requestUri = "https://api.sendgrid.com/v3/mail/send";
            
            var payload = new
            {
                personalizations = new[]
                {
                    new
                    {
                        to = new[]
                        {
                            new { email = toEmail }
                        }
                    }
                },
                from = new
                {
                    email = senderEmail,
                    name = senderName
                },
                subject = subject,
                content = new[]
                {
                    new
                    {
                        type = "text/html",
                        value = htmlMessage
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using (var request = new HttpRequestMessage(HttpMethod.Post, requestUri))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    throw new Exception($"SendGrid Web API returned status code {response.StatusCode}: {responseBody}");
                }
                
                _logger.LogInformation("Email sent successfully via SendGrid Web API to {ToEmail}", toEmail);
            }
        }

        private async Task SendViaSmtpAsync(string toEmail, string subject, string htmlMessage, string smtpServer, string senderEmail, string senderName, string password)
        {
            var portStr = _configuration["EmailSettings:Port"] ?? "587";
            var username = _configuration["EmailSettings:Username"] ?? "";
            if (string.IsNullOrEmpty(username))
            {
                username = senderEmail;
            }
            var enableSslStr = _configuration["EmailSettings:EnableSsl"] ?? "true";

            int port = int.Parse(portStr);
            bool enableSsl = bool.Parse(enableSslStr);

            using (var smtpClient = new SmtpClient(smtpServer))
            {
                smtpClient.Port = port;
                smtpClient.Credentials = new NetworkCredential(username, password);
                smtpClient.EnableSsl = enableSsl;
                smtpClient.Timeout = 15000; // 15 seconds

                using (var mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress(senderEmail, senderName);
                    mailMessage.Subject = subject;
                    mailMessage.Body = htmlMessage;
                    mailMessage.IsBodyHtml = true;
                    mailMessage.To.Add(toEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                    _logger.LogInformation("Email sent successfully via SMTP to {ToEmail}", toEmail);
                }
            }
        }
    }
}
