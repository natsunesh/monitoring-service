using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Contracts;
using Contracts.Interface;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Monitor_zakupki.Services;

public sealed class PipeServer : IHostedService
{
    private const string PipeName = "monitor_zakupki_pipe";
    private readonly IConfigService _configService;
    private readonly ILogger<PipeServer> _logger;
    private CancellationTokenSource? _cts;
    private Task? _runTask;
    private readonly IServiceStatusReader _serviceStatusReader;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PipeServer(IConfigService configService, IServiceStatusReader serviceStatusReader, ILogger<PipeServer> logger)
    {
        _configService = configService;
        _serviceStatusReader = serviceStatusReader;
        _logger = logger;
        _logger.LogInformation("PipeServer created");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PipeServer StartAsync called");

        if (_runTask is not null && !_runTask.IsCompleted)
            return Task.CompletedTask;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runTask = RunAsync(_cts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PipeServer StopAsync called");

        if (_cts is null || _runTask is null)
            return;

        _cts.Cancel();

        try
        {
            await _runTask.WaitAsync(cancellationToken);
        }
        catch { }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RunAsync loop started");

        while (!cancellationToken.IsCancellationRequested)
        {
            await using var server = new NamedPipeServerStream(
                PipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            try
            {
                _logger.LogInformation("Waiting for pipe connection...");
                await server.WaitForConnectionAsync(cancellationToken);
                _logger.LogInformation("Pipe connected");

                await HandleConnectionAsync(server, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RunAsync error");
            }
        }
    }

    private async Task HandleConnectionAsync(NamedPipeServerStream server, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("HandleConnectionAsync started");

            var lengthBuffer = new byte[4];
            _logger.LogInformation("Reading request length...");
            await ReadExactlyAsync(server, lengthBuffer, 4, cancellationToken);
            var requestLength = BitConverter.ToInt32(lengthBuffer, 0);
            _logger.LogInformation("Request length={Length}", requestLength);

            var requestBuffer = new byte[requestLength];
            _logger.LogInformation("Reading request payload...");
            await ReadExactlyAsync(server, requestBuffer, requestLength, cancellationToken);

            var requestJson = Encoding.UTF8.GetString(requestBuffer);
            var request = JsonSerializer.Deserialize<PipeRequest>(requestJson, JsonOptions);

            if (request is null)
            {
                await WriteResponseAsync(server, new PipeResponse { Success = false, Error = "Invalid request" }, cancellationToken);
                return;
            }

            _logger.LogInformation("Request deserialized. Command={Command}", request.Command);

            var response = await ProcessRequestAsync(request);
            await WriteResponseAsync(server, response, cancellationToken);
            _logger.LogInformation("Response sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HandleConnectionAsync error");
        }
    }

    private async Task WriteResponseAsync(Stream stream, PipeResponse response, CancellationToken cancellationToken)
    {
        var responseJson = JsonSerializer.Serialize(response, JsonOptions);
        var responseBytes = Encoding.UTF8.GetBytes(responseJson);

        _logger.LogInformation("Writing response length={Length}", responseBytes.Length);
        await stream.WriteAsync(BitConverter.GetBytes(responseBytes.Length), cancellationToken);
        await stream.WriteAsync(responseBytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private async Task ReadExactlyAsync(Stream stream, byte[] buffer, int count, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < count)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), cancellationToken);
            if (read == 0)
                throw new EndOfStreamException("Pipe closed before all data was received.");

            offset += read;
        }
    }

    private Task<PipeResponse> ProcessRequestAsync(PipeRequest request)
    {
        _logger.LogInformation("ProcessRequestAsync started. Command={Command}", request.Command);

        return request.Command switch
        {
            PipeCommands.GetSettings => Task.FromResult(new PipeResponse
            {
                Success = true,
                Payload = GetCurrentConfig()
            }),

            PipeCommands.UpdateSettings when request.Payload is not null => UpdateAsync(request.Payload),

            PipeCommands.UpdateSettings => Task.FromResult(new PipeResponse
            {
                Success = false,
                Error = "Payload is empty"
            }),

            _ => Task.FromResult(new PipeResponse
            {
                Success = false,
                Error = $"Unknown command: {request.Command}"
            })
        };
    }

    private Task<PipeResponse> UpdateAsync(AppConfigDto dto)
    {
        _configService.Update(dto);

        var result = GetCurrentConfig();

        return Task.FromResult(new PipeResponse
        {
            Success = true,
            Payload = result
        });
    }

    private AppConfigDto GetCurrentConfig()
    {
        var dto = _configService.Get();
        dto.ServiceStatus = _serviceStatusReader.ReadServiceStatus("Monitor_zakupki");
        return dto;
    }
}