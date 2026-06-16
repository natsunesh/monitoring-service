using System.Text.Json;
using Contracts;
using Monitor_zakupki.Models;
using Contracts.Interface;

namespace Monitor_zakupki.Services;

public sealed class ConfigService : IConfigService
{
    private readonly string _filePath;
    private readonly object _sync = new();
    private AppConfigDto _current;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
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
            _current = safe;
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(ToFileModel(safe), JsonOptions));
        }
    }

    private static AppConfigDto LoadFromFile(string path)
    {
        if (!File.Exists(path))
            return new AppConfigDto();

        var json = File.ReadAllText(path);
        var fileModel = JsonSerializer.Deserialize<AppConfigFileDto>(json) ?? new AppConfigFileDto();

        return FromFileModel(fileModel);
    }

    private static AppConfigDto Normalize(AppConfigDto config)
    {
        return new AppConfigDto
        {
            InnList = config.InnList ?? [],
            IntervalHours = config.IntervalHours <= 0 ? 1 : config.IntervalHours,
            Test = config.Test,
            FilePathToSavedHtml = config.FilePathToSavedHtml ?? "",
            FilePathToLogs = config.FilePathToLogs ?? "",
            FilePathToAppConfig = config.FilePathToAppConfig ?? ""
        };
    }

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

    private static AppConfigFileDto ToFileModel(AppConfigDto dto) => new()
    {
        UserSettings = new UserSettings
        {
            InnList = dto.InnList ?? [],
            IntervalHours = dto.IntervalHours
        },
        MainSettings = new MainSettings
        {
            Test = dto.Test
        },
        ParserOptions = new ParserOptions
        {
            FilePathToSavedHtml = dto.FilePathToSavedHtml,
            FilePathToLogs = dto.FilePathToLogs,
            FilePathToAppConfig = dto.FilePathToAppConfig
        }
    };
}