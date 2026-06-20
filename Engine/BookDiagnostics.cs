using Powmada.Console;

namespace Powmada.Engine
{
    internal static class BookDiagnostics
    {
        /// <summary>
        /// Changing of a price on an existing order is not allowed without changing of the priority.
        /// Should be handled as Delete/Insert pair. This method logs such invalid events for further analysis.
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="originalPrice"></param>
        public static void LogInvalidOrderUpdatePrice(in MarketEvent ev, long originalPrice)
        {
            ConsoleOutput.WriteWarning(
                $"Invalid Update event: orderId={ev.OrderId} side={ev.Side} action={ev.Action} " +
                $"timestampMs={ev.TimestampMs} storedPrice={PriceScale.FormatedPrice(originalPrice)} " +
                $"eventPrice={PriceScale.FormatedPrice(ev.Price)}");
        }

        /// <summary>
        /// Increasing the quantity of an existing order beyond its original size is not allowed.
        /// Should be handled as Delete/Insert pair. This method logs such invalid events for further analysis.
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="originalQuantity"></param>
        public static void LogInvalidOrderUpdateUpsizeQuantity(in MarketEvent ev, int originalQuantity)
        {
            ConsoleOutput.WriteWarning(
                $"Invalid quantity upsize on Update: orderId={ev.OrderId} side={ev.Side} action={ev.Action} " +
                $"timestampMs={ev.TimestampMs} storedQuantity={originalQuantity} eventQuantity={ev.Quantity}");
        }

        /// <summary>
        /// Insert would exceed the MaxDepth (128) tracked depth. The order is not inserted.
        /// </summary>
        /// <param name="ev"></param>
        public static void LogDepthOverflowOnInsert(in MarketEvent ev)
        {
            ConsoleOutput.WriteWarning(
                $"Insert exceeded MaxDepth (128) tracked depth: orderId={ev.OrderId} side={ev.Side} action={ev.Action} " +
                $"timestampMs={ev.TimestampMs} price={PriceScale.FormatedPrice(ev.Price)} quantity={ev.Quantity}");
        }

        /// <summary>
        /// Update/Delete cannot find orderId in top-128; order outside tracked depth
        /// (may be due to earlier overflow or never inserted).
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="action"></param>
        public static void LogOrderNotInTrackedDepth(in MarketEvent ev, MarketAction action)
        {
            ConsoleOutput.WriteWarning(
                $"Update/Delete cannot find orderId in top-128; order outside tracked depth " +
                $"(may be due to earlier overflow or never inserted): orderId={ev.OrderId} side={ev.Side} action={action} " +
                $"timestampMs={ev.TimestampMs} price={PriceScale.FormatedPrice(ev.Price)} quantity={ev.Quantity}");
        }
    }
}
