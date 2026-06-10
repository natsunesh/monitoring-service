using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monitor_zakupki.Interfaces;
using Monitor_zakupki.Models;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Monitor_zakupki.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly UserSettings _userSettings;
        private readonly MainSettings _mainSettings;

        public NotificationService(
            ILogger<NotificationService> logger,
            IOptions<UserSettings> userSettings,
            IOptions<MainSettings> mainSettings)
        {
            _logger = logger;
            _userSettings = userSettings.Value;
            _mainSettings = mainSettings.Value;
        }

        public async Task SendAsync(string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(_mainSettings.Email.SmtpLogin);
            mailMessage.To.Add(_mainSettings.Email.SmtpTo);
            mailMessage.Subject = "Найдено новых закупок";
            mailMessage.Body = message;
            mailMessage.BodyEncoding = Encoding.UTF8;
            mailMessage.SubjectEncoding = Encoding.UTF8;
            mailMessage.IsBodyHtml = false;

            using var smtpClient = new SmtpClient(_mainSettings.Email.SmtpServer, _mainSettings.Email.SmtpPort)
            {
                Credentials = new NetworkCredential(_mainSettings.Email.SmtpLogin, _mainSettings.Email.SmtpPassword),
                EnableSsl = true
            };

            try
            {
                await smtpClient.SendMailAsync(mailMessage, cancellationToken);
                _logger.LogInformation("Email sent to {Email}", _mainSettings.Email.SmtpTo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email");
                throw;
            }
        }
    }
}