
class EmailSettings
{
    public string SmtpServer { get; set; }
    public int SmtpPort { get; set; }
    public string SmtpLogin { get; set; }
    public string SmtpPassword { get; set; }
    public string SmtpFrom { get; set; }

}

class MainSettings
{
    public EmailSettings Email {  get; set; }
    public string PathToLog { get; set; }
}
