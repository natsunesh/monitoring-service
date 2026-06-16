namespace Contracts.Interface
{
	public interface IPipeService
	{
		Task StartAsync(CancellationToken cancellationToken);
		Task StopAsync();
		bool IsRunning { get; }
	}
}