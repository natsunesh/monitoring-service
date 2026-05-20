using Monitor_zakupki;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

class UserSettings
{
    public string[] InnList {  get; set; }
    public string NotificationEmail { get; set; }
    public int IntervalHours { get; set; }
}

class MainSettings
{
    public string PathToLog { get; set; }
}

class EmailSettings
{
    public string SmtpServer { get; set; }
    public int SmtpPort { get; set; }
    public string SmtpLogin { get; set; }
    public string SmtpPassword { get; set; }
    public string SmtpFrom { get; set; }

}