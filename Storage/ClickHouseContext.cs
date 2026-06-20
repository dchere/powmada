using System.Runtime.CompilerServices;
using ClickHouse.Client.ADO;
using ClickHouse.Client.ADO.Parameters;

namespace Powmada.Storage
{
    public sealed class ClickHouseContext(string connectionString)
    {
        private readonly string _connectionString = connectionString;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ClickHouseConnection CreateConnection()
        {
            return new ClickHouseConnection(_connectionString);
        }

        public void InitializeDatabaseSchema()
        {
            using var connection = CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS market_events (
                    order_id UInt64,
                    timestamp_ms UInt64,
                    price Int64,
                    quantity Int32,
                    side UInt8,
                    action UInt8
                ) ENGINE = ReplacingMergeTree(timestamp_ms)
                ORDER BY (timestamp_ms, order_id);";

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<Engine.MarketEvent> GetEventsUpToTimestamp(long targetTimestampMs)
        {
            using var connection = CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT order_id, timestamp_ms, price, quantity, side, action 
                FROM market_events FINAL
                WHERE timestamp_ms <= {targetTime:UInt64}
                ORDER BY timestamp_ms ASC, order_id ASC;";

            var targetParam = new ClickHouseDbParameter
            {
                ParameterName = "targetTime",
                Value = (ulong)targetTimestampMs
            };
            cmd.Parameters.Add(targetParam);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return new Engine.MarketEvent
                {
                    OrderId = (long)Convert.ToUInt64(reader.GetValue(0)),
                    TimestampMs = (long)Convert.ToUInt64(reader.GetValue(1)),
                    Price = reader.GetInt64(2),
                    Quantity = reader.GetInt32(3),
                    Side = (Engine.MarketSide)reader.GetByte(4),
                    Action = (Engine.MarketAction)reader.GetByte(5)
                };
            }
        }
        public long GetTotalEventCount()
        {
            using var connection = CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT count() FROM market_events;";

            object? result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt64(result) : 0;
        }

        public (long MinTimestamp, long MaxTimestamp) GetHistoricalTimeBoundaries()
        {
            using var connection = CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT min(timestamp_ms), max(timestamp_ms) FROM market_events;";

            using var reader = cmd.ExecuteReader();
            if (reader.Read() && !reader.IsDBNull(0) && !reader.IsDBNull(1))
            {
                long min = (long)Convert.ToUInt64(reader.GetValue(0));
                long max = (long)Convert.ToUInt64(reader.GetValue(1));
                return (min, max);
            }

            return (0, 0);
        }
    }
}