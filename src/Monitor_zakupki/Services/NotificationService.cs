using Monitor_zakupki.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;


namespace Monitor_zakupki.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string message, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Notification: {Message}", message);
            return Task.CompletedTask;
        }
    }
}

