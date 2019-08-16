using System;
using System.Security.Cryptography;
using System.Threading;

namespace Core
{
    public static class R
    {
        // public static ThreadLocal<CryptoRandom> NG { get; private set; } 
        //     = new ThreadLocal<CryptoRandom>(() => new CryptoRandom());
        public static CryptoRandom NG { get; private set; } = new CryptoRandom();
    }

    public class CryptoRandom
    {
        private readonly RNGCryptoServiceProvider rng;

        public CryptoRandom()
        {
            rng = new RNGCryptoServiceProvider();
        }

        // https://stackoverflow.com/questions/2854438/how-to-generate-a-cryptographically-secure-double-between-0-and-1
        public double NextDouble()
        {
            // Step 1: fill an array with 8 random bytes
            var bytes = new byte[8];
            rng.GetBytes(bytes);
            // Step 2: bit-shift 11 and 53 based on double's mantissa bits
            var ul = BitConverter.ToUInt64(bytes, 0) >> 11;
            double d = ul / (double)(1UL << 53);

            return d;
        }

        public int Next(int maxValue)
        {
            return Next(0, maxValue);
        }

        public int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue) throw new ArgumentException();
            int d = maxValue - minValue;

            return (int)Math.Floor(NextDouble() * d + minValue);
        }
    }
}
