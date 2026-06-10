namespace Monitor_zakupki.Models
{
    public class RootSettings
    {
        public UserSettings UserSettings { get; set; } = new();
        public MainSettings MainSettings { get; set; } = new();
        public ParserOptions ParserOptions { get; set; } = new();
    }

    public class UserSettings
    {
        public string[] InnList { get; set; } = Array.Empty<string>();
        public string NotificationEmail { get; set; } = string.Empty;
        public double IntervalHours { get; set; }
    }

    public class MainSettings
    {
        public EmailSettings Email { get; set; } = new();
        public string PathToLog { get; set; } = string.Empty;
        public bool Test { get; set; }
    }

    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpLogin { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string SmtpTo { get; set; } = string.Empty;
    }


}