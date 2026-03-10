using System;
using System.Text;

namespace DataMasking.Utils
{
    public class MD5Hash
    {
        // Các hằng số cho MD5
        private static readonly uint[] T = new uint[64];
        private static readonly int[] S = {
            7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22,
            5,  9, 14, 20, 5,  9, 14, 20, 5,  9, 14, 20, 5,  9, 14, 20,
            4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23,
            6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21
        };

        static MD5Hash()
        {
            // Khởi tạo bảng T
            for (int i = 0; i < 64; i++)
            {
                T[i] = (uint)(Math.Abs(Math.Sin(i + 1)) * Math.Pow(2, 32));
            }
        }

        public static string ComputeHash(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] paddedMessage = PadMessage(inputBytes);
            
            // Khởi tạo buffer MD5
            uint A = 0x67452301;
            uint B = 0xEFCDAB89;
            uint C = 0x98BADCFE;
            uint D = 0x10325476;

            // Xử lý từng block 512-bit
            for (int i = 0; i < paddedMessage.Length; i += 64)
            {
                uint[] M = new uint[16];
                for (int j = 0; j < 16; j++)
                {
                    M[j] = BitConverter.ToUInt32(paddedMessage, i + j * 4);
                }

                uint AA = A, BB = B, CC = C, DD = D;

                // 64 vòng lặp
                for (int j = 0; j < 64; j++)
                {
                    uint F, g;
                    
                    if (j < 16)
                    {
                        F = (B & C) | (~B & D);
                        g = (uint)j;
                    }
                    else if (j < 32)
                    {
                        F = (D & B) | (~D & C);
                        g = (uint)((5 * j + 1) % 16);
                    }
                    else if (j < 48)
                    {
                        F = B ^ C ^ D;
                        g = (uint)((3 * j + 5) % 16);
                    }
                    else
                    {
                        F = C ^ (B | ~D);
                        g = (uint)((7 * j) % 16);
                    }

                    uint temp = D;
                    D = C;
                    C = B;
                    B = B + LeftRotate(A + F + T[j] + M[g], S[j]);
                    A = temp;
                }

                A += AA;
                B += BB;
                C += CC;
                D += DD;
            }

            // Chuyển kết quả thành chuỗi hex
            byte[] result = new byte[16];
            Array.Copy(BitConverter.GetBytes(A), 0, result, 0, 4);
            Array.Copy(BitConverter.GetBytes(B), 0, result, 4, 4);
            Array.Copy(BitConverter.GetBytes(C), 0, result, 8, 4);
            Array.Copy(BitConverter.GetBytes(D), 0, result, 12, 4);

            return BitConverter.ToString(result).Replace("-", "").ToLower();
        }

        private static byte[] PadMessage(byte[] message)
        {
            long originalLength = message.Length;
            long bitLength = originalLength * 8;
            
            // Tính độ dài sau khi padding
            int paddingLength = (int)((448 - (bitLength + 1) % 512 + 512) % 512);
            int totalLength = (int)(originalLength + 1 + paddingLength / 8 + 8);
            
            byte[] paddedMessage = new byte[totalLength];
            Array.Copy(message, paddedMessage, originalLength);
            
            // Thêm bit 1 (0x80)
            paddedMessage[originalLength] = 0x80;
            
            // Thêm độ dài ban đầu (64-bit little-endian)
            byte[] lengthBytes = BitConverter.GetBytes(bitLength);
            Array.Copy(lengthBytes, 0, paddedMessage, totalLength - 8, 8);
            
            return paddedMessage;
        }

        private static uint LeftRotate(uint value, int shift)
        {
            return (value << shift) | (value >> (32 - shift));
        }
    }
}
