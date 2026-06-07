using Monitor_zakupki;
using Monitor_zakupki.Models;
using Monitor_zakupki.Interfaces;
using Monitor_zakupki.Services;
using Microsoft.Extensions.Hosting.WindowsServices;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;

LogManager.Setup().LoadConfigurationFromFile("nlog.config");

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.AddWindowsService();

builder.Configuration.AddJsonFile("config/app-config.json", optional: false, reloadOnChange: true);

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
builder.Logging.AddNLog();

builder.Services.AddHostedService<Worker>();
builder.Services.AddTransient<INotificationService, NotificationService>();

builder.Services.Configure<ParserOptions>(
    builder.Configuration.GetSection("ParserOptions"));
builder.Services.AddTransient<IProcurementParserService, ProcurementParserService>();

builder.Services.Configure<UserSettings>(builder.Configuration.GetSection("UserSettings"));
builder.Services.Configure<MainSettings>(builder.Configuration.GetSection("MainSettings"));

var host = builder.Build();
host.Run();

LogManager.Shutdown();