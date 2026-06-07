namespace Monitor_zakupki.Models
{
    public class RootSettings
    {
        public UserSettings UserSettings { get; set; }
        public MainSettings MainSettings { get; set; }
    }
    public class UserSettings
    {
        public required string[] InnList { get; set; }
        public required string NotificationEmail { get; set; }
        public double IntervalHours { get; set; }
    }

    public class MainSettings
    {
        public EmailSettings email { get; set; }
        public string PathToLog { get; set; }
        public bool test { get; set; }
    }

    public class EmailSettings
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpLogin { get; set; }
        public string SmtpPassword { get; set; }
        public string SmtpFrom { get; set; }
    }

}
