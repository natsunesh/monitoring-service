using System.Text.Json;

namespace ZakupkiConfig;

public static class ConfigReader
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static AppConfig Load(string configPath)
    {
        if (!File.Exists(configPath))
            throw new FileNotFoundException($"Конфиг не найден: {configPath}");

        var json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<AppConfig>(json, _options);

        if (config == null)
            throw new InvalidOperationException("Не удалось десериализовать конфиг");

        return config;
    }

    public static void Save(string configPath, AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, _options);
        File.WriteAllText(configPath, json);
    }
}
--
--