
namespace Monitor_zakupki.Interfaces
{
	public interface INotificationService
	{
		Task SendAsync(string message, CancellationToken cancellationToken = default);
	}
}
