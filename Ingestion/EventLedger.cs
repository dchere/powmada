using ClickHouse.Client.Copy;
using Powmada.Engine;
using Powmada.Storage;

namespace Powmada.Ingestion
{
    public sealed class EventLedger(ClickHouseContext context, int batchSize)
    {
        private readonly ClickHouseContext _context = context;
        private readonly int _batchSize = batchSize;

        public async Task AppendBatchAsync(List<MarketEvent> batch)
        {
            using var connection = _context.CreateConnection();
            await connection.OpenAsync().ConfigureAwait(false);

            using var bulkCopy = new ClickHouseBulkCopy(connection)
            {
                DestinationTableName = "market_events",
                BatchSize = batch.Count
            };

            await bulkCopy.InitAsync().ConfigureAwait(false);

            var rows = new List<object[]>(_batchSize);

            foreach (var ev in batch)
            {
                rows.Add(new object[]
                {
                    (ulong)ev.OrderId,
                    (ulong)ev.TimestampMs,
                    ev.Price,
                    ev.Quantity,
                    (byte)ev.Side,
                    (byte)ev.Action
                });
            }

            await bulkCopy.WriteToServerAsync(rows).ConfigureAwait(false);
        }
    }
}