namespace Contracts
{
    public sealed class AppConfigDto
    {
        public string[] InnList { get; set; } = [];
        public double IntervalHours { get; set; } = 24;
        public bool Test { get; set; }
        public string FilePathToSavedHtml { get; set; } = "";
        public string FilePathToLogs { get; set; } = "";
        public string FilePathToAppConfig { get; set; } = "";
    }
}