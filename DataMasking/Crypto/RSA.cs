using System;
using System.Numerics;
using System.Text;
using DataMasking.Utils;

namespace DataMasking.Crypto
{
    // Triển khai RSA từ đầu
    public class RSA
    {
        private BigInteger n;
        private BigInteger e;
        private BigInteger d;

        // Constructor với public key
        public RSA(BigInteger n, BigInteger e)
        {
            this.n = n;
            this.e = e;
        }

        // Constructor với cả public và private key
        public RSA(BigInteger n, BigInteger e, BigInteger d)
        {
            this.n = n;
            this.e = e;
            this.d = d;
        }

        // Mã hóa
        public byte[] Encrypt(byte[] data)
        {
            // Đảm bảo data không quá lớn
            int maxDataSize = (n.ToByteArray().Length) - 1;
            if (data.Length > maxDataSize)
                throw new ArgumentException($"Dữ liệu quá lớn. Max: {maxDataSize} bytes");
            
            // Thêm byte 0 vào cuối để đảm bảo số dương
            byte[] paddedData = new byte[data.Length + 1];
            Array.Copy(data, paddedData, data.Length);
            paddedData[data.Length] = 0;
            
            BigInteger message = new BigInteger(paddedData);
            
            // Kiểm tra message < n
            if (message >= n)
                throw new ArgumentException("Dữ liệu quá lớn để mã hóa với key này");
            
            BigInteger encrypted = BigMath.ModPow(message, e, n);
            return encrypted.ToByteArray();
        }

        // Giải mã
        public byte[] Decrypt(byte[] data)
        {
            if (d == 0)
                throw new InvalidOperationException("Không có private key để giải mã");

            BigInteger encrypted = new BigInteger(data);
            
            // Đảm bảo encrypted là số dương
            if (encrypted < 0)
                encrypted = BigInteger.Abs(encrypted);
            
            BigInteger decrypted = BigMath.ModPow(encrypted, d, n);
            byte[] result = decrypted.ToByteArray();
            
            // Loại bỏ byte padding cuối cùng (0 byte)
            if (result.Length > 0 && result[result.Length - 1] == 0)
            {
                byte[] trimmed = new byte[result.Length - 1];
                Array.Copy(result, trimmed, trimmed.Length);
                return trimmed;
            }
            
            return result;
        }

        // Mã hóa chuỗi
        public string EncryptString(string plaintext)
        {
            byte[] data = Encoding.UTF8.GetBytes(plaintext);
            byte[] encrypted = Encrypt(data);
            return Convert.ToBase64String(encrypted);
        }

        // Giải mã chuỗi
        public string DecryptString(string ciphertext)
        {
            byte[] data = Convert.FromBase64String(ciphertext);
            byte[] decrypted = Decrypt(data);
            return Encoding.UTF8.GetString(decrypted);
        }

        // Mã hóa dữ liệu lớn (chia nhỏ thành các block)
        public byte[] EncryptLarge(byte[] data)
        {
            int blockSize = (n.ToByteArray().Length) - 11; // PKCS#1 padding
            if (blockSize <= 0) blockSize = 100;

            int numBlocks = (data.Length + blockSize - 1) / blockSize;
            byte[][] encryptedBlocks = new byte[numBlocks][];

            for (int i = 0; i < numBlocks; i++)
            {
                int offset = i * blockSize;
                int length = Math.Min(blockSize, data.Length - offset);
                
                byte[] block = new byte[length];
                Array.Copy(data, offset, block, 0, length);
                
                encryptedBlocks[i] = Encrypt(block);
            }

            // Ghép các block lại
            int totalLength = 0;
            foreach (var block in encryptedBlocks)
                totalLength += block.Length + 4; // 4 bytes cho length

            byte[] result = new byte[totalLength];
            int position = 0;

            foreach (var block in encryptedBlocks)
            {
                // Ghi độ dài block
                byte[] lengthBytes = BitConverter.GetBytes(block.Length);
                Array.Copy(lengthBytes, 0, result, position, 4);
                position += 4;

                // Ghi block
                Array.Copy(block, 0, result, position, block.Length);
                position += block.Length;
            }

            return result;
        }

        // Giải mã dữ liệu lớn
        public byte[] DecryptLarge(byte[] data)
        {
            if (d == 0)
                throw new InvalidOperationException("Không có private key để giải mã");

            System.Collections.Generic.List<byte[]> decryptedBlocks = new System.Collections.Generic.List<byte[]>();
            int position = 0;

            while (position < data.Length)
            {
                // Đọc độ dài block
                byte[] lengthBytes = new byte[4];
                Array.Copy(data, position, lengthBytes, 0, 4);
                int blockLength = BitConverter.ToInt32(lengthBytes, 0);
                position += 4;

                // Đọc block
                byte[] block = new byte[blockLength];
                Array.Copy(data, position, block, 0, blockLength);
                position += blockLength;

                // Giải mã block
                byte[] decryptedBlock = Decrypt(block);
                decryptedBlocks.Add(decryptedBlock);
            }

            // Ghép các block lại
            int totalLength = 0;
            foreach (var block in decryptedBlocks)
                totalLength += block.Length;

            byte[] result = new byte[totalLength];
            position = 0;

            foreach (var block in decryptedBlocks)
            {
                Array.Copy(block, 0, result, position, block.Length);
                position += block.Length;
            }

            return result;
        }
    }
}
