namespace Contracts
{
    public sealed class PipeRequest
    {
        public string Command { get; set; } = "";
        public AppConfigDto? Payload { get; set; }
    }
}