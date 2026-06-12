using Monitor_zakupki;
using Monitor_zakupki.Models;
using Monitor_zakupki.Interfaces;
using Monitor_zakupki.Services;
using Microsoft.Extensions.Hosting.WindowsServices;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;

LogManager.Setup().LoadConfigurationFromFile(Path.Combine(AppContext.BaseDirectory, "nlog.config"));

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.AddWindowsService();

builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "config/app-config.json"),
    optional: false,
    reloadOnChange: true);

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
builder.Logging.AddNLog();

builder.Services.Configure<ParserOptions>(
    builder.Configuration.GetSection("ParserOptions"));
builder.Services.Configure<UserSettings>(
    builder.Configuration.GetSection("UserSettings"));
builder.Services.Configure<MainSettings>(
    builder.Configuration.GetSection("MainSettings"));

builder.Services.AddHostedService<Worker>();
builder.Services.AddTransient<INotificationService, NotificationService>();

if (builder.Configuration.GetValue<bool>("MainSettings:Test"))
{
    builder.Services.AddTransient<IProcurementParserService, FakeProcurementParserService>();
}
else
{
    builder.Services.AddTransient<IProcurementParserService, ProcurementParserService>();
}

var host = builder.Build();
try
{
    host.Run();
}
finally
{
    LogManager.Shutdown();
}