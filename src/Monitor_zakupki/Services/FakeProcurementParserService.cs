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
                new("123-001", "Поставка офисной бумаги", DateTime.Now.Date, "https://example.com/1", "new"),
                new("123-002", "Поставка картриджей", DateTime.Now.Date, "https://example.com/2", "new")
            };

            return Task.FromResult(items);
        }
    }
}