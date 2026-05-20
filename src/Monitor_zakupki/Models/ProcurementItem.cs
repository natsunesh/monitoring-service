namespace Monitor_zakupki.Models
{
    public record ProcurementItem(
        string Number,
        string Description,
        DateTime Date,
        string Url,
        string Status);
}