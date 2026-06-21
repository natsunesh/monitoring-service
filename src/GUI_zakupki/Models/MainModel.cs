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
    private ServiceStatus _serviceState = ServiceStatus.Stopped;

    private int _intervalHours = 24;
    private bool _test;
    private string _status = "Ready";
    private string? _newInn;
    private string? _selectedInn;
    private string? _smtpTo;


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





    public ServiceStatus ServiceState
    {
        get => _serviceState;
        set
        {
            if (_serviceState == value) return;
            _serviceState = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ServiceStatusText));
        }
    }

    public string ServiceStatusText =>
        ServiceState switch
        {
            ServiceStatus.Running => "Запущено",
            ServiceStatus.Stopped => "Остановлено",
            ServiceStatus.Error => "Ошибка",
            _ => "Неизвестно"
        };

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
        Status = "Loading...";
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

            await Application.Current.Dispatcher.InvokeAsync(() => Apply(response.Payload));
            Status = "Loaded";
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    private async Task SaveAsync()
    {
        Status = "Saving...";
        try
        {
            var dto = Collect();
            var response = await _pipeClient.SendAsync(new PipeRequest
            {
                Command = PipeCommands.UpdateSettings,
                Payload = dto
            });

            if (!response.Success)
            {
                Status = response.Error ?? "Save failed";
                return;
            }

            if (response.Payload is not null)
                await Application.Current.Dispatcher.InvokeAsync(() => Apply(response.Payload));

            Status = "Saved";
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

        IntervalHours = (int)dto.IntervalHours;
        Test = dto.Test;
        ServiceState = dto.ServiceStatus;
    }

    private AppConfigDto Collect() => new()
    {
        InnList = InnList.ToArray(),
        IntervalHours = IntervalHours,
        Test = Test,
        ServiceStatus = ServiceState
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}