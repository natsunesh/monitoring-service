using System.Windows;
using GUI_zakupki.DependencyInjection;
using GUI_zakupki.Views;
using Microsoft.Extensions.DependencyInjection;
using GUI_zakupki.ViewModels;
using Microsoft.Extensions.Hosting;

namespace GUI_zakupki;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddServices();
        builder.Services.AddTransient<MainWindow>();

        _host = builder.Build();

        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}