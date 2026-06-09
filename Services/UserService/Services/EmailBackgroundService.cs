using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using UserService.Models;
using UserService.Repositories;

namespace UserService.Services
{
    public class EmailBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailBackgroundService> _logger;

        public EmailBackgroundService(IServiceProvider serviceProvider, ILogger<EmailBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try { await ProcessEmailQueueAsync(); }
                catch (Exception ex) { _logger.LogError(ex, "Error processing email queue."); }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ProcessEmailQueueAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var pendingEmails = await repository.GetPendingEmailsAsync();

            foreach (var email in pendingEmails)
            {
                try
                {
                    _logger.LogInformation("Sending email [{Id}] to {ToEmail}", email.Id, email.ToEmail);

                    // ── MailKit Implementation ─────────────────────────────
                    // using var client = new MimeKit.MimeMessage();
                    // var message = new MimeKit.MimeMessage();
                    // message.From.Add(new MimeKit.MailboxAddress("Journal Tracker", "noreply@example.com"));
                    // message.To.Add(MimeKit.MailboxAddress.Parse(email.ToEmail));
                    // message.Subject = email.Subject;
                    // var bodyBuilder = new MimeKit.BodyBuilder { HtmlBody = email.BodyHtml };
                    // message.Body = bodyBuilder.ToMessageBody();
                    // using var smtp = new MailKit.Net.Smtp.SmtpClient();
                    // await smtp.ConnectAsync("smtp.host", 587, MailKit.Security.SecureSocketOptions.StartTls);
                    // await smtp.AuthenticateAsync("user", "pass");
                    // await smtp.SendAsync(message);
                    // await smtp.DisconnectAsync(true);
                    // ─────────────────────────────────────────────────────

                    await repository.UpdateEmailStatusAsync(email.Id, DeliveryStatus.sent, DateTime.UtcNow, null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email [{Id}]", email.Id);
                    await repository.UpdateEmailStatusAsync(email.Id, DeliveryStatus.failed, null, ex.Message);
                }
            }
        }
    }
}
