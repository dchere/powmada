using Powmada.Engine;
using Powmada.Storage;

namespace Powmada.Services
{
    public sealed class HistoricalReconstructor(ClickHouseContext context)
    {
        private readonly ClickHouseContext _context = context;

        public LiveOrderBook ReconstructBookAt(long targetTimestampMs)
        {
            var historicalBook = new LiveOrderBook();
            var historicalEvents = _context.GetEventsUpToTimestamp(targetTimestampMs);

            foreach (var ev in historicalEvents)
            {
                historicalBook.Update(in ev);
            }

            return historicalBook;
        }
    }
}