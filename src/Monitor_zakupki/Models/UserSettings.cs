namespace Monitor_zakupki.Models
{
	public class UserSettings
	{
		public required string[] InnList { get; set; }
		public required string NotificationEmail { get; set; }
		public int IntervalHours { get; set; }
	}
}
