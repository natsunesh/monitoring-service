namespace Monitor_zakupki.Models;

public sealed class AppConfigFileDto
{
    public UserSettings? UserSettings { get; set; }
    public MainSettings? MainSettings { get; set; }
    public ParserOptions? ParserOptions { get; set; }
}