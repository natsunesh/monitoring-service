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
                new("123-002","7728168971","крупная сделка","https://example.com/1","2026-06-10"),
            };

            return Task.FromResult(items);
        }
    }
}