using System.Globalization;
using Powmada.Engine;
using System.Runtime.CompilerServices;

namespace Powmada.Ingestion
{
    public sealed class CsvStreamReader(string filePath)
    {
        private readonly string _filePath = filePath;

        public IEnumerable<MarketEvent> ParseEvents()
        {
            using var reader = new StreamReader(_filePath);

            // Skip the header row (Timestamp;Price;Quantity;Side;Action;OrderId)
            string? header = reader.ReadLine();
            if (header == null) yield break;

            while (reader.ReadLine() is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                yield return ParseRow(line.AsSpan());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetTokenLength(ReadOnlySpan<char> line, int startIdx)
        {
            return line.Slice(startIdx).IndexOf(';');
        }

        private static MarketEvent ParseRow(ReadOnlySpan<char> line)
        {
            int startIdx = 0;
            int tokenLength;

            /*
             * 1. Timestamp
             * DateTime YYYY-MM-DD HH:mm:ss.fff -> unix milliseconds
             */
            tokenLength = GetTokenLength(line, startIdx);
            ReadOnlySpan<char> timeSpan = line.Slice(startIdx, tokenLength);
            long timestampMs = DateTimeOffset.ParseExact(
                timeSpan,
                "yyyy-MM-dd HH:mm:ss.fff",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal
            ).ToUnixTimeMilliseconds();
            startIdx += tokenLength + 1;

            /*
             * 2. Price
             * Convert real price (double) to 10^7 scaled long integer
             * We assume that the price is always formatted string with '.' separator and 7 digits after it
             */
            tokenLength = GetTokenLength(line, startIdx);
            ReadOnlySpan<char> priceSpan = line.Slice(startIdx, tokenLength);
            startIdx += tokenLength + 1;

            // integer part of the price
            int dotIdx = priceSpan.IndexOf('.');
            // decimal separator present and there is a character before it
            if (dotIdx < 1)
            {
                throw new FormatException($"Wrong price format: '{priceSpan.ToString()}'. Missing or misplaced decimal point.");
            }
            ReadOnlySpan<char> integerSpan = priceSpan.Slice(0, dotIdx);

            // fractional part of the price
            ReadOnlySpan<char> fractionalSpan = priceSpan.Slice(dotIdx + 1);
            if (fractionalSpan.Length != 7)
            {
                throw new FormatException($"Wrong price format: '{priceSpan.ToString()}'. Expected exactly 7 decimal digits after the point.");
            }

            long scaledPrice = long.Parse(integerSpan, CultureInfo.InvariantCulture) * 10_000_000 +
                long.Parse(fractionalSpan, CultureInfo.InvariantCulture);

            /* 
             * 3. Quantity
             */
            tokenLength = GetTokenLength(line, startIdx);
            int quantity = int.Parse(line.Slice(startIdx, tokenLength));
            startIdx += tokenLength + 1;

            /*
             * 4. Side (Bid / Ask)
             */
            tokenLength = GetTokenLength(line, startIdx);
            MarketSide side = MarketEvent.ParseSide(line.Slice(startIdx, tokenLength));
            startIdx += tokenLength + 1;

            /*
             * 5. Action (Insert / Update / Delete)
             */
            tokenLength = GetTokenLength(line, startIdx);
            MarketAction action = MarketEvent.ParseAction(line.Slice(startIdx, tokenLength));
            startIdx += tokenLength + 1;

            /*
             * 6. OrderId (Remaining line slice buffer window)
             */
            ReadOnlySpan<char> idSpan = line.Slice(startIdx);
            long orderId = long.Parse(idSpan);

            return new MarketEvent
            {
                OrderId = orderId,
                TimestampMs = timestampMs,
                Price = scaledPrice,
                Quantity = quantity,
                Side = side,
                Action = action
            };
        }
    }
}