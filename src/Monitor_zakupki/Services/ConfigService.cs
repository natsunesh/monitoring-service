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
        lock (_sync)
        {
            var root = LoadOrCreateRoot();

            UpdateUserSettings(root, config);
            UpdateMainSettings(root, config);
            UpdateEmail(root, config);
            UpdateStatus(root, config);

            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            File.WriteAllText(_filePath, root.ToJsonString(JsonOptions));

            _current = Merge(_current, config);
        }
    }

    private JsonObject LoadOrCreateRoot()
    {
        if (!File.Exists(_filePath))
            return new JsonObject();

        var json = File.ReadAllText(_filePath);
        return JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
    }

    private static void UpdateUserSettings(JsonObject root, AppConfigDto config)
    {
        var user = root["UserSettings"]?.AsObject() ?? new JsonObject();

        if (config.InnList is not null && config.InnList.Length > 0)
            user["InnList"] = JsonSerializer.SerializeToNode(config.InnList, JsonOptions);

        if (config.IntervalHours > 0)
            user["IntervalHours"] = config.IntervalHours;

        root["UserSettings"] = user;
    }

    private static void UpdateMainSettings(JsonObject root, AppConfigDto config)
    {
        var main = root["MainSettings"]?.AsObject() ?? new JsonObject();
        main["Test"] = config.Test;
        root["MainSettings"] = main;
    }

    private static void UpdateEmail(JsonObject root, AppConfigDto config)
    {
        if (string.IsNullOrWhiteSpace(config.SmtpTo))
            return;

        var email = root["Email"]?.AsObject() ?? new JsonObject();
        email["SmtpTo"] = config.SmtpTo;
        root["Email"] = email;
    }

    private static void UpdateStatus(JsonObject root, AppConfigDto config)
    {
        var service = root["Service"]?.AsObject() ?? new JsonObject();
        service["ServiceStatus"] = config.ServiceStatus.ToString();
        root["Service"] = service;
    }

    private static AppConfigDto LoadFromFile(string path)
    {
        if (!File.Exists(path))
            return new AppConfigDto();

        var json = File.ReadAllText(path);
        var root = JsonSerializer.Deserialize<ConfigFileDto>(json) ?? new ConfigFileDto();

        return new AppConfigDto
        {
            InnList = root.UserSettings?.InnList ?? [],
            IntervalHours = root.UserSettings?.IntervalHours ?? 24,
            Test = root.MainSettings?.Test ?? false,
            ServiceStatus = Enum.TryParse<ServiceStatus>(root.Service?.ServiceStatus, out var status)
                ? status
                : ServiceStatus.Stopped,
            SmtpTo = root.Email?.SmtpTo
        };
    }

    private static AppConfigDto Clone(AppConfigDto source) => new()
    {
        InnList = source.InnList.ToArray(),
        IntervalHours = source.IntervalHours,
        Test = source.Test,
        ServiceStatus = source.ServiceStatus,
        SmtpTo = source.SmtpTo
    };

    private static AppConfigDto Merge(AppConfigDto current, AppConfigDto update) => new()
    {
        InnList = update.InnList.Length > 0 ? update.InnList : current.InnList,
        IntervalHours = update.IntervalHours > 0 ? update.IntervalHours : current.IntervalHours,
        Test = update.Test,
        ServiceStatus = update.ServiceStatus,
        SmtpTo = !string.IsNullOrWhiteSpace(update.SmtpTo) ? update.SmtpTo : current.SmtpTo
    };
}

public sealed class ConfigFileDto
{
    public UserSettings? UserSettings { get; set; }
    public MainSettings? MainSettings { get; set; }
    public EmailSettings? Email { get; set; }
    public ServiceSettings? Service { get; set; }
}

public sealed class EmailSettings
{
    public string? SmtpTo { get; set; }
}

public sealed class ServiceSettings
{
    public string? ServiceStatus { get; set; }
}