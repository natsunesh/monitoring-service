using Contracts;
using GUI_zakupki.Services;
using GUI_zakupki.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GUI_zakupki.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddTransient<IPipeClient, PipeClient>();
        services.AddTransient<MainViewModel>();
        return services;
    }
}