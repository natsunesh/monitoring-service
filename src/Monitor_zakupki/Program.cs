using Monitor_zakupki;
using Monitor_zakupki.Models;
using Monitor_zakupki.Interfaces;
using Monitor_zakupki.Services;

// Создаём builder для приложения-хоста worker-службы
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    // Указываем корень проекта, чтобы JSON и другие файлы читались из нужной папки
    ContentRootPath = @"L:\\программирование\\monitoring_service"
});

// Регистрируем фонового worker-а, который будет запускаться как hosted service
builder.Services.AddHostedService<Worker>();

// Подключаем JSON-конфиг приложения
// reloadOnChange: true позволяет подхватывать изменения файла без перезапуска
builder.Configuration.AddJsonFile("config/app-config.json", optional: false, reloadOnChange: true);

// Регистрируем сервис уведомлений
builder.Services.AddTransient<INotificationService, NotificationService>();

// Регистрируем сервис парсинга закупок
builder.Services.AddTransient<IProcurementParserService, FakeProcurementParserService>();

// Привязываем секцию UserSettings из JSON к классу UserSettings
builder.Services.Configure<UserSettings>(
    builder.Configuration.GetSection("UserSettings"));

// Привязываем секцию MainSettings из JSON к классу MainSettings
builder.Services.Configure<MainSettings>(
    builder.Configuration.GetSection("MainSettings"));

var host = builder.Build();
host.Run();




