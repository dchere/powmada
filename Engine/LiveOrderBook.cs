using System.Runtime.CompilerServices;

namespace Powmada.Engine
{
    public sealed class LiveOrderBook
    {
        // Maximum depth of bids/asks to track
        private const int MaxDepth = 128; // by 8 bytes = 1KB per side, fits in L1 cache

        // Primitive parallel arrays to track order book sides
        private readonly long[] _bidPrices = new long[MaxDepth];
        private readonly int[] _bidQuantities = new int[MaxDepth];
        private readonly long[] _bidOrderIds = new long[MaxDepth];

        private readonly long[] _askPrices = new long[MaxDepth];
        private readonly int[] _askQuantities = new int[MaxDepth];
        private readonly long[] _askOrderIds = new long[MaxDepth];

        private int _bidCount = 0;
        private int _askCount = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ref readonly MarketEvent ev)
        {
            // we trust blindly our parser, so compiler allowed to make a JumpTable
            switch (ev.Action)
            {
                case MarketAction.Insert:
                    InsertOrder(in ev);
                    break;
                case MarketAction.Update:
                    UpdateOrder(in ev);
                    break;
                case MarketAction.Delete:
                    DeleteOrder(in ev);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InsertOrder(ref readonly MarketEvent ev)
        {
            bool isBid = ev.Side == MarketSide.Bid;
            int ordersCount = isBid ? _bidCount : _askCount;
            long[] prices = isBid ? _bidPrices : _askPrices;

            // Find insertion index based on price priority
            int index = 0;
            if (isBid)
            {
                while (index < ordersCount && prices[index] >= ev.Price) index++;
            }
            else
            {
                while (index < ordersCount && prices[index] <= ev.Price) index++;
            }

            // Guard: order outside of our tracking depth
            if (index >= MaxDepth) return;

            int[] quantities = isBid ? _bidQuantities : _askQuantities;
            long[] orderIds = isBid ? _bidOrderIds : _askOrderIds;

            // Shift elements to make room for new order
            int countToShift = ordersCount - index;
            if (countToShift > 0 && (index + 1) < MaxDepth)
            {
                int maxSafeShift = Math.Min(countToShift, MaxDepth - index - 1);
                Array.Copy(prices, index, prices, index + 1, maxSafeShift);
                Array.Copy(quantities, index, quantities, index + 1, maxSafeShift);
                Array.Copy(orderIds, index, orderIds, index + 1, maxSafeShift);
            }

            // Write the values
            prices[index] = ev.Price;
            quantities[index] = ev.Quantity;
            orderIds[index] = ev.OrderId;

            // Increment matching counter if book not full
            if (ordersCount < MaxDepth)
            {
                if (isBid) _bidCount++; else _askCount++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateOrder(ref readonly MarketEvent ev)
        {
            bool isBid = ev.Side == MarketSide.Bid;
            int ordersCount = isBid ? _bidCount : _askCount;
            long[] orderIds = isBid ? _bidOrderIds : _askOrderIds;

            // Scan for the unique Order ID slot
            int targetIdx = -1;
            for (int i = 0; i < ordersCount; i++)
            {
                if (orderIds[i] == ev.OrderId)
                {
                    targetIdx = i;
                    break;
                }
            }

            // Guard: Order outside of our tracking depth
            if (targetIdx == -1) return;

            // Update quantity field directly inside array layout
            int[] quantities = isBid ? _bidQuantities : _askQuantities;
            quantities[targetIdx] = ev.Quantity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DeleteOrder(ref readonly MarketEvent ev)
        {
            bool isBid = ev.Side == MarketSide.Bid;
            int ordersCount = isBid ? _bidCount : _askCount;
            long[] orderIds = isBid ? _bidOrderIds : _askOrderIds;

            // Scan for the unique Order ID slot
            int targetIdx = -1;
            for (int i = 0; i < ordersCount; i++)
            {
                if (orderIds[i] == ev.OrderId)
                {
                    targetIdx = i;
                    break;
                }
            }

            // Guard: Order outside of our tracking depth
            if (targetIdx == -1) return;

            // Select remaining matching target structures
            long[] prices = isBid ? _bidPrices : _askPrices;
            int[] quantities = isBid ? _bidQuantities : _askQuantities;

            // Shift trailing elements forward by 1 slot
            int countToShift = ordersCount - 1 - targetIdx;
            if (countToShift > 0)
            {
                Array.Copy(prices, targetIdx + 1, prices, targetIdx, countToShift);
                Array.Copy(quantities, targetIdx + 1, quantities, targetIdx, countToShift);
                Array.Copy(orderIds, targetIdx + 1, orderIds, targetIdx, countToShift);
            }

            // memory hygiene
            targetIdx = ordersCount - 1;
            prices[targetIdx] = 0;
            quantities[targetIdx] = 0;
            orderIds[targetIdx] = 0;

            // Decrement the track counter
            if (isBid) _bidCount--; else _askCount--;
        }

        public void PrintBookView(int levelsToPrint)
        {
            Console.WriteLine("--------------------------------- Asks orders  ---------------------------------");
            int asksToShow = Math.Min(_askCount, levelsToPrint);
            for (int i = asksToShow - 1; i >= 0; i--)
            {
                double realPrice = _askPrices[i] / 10_000_000.0;
                Console.WriteLine($"ID: {_askOrderIds[i]} asks {realPrice:F2} EUR/MWh for {_askQuantities[i]} MW");
            }
            if (asksToShow == 0) Console.WriteLine("(No Ask liquidity)");

            Console.WriteLine("--------------------------------- Bids orders  ---------------------------------");
            int bidsToShow = Math.Min(_bidCount, levelsToPrint);
            for (int i = 0; i < bidsToShow; i++)
            {
                double realPrice = _bidPrices[i] / 10_000_000.0;
                Console.WriteLine($"ID: {_bidOrderIds[i]} bids {realPrice:F2} EUR/MWh for {_bidQuantities[i]} MW");
            }
            if (bidsToShow == 0) Console.WriteLine("(No Bid liquidity)");

            Console.WriteLine("--------------------------------------------------------------------------------\n");
        }
    }
}