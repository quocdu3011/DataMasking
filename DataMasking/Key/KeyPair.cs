using System;
using System.Numerics;
using DataMasking.Utils;

namespace DataMasking.Key
{
    // Cặp khóa RSA
    public class RSAKeyPair
    {
        public BigInteger N { get; set; }  // Modulus
        public BigInteger E { get; set; }  // Public exponent
        public BigInteger D { get; set; }  // Private exponent
        public BigInteger P { get; set; }  // Prime p
        public BigInteger Q { get; set; }  // Prime q

        public RSAKeyPair(int keySize = 1024)
        {
            GenerateKeys(keySize);
        }

        private void GenerateKeys(int keySize)
        {
            // Tạo 2 số nguyên tố p và q
            P = BigMath.GeneratePrime(keySize / 2);
            Q = BigMath.GeneratePrime(keySize / 2);

            // N = p * q
            N = P * Q;

            // φ(n) = (p-1)(q-1)
            BigInteger phi = (P - 1) * (Q - 1);

            // Chọn e (thường là 65537)
            E = 65537;
            while (BigMath.GCD(E, phi) != 1)
            {
                E += 2;
            }

            // Tính d = e^(-1) mod φ(n)
            D = BigMath.ModInverse(E, phi);
        }

        public byte[] GetPublicKey()
        {
            return System.Text.Encoding.UTF8.GetBytes($"{N}|{E}");
        }

        public string GetPublicKeyBase64()
        {
            return Convert.ToBase64String(GetPublicKey());
        }

        public byte[] GetPrivateKey()
        {
            return System.Text.Encoding.UTF8.GetBytes($"{N}|{D}");
        }

        public string GetPrivateKeyBase64()
        {
            return Convert.ToBase64String(GetPrivateKey());
        }

        // Export public key dạng PEM-like format
        public string GetPublicKeyPEM()
        {
            string base64 = GetPublicKeyBase64();
            return "-----BEGIN RSA PUBLIC KEY-----\n" +
                   FormatBase64(base64, 64) +
                   "\n-----END RSA PUBLIC KEY-----";
        }

        private string FormatBase64(string base64, int lineLength)
        {
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < base64.Length; i += lineLength)
            {
                int length = Math.Min(lineLength, base64.Length - i);
                result.AppendLine(base64.Substring(i, length));
            }
            return result.ToString().TrimEnd();
        }
    }

    // Khóa AES
    public class AESKey
    {
        public byte[] Key { get; set; }
        public byte[] IV { get; set; }

        public AESKey()
        {
            Key = new byte[32]; // AES-256
            IV = new byte[16];  // 128-bit IV
            
            Random rand = new Random();
            rand.NextBytes(Key);
            rand.NextBytes(IV);
        }

        public AESKey(byte[] key, byte[] iv)
        {
            Key = key;
            IV = iv;
        }

        public string GetKeyBase64()
        {
            return Convert.ToBase64String(Key);
        }

        public string GetIVBase64()
        {
            return Convert.ToBase64String(IV);
        }
    }
}
