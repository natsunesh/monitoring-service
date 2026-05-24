namespace Monitor_zakupki.Models
{
    public record ProcurementItem(
        string Number,
        string INN,
        string Name,
        DateTime Date);
}