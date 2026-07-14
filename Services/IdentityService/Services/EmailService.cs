using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IdentityService.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var portStr = _configuration["EmailSettings:Port"] ?? "587";
                var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "";
                var username = _configuration["EmailSettings:Username"] ?? "";
                if (string.IsNullOrEmpty(username))
                {
                    username = senderEmail;
                }
                var senderName = _configuration["EmailSettings:SenderName"] ?? "Journal Trend Tracker";
                var password = _configuration["EmailSettings:Password"] ?? "";
                var enableSslStr = _configuration["EmailSettings:EnableSsl"] ?? "true";

                if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("SMTP sender email or password is not configured. Email will not be sent, but process will proceed. Logged email details: To={ToEmail}, Subject={Subject}, Message={Message}", toEmail, subject, htmlMessage);
                    return;
                }

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
                        _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
                throw new Exception($"Failed to send email: {ex.Message}", ex);
            }
        }
    }
}
