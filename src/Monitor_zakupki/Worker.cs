using Monitor_zakupki.Models;
using Monitor_zakupki.Interfaces;
using Monitor_zakupki.Services;

using Microsoft.Extensions.Options;

namespace Monitor_zakupki
{
    public class Worker : BackgroundService
    {
        // Логгер для записи событий работы службы
        private readonly ILogger<Worker> _logger;

        // Настройки пользователя: ИНН, email, интервал проверки и т.д.
        private readonly UserSettings _userSettings;

        // Главные настройки: test-режим, путь к логам и т.п.
        private readonly MainSettings _mainSettings;

        // Сервис уведомлений, который отправляет сообщение о найденных закупках
        private readonly INotificationService _notificationService;

        // Сервис парсинга закупок
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
            // Старт службы
            _logger.LogInformation("Start program");

            // Если включён тестовый режим, выполняем один проход и завершаемся
            if (_mainSettings.Test)
            {
                await RunOnceAsync(stoppingToken);
            }
            else
            {
                

                // Основной цикл службы: работает, пока не придёт сигнал остановки (ctrl+c)
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Один проход проверки закупок
                    await RunOnceAsync(stoppingToken);

                    try
                    {
                        // Пауза между проверками, интервал берётся из настроек
                        await Task.Delay(TimeSpan.FromHours(_userSettings.IntervalHours), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Если службу остановили во время ожидания, выходим из цикла
                        break;
                    }
                }
            }

            // Если вышли не через catch, то лог завершения всё равно должен быть виден
            _logger.LogInformation("Stopping program");
        }

        private async Task RunOnceAsync(CancellationToken stoppingToken)
        {
            // Получаем список новых закупок от парсера
            var items = await _parserService.GetNewProcurementsAsync(stoppingToken);

            // Если закупки найдены, отправляем уведомление
            if (items.Count > 0)
            {
                await _notificationService.SendAsync($"Найдено новых закупок: {items.Count}", stoppingToken);
            }
        }
    }
}
