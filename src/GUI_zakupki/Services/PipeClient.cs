using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Contracts;
using Microsoft.Extensions.Logging;

namespace GUI_zakupki.Services;

public sealed class PipeClient : IPipeClient
{
    private const string PipeName = "monitor_zakupki_pipe";
    private const int DefaultTimeoutMs = 10000;

    private readonly ILogger<PipeClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PipeClient(ILogger<PipeClient> logger)
    {
        _logger = logger;
        _logger.LogInformation("PipeClient created");
    }

    public async Task<PipeResponse> SendAsync(PipeRequest request, int timeoutMs = DefaultTimeoutMs)
    {
        _logger.LogInformation("SendAsync started. Command={Command}, TimeoutMs={TimeoutMs}", request.Command, timeoutMs);

        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            _logger.LogInformation("Connecting to pipe...");
            await client.ConnectAsync(timeoutMs);
            _logger.LogInformation("Connected to pipe");

            var requestJson = JsonSerializer.Serialize(request, JsonOptions);
            var requestBytes = Encoding.UTF8.GetBytes(requestJson);

            _logger.LogInformation("Writing length={Length}", requestBytes.Length);
            await client.WriteAsync(BitConverter.GetBytes(requestBytes.Length));
            _logger.LogInformation("Writing payload...");
            await client.WriteAsync(requestBytes, 0, requestBytes.Length);
            await client.FlushAsync();
            _logger.LogInformation("Request sent");

            var lengthBuffer = new byte[4];
            _logger.LogInformation("Reading response length...");
            await ReadExactlyAsync(client, lengthBuffer, 4);
            var responseLength = BitConverter.ToInt32(lengthBuffer, 0);
            _logger.LogInformation("Response length={Length}", responseLength);

            var responseBuffer = new byte[responseLength];
            _logger.LogInformation("Reading response payload...");
            await ReadExactlyAsync(client, responseBuffer, responseLength);
            _logger.LogInformation("Response payload received");

            var responseJson = Encoding.UTF8.GetString(responseBuffer);
            var response = JsonSerializer.Deserialize<PipeResponse>(responseJson, JsonOptions)
                ?? new PipeResponse { Success = false, Error = "Empty response" };

            _logger.LogInformation("Response deserialized. Success={Success}, Error={Error}", response.Success, response.Error);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendAsync failed");
            return new PipeResponse { Success = false, Error = ex.Message };
        }
    }

    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, int count)
    {
        var offset = 0;
        while (offset < count)
        {
            var read = await stream.ReadAsync(buffer, offset, count - offset);
            if (read == 0)
                throw new EndOfStreamException("Pipe closed before all data was received.");

            offset += read;
        }
    }
}