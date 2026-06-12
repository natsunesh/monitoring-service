using Monitor_zakupki.Models;
using Monitor_zakupki.Interfaces;
using Monitor_zakupki.Services;
using System.Text;

using Microsoft.Extensions.Options;

namespace Monitor_zakupki
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly UserSettings _userSettings;
        private readonly MainSettings _mainSettings;
        private readonly INotificationService _notificationService;
        private readonly IProcurementParserService _parserService;

        public Worker(
            ILogger<Worker> logger,
            IOptions<UserSettings> userSettings,
            IOptions<MainSettings> mainSettings,
            INotificationService notificationService,
            IProcurementParserService parserService)
        {
            _logger = logger;
            _userSettings = userSettings.Value;
            _mainSettings = mainSettings.Value;
            _notificationService = notificationService;
            _parserService = parserService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Start program");

                if (_mainSettings.Test)
                {
                    _logger.LogInformation("Test mode enabled. Running once and stopping.");
                    await RunOnceAsync(stoppingToken);
                }
                else
                {
                    _logger.LogInformation("Normal mode enabled. Service will run in loop.");

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        await RunOnceAsync(stoppingToken);

                        var delay = TimeSpan.FromHours(_userSettings.IntervalHours);
                        if (delay <= TimeSpan.Zero)
                            delay = TimeSpan.FromMinutes(1);

                        await Task.Delay(delay, stoppingToken);
                    }
                }

                _logger.LogInformation("Stopping program");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Service cancellation requested.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Worker failed unexpectedly");
                throw;
            }
        }

        private async Task RunOnceAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Checking procurements...");

            var items = await _parserService.GetNewProcurementsAsync(stoppingToken);

            _logger.LogInformation("Check finished. Found {Count} items.", items.Count);

            if (items.Count == 0)
                return;

            var message = new StringBuilder();
            message.AppendLine($"Найдена закупка");
            message.AppendLine();

            foreach (var pi in items)
            {
                message.AppendLine($"Наименование организации: {pi.Name}");
                message.AppendLine($"ИНН: {pi.Inn}");
                message.AppendLine($"Номер закупки: {pi.Number}");
                message.AppendLine($"Ссылка: {pi.Url}");
                message.AppendLine($"Дата размещения: {pi.Date}");
                message.AppendLine();
            }

            await _notificationService.SendAsync(message.ToString(), stoppingToken);
        }
    }
}
