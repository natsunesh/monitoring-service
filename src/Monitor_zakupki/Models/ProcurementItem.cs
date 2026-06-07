using System.Text.Json.Serialization;
namespace Monitor_zakupki.Models
{
    public record ProcurementItem(
        string Number,
        string Inn,
        string Name,
        string Url,
        string Date);

    public class SavedHTML
    {
        [JsonPropertyName("inn")]
        public string Inn { get; set; } = string.Empty;

        [JsonPropertyName("raw_html")]
        public string RawHtml { get; set; } = string.Empty;
    }
}