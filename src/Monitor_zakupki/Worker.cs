using Monitor_zakupki.Models;
using Microsoft.Extensions.Options;

namespace Monitor_zakupki
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly UserSettings _userSettings;
        private readonly MainSettings _mainSettings;

        public Worker(ILogger<Worker> logger, IOptions<UserSettings> userSettings, IOptions<MainSettings> mainSettings)
        {
            _logger = logger;
            _userSettings = userSettings.Value;
            _mainSettings = mainSettings.Value;
        }

         

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int iteration = 0;
            _logger.LogInformation("Start program");
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Log itteration{iteration++}");
                await Task.Delay(TimeSpan.FromHours(_userSettings.IntervalHours), stoppingToken);
            }
            _logger.LogInformation("stoping program");
        }

        

    }
}
