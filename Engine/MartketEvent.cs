using System.Runtime.CompilerServices;

namespace Powmada.Engine
{
    public enum MarketAction : byte
    {
        Insert = 0,
        Update = 1,
        Delete = 2
    }

    public enum MarketSide : byte
    {
        Bid = 0,
        Ask = 1
    }

    public struct MarketEvent
    {
        public long OrderId;
        public long TimestampMs;
        public long Price;
        public int Quantity;
        public MarketSide Side;
        public MarketAction Action;

        /// <summary>
        /// Strictly parses a text slice into a MarketSide enum.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MarketSide ParseSide(ReadOnlySpan<char> span)
        {
            if (span.Equals("bid".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return MarketSide.Bid;
            }
            if (span.Equals("ask".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return MarketSide.Ask;
            }

            throw new FormatException($"Invalid market side data value: '{span.ToString()}'. Expected 'Bid' or 'Ask'.");
        }

        /// <summary>
        /// Strictly parses a text slice into a MarketAction enum.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MarketAction ParseAction(ReadOnlySpan<char> span)
        {
            if (span.Equals("insert".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return MarketAction.Insert;
            }
            if (span.Equals("update".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return MarketAction.Update;
            }
            if (span.Equals("delete".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return MarketAction.Delete;
            }

            throw new FormatException($"Invalid market action data value: '{span.ToString()}'. Expected 'Insert', 'Update', or 'Delete'.");
        }
    }
}