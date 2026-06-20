using System.Diagnostics;
using System.Threading.Channels;
using Powmada.Engine;
using Powmada.Ingestion;
using Powmada.Services;
using Powmada.Storage;

namespace Powmada
{
    internal static class Program
    {
        private const int IngestionBatchSize = 100_000;
        private const int ChannelSize = 500_000;
        private const int CentralizedBookDepthToShow = 5;

        private static async Task Main(string[] args)
        {
            Console.WriteLine("========================= Powmada (Power Market Data)  =========================");

            string csvPath = args.Length > 0 ? args[0] : "test_data.csv";
            var reader = new CsvStreamReader(csvPath);
            var orderBook = new LiveOrderBook();

            var chContext = new ClickHouseContext("Host=localhost;Port=8123;Database=default");
            chContext.InitializeDatabaseSchema();

            var ledger = new EventLedger(chContext, IngestionBatchSize);

            var channelOptions = new BoundedChannelOptions(ChannelSize)
            {
                SingleWriter = true,
                SingleReader = true,
                FullMode = BoundedChannelFullMode.Wait
            };
            Channel<MarketEvent> marketDataChannel = Channel.CreateBounded<MarketEvent>(channelOptions);
            Task consumerTask = StartClickHouseConsumerAsync(marketDataChannel.Reader, ledger, IngestionBatchSize);

            long totalProcessed = 0;
            Console.WriteLine($"Starting stream processing from: {csvPath}...");
            var stopwatch = Stopwatch.StartNew();
            try
            {
                foreach (var ev in reader.ParseEvents())
                {
                    orderBook.Update(in ev);

                    if (!marketDataChannel.Writer.TryWrite(ev))
                    {
                        await marketDataChannel.Writer.WriteAsync(ev).ConfigureAwait(false);
                    }

                    totalProcessed++;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nIngestion error: {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                marketDataChannel.Writer.TryComplete();
            }

            try
            {
                await consumerTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nDB flush failure: {ex.Message}");
                Console.ResetColor();
            }

            stopwatch.Stop();
            Console.WriteLine($"Processed Events : {totalProcessed:N0}");
            Console.WriteLine($"Total Duration   : {stopwatch.Elapsed.TotalSeconds:F2} seconds");
            Console.WriteLine($"Average Velocity : {totalProcessed / stopwatch.Elapsed.TotalSeconds:F0} events/sec");

            Console.WriteLine($"Current Order Book Top {CentralizedBookDepthToShow}:");
            orderBook.PrintBookView(CentralizedBookDepthToShow);

            // Historical Order Book Reconstruction
            var (minTime, maxTime) = chContext.GetHistoricalTimeBoundaries();
            if (minTime > 0 && maxTime > minTime)
            {
                long randomTargetMs = Random.Shared.NextInt64(minTime, maxTime);
                Console.WriteLine($"Order Book Top {CentralizedBookDepthToShow} on {DateTimeOffset.FromUnixTimeMilliseconds(randomTargetMs):yyyy-MM-dd HH:mm:ss.fff}");

                var reconstructor = new HistoricalReconstructor(chContext);

                stopwatch = Stopwatch.StartNew();
                var historicalSnapshot = reconstructor.ReconstructBookAt(randomTargetMs);
                stopwatch.Stop();

                historicalSnapshot.PrintBookView(CentralizedBookDepthToShow);
                Console.WriteLine($"Historical replay assembly complete in: {stopwatch.Elapsed.TotalSeconds:F4} seconds");
            }
        }

        private static async Task StartClickHouseConsumerAsync(
            ChannelReader<MarketEvent> reader,
            EventLedger ledger,
            int batchSize)
        {
            var localBatch = new List<MarketEvent>(batchSize);

            while (await reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (reader.TryRead(out var ev))
                {
                    localBatch.Add(ev);

                    if (localBatch.Count >= batchSize)
                    {
                        var batchToSend = localBatch;
                        localBatch = new List<MarketEvent>(batchSize);
                        await ledger.AppendBatchAsync(batchToSend).ConfigureAwait(false);
                    }
                }
            }

            if (localBatch.Count > 0)
            {
                await ledger.AppendBatchAsync(localBatch).ConfigureAwait(false);
            }
        }
    }
}