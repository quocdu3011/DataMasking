using System;
using System.Numerics;

namespace DataMasking.Utils
{
    // Thư viện toán học cho số lớn - hỗ trợ RSA
    internal class BigMath
    {
        private static Random random = new Random();

        // Kiểm tra số nguyên tố bằng Miller-Rabin
        public static bool IsPrime(BigInteger n, int k = 10)
        {
            if (n < 2) return false;
            if (n == 2 || n == 3) return true;
            if (n % 2 == 0) return false;

            BigInteger d = n - 1;
            int r = 0;
            while (d % 2 == 0)
            {
                d /= 2;
                r++;
            }

            for (int i = 0; i < k; i++)
            {
                BigInteger a = RandomBigInteger(2, n - 2);
                BigInteger x = ModPow(a, d, n);

                if (x == 1 || x == n - 1) continue;

                bool continueLoop = false;
                for (int j = 0; j < r - 1; j++)
                {
                    x = ModPow(x, 2, n);
                    if (x == n - 1)
                    {
                        continueLoop = true;
                        break;
                    }
                }
                if (continueLoop) continue;
                return false;
            }
            return true;
        }

        // Tạo số nguyên tố ngẫu nhiên
        public static BigInteger GeneratePrime(int bits)
        {
            while (true)
            {
                BigInteger candidate = RandomBigInteger(bits);
                candidate |= (BigInteger.One << (bits - 1)) | BigInteger.One;
                if (IsPrime(candidate))
                    return candidate;
            }
        }

        // Tạo số BigInteger ngẫu nhiên
        public static BigInteger RandomBigInteger(int bits)
        {
            byte[] bytes = new byte[bits / 8 + 1];
            random.NextBytes(bytes);
            bytes[bytes.Length - 1] &= 0x7F;
            return new BigInteger(bytes);
        }

        public static BigInteger RandomBigInteger(BigInteger min, BigInteger max)
        {
            if (min > max) throw new ArgumentException("min must be <= max");
            BigInteger range = max - min;
            int bytes = range.ToByteArray().Length;
            BigInteger result;
            do
            {
                byte[] buffer = new byte[bytes];
                random.NextBytes(buffer);
                buffer[buffer.Length - 1] &= 0x7F;
                result = new BigInteger(buffer);
            } while (result > range);
            return result + min;
        }

        // Lũy thừa modulo: (base^exp) % mod
        public static BigInteger ModPow(BigInteger baseNum, BigInteger exp, BigInteger mod)
        {
            BigInteger result = 1;
            baseNum %= mod;
            while (exp > 0)
            {
                if ((exp & 1) == 1)
                    result = (result * baseNum) % mod;
                baseNum = (baseNum * baseNum) % mod;
                exp >>= 1;
            }
            return result;
        }

        // Thuật toán Euclid mở rộng - tìm nghịch đảo modulo
        public static BigInteger ModInverse(BigInteger a, BigInteger m)
        {
            BigInteger m0 = m, x0 = 0, x1 = 1;
            if (m == 1) return 0;

            while (a > 1)
            {
                BigInteger q = a / m;
                BigInteger t = m;
                m = a % m;
                a = t;
                t = x0;
                x0 = x1 - q * x0;
                x1 = t;
            }

            if (x1 < 0) x1 += m0;
            return x1;
        }

        // GCD - Ước chung lớn nhất
        public static BigInteger GCD(BigInteger a, BigInteger b)
        {
            while (b != 0)
            {
                BigInteger temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }
    }
}
