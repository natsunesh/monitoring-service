using System.ServiceProcess;
using Contracts;
using Contracts.Interface;

namespace Monitor_zakupki.Services;

public sealed class ServiceStatusReader : IServiceStatusReader
{
    public ServiceStatus ReadServiceStatus(string serviceName)
    {
        try
        {
            using var controller = new ServiceController(serviceName);
            controller.Refresh();

            return controller.Status switch
            {
                ServiceControllerStatus.Running => ServiceStatus.Running,
                ServiceControllerStatus.Stopped => ServiceStatus.Stopped,
                _ => ServiceStatus.Error
            };
        }
        catch
        {
            return ServiceStatus.Error;
        }
    }
}