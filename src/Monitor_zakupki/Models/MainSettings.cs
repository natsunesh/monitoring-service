namespace Monitor_zakupki.Models
{
    public class EmailSettings
    {
        public required string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public required string SmtpLogin { get; set; }
        public required string SmtpPassword { get; set; }
        public required string SmtpFrom { get; set; }

    }

    public class MainSettings
    {
        public EmailSettings? Email { get; set; }
        public string? PathToLog { get; set; }
        public bool Test { get; set; }
    }
}


