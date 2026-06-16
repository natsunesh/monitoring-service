using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Contracts;
using Contracts.Interface;
using GUI_zakupki.Helpers;
using Microsoft.Extensions.Logging;

namespace GUI_zakupki.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IPipeClient _pipeClient;
    private readonly ILogger<MainViewModel> _logger;

    private int _intervalHours = 24;
    private bool _test;
    private string _filePathToSavedHtml = "";
    private string _filePathToLogs = "";
    private string _filePathToAppConfig = "";
    private string _status = "Ready";
    private string? _newInn;
    private string? _selectedInn;

    public ObservableCollection<string> InnList { get; } = new();

    public int IntervalHours
    {
        get => _intervalHours;
        set
        {
            if (_intervalHours == value) return;
            _intervalHours = value;
            OnPropertyChanged();
            _logger.LogInformation("IntervalHours changed to {Value}", value);
        }
    }

    public bool Test
    {
        get => _test;
        set
        {
            if (_test == value) return;
            _test = value;
            OnPropertyChanged();
            _logger.LogInformation("Test changed to {Value}", value);
        }
    }

    public string FilePathToSavedHtml
    {
        get => _filePathToSavedHtml;
        set
        {
            if (_filePathToSavedHtml == value) return;
            _filePathToSavedHtml = value;
            OnPropertyChanged();
            _logger.LogInformation("FilePathToSavedHtml changed to {Value}", value);
        }
    }

    public string FilePathToLogs
    {
        get => _filePathToLogs;
        set
        {
            if (_filePathToLogs == value) return;
            _filePathToLogs = value;
            OnPropertyChanged();
            _logger.LogInformation("FilePathToLogs changed to {Value}", value);
        }
    }

    public string FilePathToAppConfig
    {
        get => _filePathToAppConfig;
        set
        {
            if (_filePathToAppConfig == value) return;
            _filePathToAppConfig = value;
            OnPropertyChanged();
            _logger.LogInformation("FilePathToAppConfig changed to {Value}", value);
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            _status = value;
            OnPropertyChanged();
            _logger.LogInformation("Status = {Status}", value);
        }
    }

    public string? NewInn
    {
        get => _newInn;
        set
        {
            if (_newInn == value) return;
            _newInn = value;
            OnPropertyChanged();
            _logger.LogInformation("NewInn changed to {Value}", value);
            ((RelayCommand)AddInnCommand).RaiseCanExecuteChanged();
        }
    }

    public string? SelectedInn
    {
        get => _selectedInn;
        set
        {
            if (_selectedInn == value) return;
            _selectedInn = value;
            OnPropertyChanged();
            _logger.LogInformation("SelectedInn changed to {Value}", value);
            ((RelayCommand)RemoveInnCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand LoadCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand AddInnCommand { get; }
    public ICommand RemoveInnCommand { get; }

    public MainViewModel(IPipeClient pipeClient, ILogger<MainViewModel> logger)
    {
        _pipeClient = pipeClient;
        _logger = logger;

        _logger.LogInformation("MainViewModel created");

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        AddInnCommand = new RelayCommand(_ => AddInn(), _ => !string.IsNullOrWhiteSpace(NewInn));
        RemoveInnCommand = new RelayCommand(_ => RemoveInn(), _ => SelectedInn is not null);
    }

    private async Task LoadAsync()
    {
        _logger.LogInformation("LoadAsync started");
        Status = "Loading...";

        try
        {
            _logger.LogInformation("Sending GetSettings request");
            var response = await _pipeClient.SendAsync(new PipeRequest
            {
                Command = PipeCommands.GetSettings
            });

            _logger.LogInformation("GetSettings response received. Success={Success}", response.Success);

            if (!response.Success || response.Payload is null)
            {
                Status = response.Error ?? "Load failed";
                _logger.LogWarning("Load failed: {Error}", Status);
                return;
            }

            _logger.LogInformation("Applying payload");
            await Application.Current.Dispatcher.InvokeAsync(() => Apply(response.Payload));
            _logger.LogInformation("Payload applied");

            Status = "Loaded";
            _logger.LogInformation("LoadAsync completed");
        }
        catch (Exception ex)
        {
            Status = ex.Message;
            _logger.LogError(ex, "LoadAsync exception");
        }
    }

    private async Task SaveAsync()
    {
        _logger.LogInformation("SaveAsync started");
        Status = "Saving...";

        try
        {
            var dto = Collect();
            _logger.LogInformation(
                "Sending UpdateSettings request. InnCount={InnCount}, Interval={Interval}, Test={Test}",
                dto.InnList.Length,
                dto.IntervalHours,
                dto.Test);

            var response = await _pipeClient.SendAsync(new PipeRequest
            {
                Command = PipeCommands.UpdateSettings,
                Payload = dto
            });

            _logger.LogInformation("UpdateSettings response received. Success={Success}", response.Success);

            if (!response.Success)
            {
                Status = response.Error ?? "Save failed";
                _logger.LogWarning("Save failed: {Error}", Status);
                return;
            }

            if (response.Payload is not null)
            {
                _logger.LogInformation("Applying returned payload");
                await Application.Current.Dispatcher.InvokeAsync(() => Apply(response.Payload));
                _logger.LogInformation("Returned payload applied");
            }

            Status = "Saved";
            _logger.LogInformation("SaveAsync completed");
        }
        catch (Exception ex)
        {
            Status = ex.Message;
            _logger.LogError(ex, "SaveAsync exception");
        }
    }

    private void AddInn()
    {
        _logger.LogInformation("AddInn started. NewInn={NewInn}", NewInn);

        var value = NewInn?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            _logger.LogInformation("AddInn aborted: empty value");
            return;
        }

        if (!InnList.Contains(value))
        {
            InnList.Add(value);
            _logger.LogInformation("Inn added: {Inn}", value);
        }
        else
        {
            _logger.LogInformation("Inn already exists: {Inn}", value);
        }

        NewInn = "";
    }

    private void RemoveInn()
    {
        _logger.LogInformation("RemoveInn started. SelectedInn={SelectedInn}", SelectedInn);

        if (SelectedInn is null)
        {
            _logger.LogInformation("RemoveInn aborted: SelectedInn is null");
            return;
        }

        InnList.Remove(SelectedInn);
        _logger.LogInformation("Inn removed: {Inn}", SelectedInn);
        SelectedInn = null;
    }

    private void Apply(AppConfigDto dto)
    {
        _logger.LogInformation(
            "Apply started. InnCount={InnCount}, Interval={Interval}, Test={Test}, Html={Html}, Logs={Logs}, Config={Config}",
            dto.InnList?.Length ?? 0,
            dto.IntervalHours,
            dto.Test,
            dto.FilePathToSavedHtml,
            dto.FilePathToLogs,
            dto.FilePathToAppConfig);

        InnList.Clear();
        foreach (var inn in dto.InnList ?? [])
            InnList.Add(inn);

        IntervalHours = (int)dto.IntervalHours;
        Test = dto.Test;
        FilePathToSavedHtml = dto.FilePathToSavedHtml;
        FilePathToLogs = dto.FilePathToLogs;
        FilePathToAppConfig = dto.FilePathToAppConfig;

        _logger.LogInformation("Apply completed");
    }

    private AppConfigDto Collect() => new()
    {
        InnList = InnList.ToArray(),
        IntervalHours = IntervalHours,
        Test = Test,
        FilePathToSavedHtml = FilePathToSavedHtml,
        FilePathToLogs = FilePathToLogs,
        FilePathToAppConfig = FilePathToAppConfig
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}