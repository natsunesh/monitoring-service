using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GUI_zakupki.Helpers;
using GUI_zakupki.Services;
using Monitor_zakupki.Contracts;

namespace GUI_zakupki.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly PipeClient _pipeClient = new();

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
        set { _intervalHours = value; OnPropertyChanged(); }
    }

    public bool Test
    {
        get => _test;
        set { _test = value; OnPropertyChanged(); }
    }

    public string FilePathToSavedHtml
    {
        get => _filePathToSavedHtml;
        set { _filePathToSavedHtml = value; OnPropertyChanged(); }
    }

    public string FilePathToLogs
    {
        get => _filePathToLogs;
        set { _filePathToLogs = value; OnPropertyChanged(); }
    }

    public string FilePathToAppConfig
    {
        get => _filePathToAppConfig;
        set { _filePathToAppConfig = value; OnPropertyChanged(); }
    }

    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public string? NewInn
    {
        get => _newInn;
        set { _newInn = value; OnPropertyChanged(); }
    }

    public string? SelectedInn
    {
        get => _selectedInn;
        set { _selectedInn = value; OnPropertyChanged(); }
    }

    public ICommand LoadCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand AddInnCommand { get; }
    public ICommand RemoveInnCommand { get; }

    public MainViewModel()
    {
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        AddInnCommand = new RelayCommand(_ => AddInn(), _ => !string.IsNullOrWhiteSpace(NewInn));
        RemoveInnCommand = new RelayCommand(_ => RemoveInn(), _ => SelectedInn is not null);
    }

    private async Task LoadAsync()
    {
        try
        {
            var response = await _pipeClient.SendAsync(new PipeRequest
            {
                Command = PipeCommands.GetSettings
            });

            if (!response.Success || response.Payload is null)
            {
                Status = response.Error ?? "Load failed";
                return;
            }

            Apply(response.Payload);
            Status = "Loaded";
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            var response = await _pipeClient.SendAsync(new PipeRequest
            {
                Command = PipeCommands.UpdateSettings,
                Payload = Collect()
            });

            Status = response.Success ? "Saved" : (response.Error ?? "Save failed");
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    private void AddInn()
    {
        var value = NewInn?.Trim();
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (!InnList.Contains(value))
            InnList.Add(value);

        NewInn = "";
    }

    private void RemoveInn()
    {
        if (SelectedInn is null)
            return;

        InnList.Remove(SelectedInn);
        SelectedInn = null;
    }

    private void Apply(AppConfigDto dto)
    {
        InnList.Clear();
        foreach (var inn in dto.InnList ?? [])
            InnList.Add(inn);

        IntervalHours = dto.IntervalHours;
        Test = dto.Test;
        FilePathToSavedHtml = dto.FilePathToSavedHtml;
        FilePathToLogs = dto.FilePathToLogs;
        FilePathToAppConfig = dto.FilePathToAppConfig;
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