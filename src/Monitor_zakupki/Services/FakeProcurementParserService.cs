using Monitor_zakupki.Interfaces;
using Monitor_zakupki.Models;

namespace Monitor_zakupki.Services
{
    public class FakeProcurementParserService : IProcurementParserService
    {
        public Task<List<ProcurementItem>> GetNewProcurementsAsync(CancellationToken cancellationToken = default)
        {
            var items = new List<ProcurementItem>
            {
                new("123-001", "7728168971", "Поставка офисной бумаги", DateTime.Now.Date),
                new("123-002", "7736050003", "Поставка картриджей", DateTime.Now.Date)
            };

            return Task.FromResult(items);
        }
    }
}