using Monitor_zakupki;
using Monitor_zakupki.Models;
using Monitor_zakupki.Interfaces;
using Monitor_zakupki.Services;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    ContentRootPath = @"L:\программирование\monitoring_service"
});


builder.Services.AddHostedService<Worker>();

builder.Configuration.AddJsonFile("config/app-config.json", optional: false, reloadOnChange: true);

builder.Services.AddTransient<INotificationService, NotificationService>();

builder.Services.Configure<UserSettings>(
    builder.Configuration.GetSection("UserSettings"));

builder.Services.Configure<MainSettings>(
    builder.Configuration.GetSection("MainSettings"));


var host = builder.Build();
host.Run();




