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
    }
}
