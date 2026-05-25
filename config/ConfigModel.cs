namespace ZakupkiConfig;

public class AppConfig
{
    public int interval_hours { get; set; } = 1;
    public List<string> emails { get; set; } = new();
    public List<string> inn_list { get; set; } = new();
    public List<string> history { get; set; } = new();
}