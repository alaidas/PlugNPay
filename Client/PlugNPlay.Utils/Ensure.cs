using System;

namespace PlugNPay.Utils
{
    public static class Ensure
    {
        public static void NotNull(string value, string paramName)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(paramName);
        }

        public static void NotNull(object value, string paramName)
        {
            if (value == null)
                throw new ArgumentNullException(paramName);
        }

        public static void Positive(double value, string paramName)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(paramName);
        }
    }
}
