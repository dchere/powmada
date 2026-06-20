namespace Powmada.Engine
{
    internal static class PriceScale
    {
        public const long Factor = 10_000_000;
        private const double ToRealMultiplier = 0.0000001;
        public const int DecimalDigits = 7;

        public static double ToReal(long scaledPrice) => scaledPrice * ToRealMultiplier;

        public static string FormatedPrice(long scaledPrice) =>
            $"{ToReal(scaledPrice):F2} EUR/MWh";
    }
}
