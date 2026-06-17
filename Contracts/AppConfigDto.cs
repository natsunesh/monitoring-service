namespace Contracts
{
    public sealed class AppConfigDto
    {
        public string[] InnList { get; set; } = [];
        public double IntervalHours { get; set; } = 24;
        public bool Test { get; set; }
        public ServiceStatus ServiceStatus { get; set; }
        public string? SmtpTo { get; set; }
    }
}