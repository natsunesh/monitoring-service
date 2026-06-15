namespace Contracts
{
    public sealed class PipeResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public AppConfigDto? Payload { get; set; }
    }
}