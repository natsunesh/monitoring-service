namespace Monitor_zakupki.Models
{
    public record ProcurementItem(
        string Number,
        string Inn,
        string Name,
        string Url,
        DateTime Date);
}