using System.ServiceProcess;
using Contracts;

private static ServiceStatus ReadServiceStatus(string serviceName)
{
    try
    {
        using var controller = new ServiceController(serviceName);

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