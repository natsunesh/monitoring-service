namespace ZakupkiConfig;

public class SelectorsCss
{
    public string purchase_card { get; set; } = "";
    public string purchase_id { get; set; } = "";
    public string purchase_name { get; set; } = "";
    public string purchase_price { get; set; } = "";
    public string publisher_inn { get; set; } = "";
    public string purchase_link { get; set; } = "";
    public string purchase_date { get; set; } = "";
}

public class Selectors
{
    public SelectorsCss css { get; set; } = new();
    public Dictionary<string, string>? xpath { get; set; }
}

public class SmtpConfig
{
    public string host { get; set; } = "";
    public int port { get; set; } = 25;
    public string from_email { get; set; } = "";
    public string from_name { get; set; } = "";
    public bool use_ssl { get; set; } = true;
    public string? login { get; set; }
    public string? password { get; set; }
}

public class AppConfig
{
    public string version { get; set; } = "1.0";
    public int interval_hours { get; set; } = 1;
    public Selectors selectors { get; set; } = new();
    public List<string> emails { get; set; } = new();
    public List<string> inn_list { get; set; } = new();
    public List<string> history { get; set; } = new();
    public SmtpConfig smtp { get; set; } = new();
}