namespace Contracts.Interface;

public interface IServiceStatusReader
{
    ServiceStatus ReadServiceStatus(string serviceName);
}