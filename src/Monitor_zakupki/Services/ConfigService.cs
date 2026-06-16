using System.Text.Json;
using System.Text.Json.Nodes;
using Contracts;
using Contracts.Interface;
using Monitor_zakupki.Models;

namespace Monitor_zakupki.Services;

public sealed class ConfigService : IConfigService
{
    private readonly string _filePath;
    private readonly object _sync = new();
    private AppConfigDto _current;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public ConfigService(IConfiguration configuration, IHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "config", "app-config.json");
        _current = LoadFromFile(_filePath);
    }

    public AppConfigDto Get()
    {
        lock (_sync)
        {
            return Clone(_current);
        }
    }

    public void Update(AppConfigDto config)
    {
        var safe = Normalize(config);

        lock (_sync)
        {
            var root = LoadOrCreateRoot();

            UpdateUserSettings(root, safe);
            UpdateMainSettings(root, safe);
            UpdateParserOptions(root, safe);
            UpdateServiceState(root, safe);

            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            File.WriteAllText(_filePath, root.ToJsonString(JsonOptions));

            _current = safe;
        }
    }

    private JsonObject LoadOrCreateRoot()
    {
        if (!File.Exists(_filePath))
            return new JsonObject();

        var json = File.ReadAllText(_filePath);
        return JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
    }

    private static void UpdateUserSettings(JsonObject root, AppConfigDto dto)
    {
        var user = root["UserSettings"]?.AsObject() ?? new JsonObject();

        user["InnList"] = JsonSerializer.SerializeToNode(dto.InnList ?? [], JsonOptions);
        user["IntervalHours"] = dto.IntervalHours;

        root["UserSettings"] = user;
    }

    private static void UpdateMainSettings(JsonObject root, AppConfigDto dto)
    {
        var main = root["MainSettings"]?.AsObject() ?? new JsonObject();

        main["Test"] = dto.Test;

        root["MainSettings"] = main;
    }

    private static void UpdateParserOptions(JsonObject root, AppConfigDto dto)
    {
        var parser = root["ParserOptions"]?.AsObject() ?? new JsonObject();

        parser["FilePathToSavedHtml"] = dto.FilePathToSavedHtml;
        parser["FilePathToLogs"] = dto.FilePathToLogs;
        parser["FilePathToAppConfig"] = dto.FilePathToAppConfig;

        root["ParserOptions"] = parser;
    }

    private static void UpdateServiceState(JsonObject root, AppConfigDto dto)
    {
        var state = root["ServiceState"]?.AsObject() ?? new JsonObject();

        state["Enabled"] = dto.Test;
        root["ServiceState"] = state;
    }

    private static AppConfigDto LoadFromFile(string path)
    {
        if (!File.Exists(path))
            return new AppConfigDto();

        var json = File.ReadAllText(path);
        var fileModel = JsonSerializer.Deserialize<AppConfigFileDto>(json) ?? new AppConfigFileDto();

        return FromFileModel(fileModel);
    }

    private static AppConfigDto Normalize(AppConfigDto config) => new()
    {
        InnList = config.InnList ?? [],
        IntervalHours = config.IntervalHours <= 0 ? 1 : config.IntervalHours,
        Test = config.Test,
        FilePathToSavedHtml = config.FilePathToSavedHtml ?? "",
        FilePathToLogs = config.FilePathToLogs ?? "",
        FilePathToAppConfig = config.FilePathToAppConfig ?? ""
    };

    private static AppConfigDto Clone(AppConfigDto source) => new()
    {
        InnList = source.InnList.ToArray(),
        IntervalHours = source.IntervalHours,
        Test = source.Test,
        FilePathToSavedHtml = source.FilePathToSavedHtml,
        FilePathToLogs = source.FilePathToLogs,
        FilePathToAppConfig = source.FilePathToAppConfig
    };

    private static AppConfigDto FromFileModel(AppConfigFileDto file) => new()
    {
        InnList = file.UserSettings?.InnList ?? [],
        IntervalHours = file.UserSettings?.IntervalHours ?? 24,
        Test = file.MainSettings?.Test ?? false,
        FilePathToSavedHtml = file.ParserOptions?.FilePathToSavedHtml ?? "",
        FilePathToLogs = file.ParserOptions?.FilePathToLogs ?? "",
        FilePathToAppConfig = file.ParserOptions?.FilePathToAppConfig ?? ""
    };
}