using Monitor_zakupki.Models;
using Monitor_zakupki.Interfaces;
using Monitor_zakupki.Services;

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
            _logger.LogInformation("Start program");

            if (_mainSettings.Test)
            {
                await RunOnceAsync(stoppingToken);
            }
            else
            {
                int iteration = 0;
                while (!stoppingToken.IsCancellationRequested)
                {
                    await RunOnceAsync(stoppingToken, iteration++);

                    try
                    {
                        await Task.Delay(TimeSpan.FromHours(_userSettings.IntervalHours), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Stoping program");
                        break;

                    }
                }
            }

            
        }

        private async Task RunOnceAsync(CancellationToken stoppingToken, int iteration = 0)
        {
            var items = await _parserService.GetNewProcurementsAsync(stoppingToken);
            if (items.Count > 0)
            {
                await _notificationService.SendAsync($"Найдено новых закупок: {items.Count}", stoppingToken);
            }
        }



    }
}
