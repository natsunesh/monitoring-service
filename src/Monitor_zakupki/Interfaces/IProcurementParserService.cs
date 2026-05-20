using Monitor_zakupki.Models;

namespace Monitor_zakupki.Interfaces
{
    public interface IProcurementParserService
    {
        Task<List<ProcurementItem>> GetNewProcurementsAsync(CancellationToken cancellationToken = default);
    }
}