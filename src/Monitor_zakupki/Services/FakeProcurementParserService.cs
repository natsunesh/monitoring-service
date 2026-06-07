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
                new("123-001","7728168971","Поставка офисной бумаги","https://example.com/1","2026-06-07"),
            };

            return Task.FromResult(items);
        }
    }
}