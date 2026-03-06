using System;
using System.Text;
using DataMasking.Key;
using DataMasking.Masking;

namespace DataMasking.Crypto
{
    // Class demo để test mã hóa
    public class CryptoDemo
    {
        public static void RunDemo()
        {
            Console.WriteLine("=== DEMO MÃ HÓA RSA VÀ AES ===\n");

            MaskingService service = new MaskingService();

            // Test AES
            Console.WriteLine("--- TEST AES-256 ---");
            string plaintext = "Đây là dữ liệu cần mã hóa bằng AES!";
            Console.WriteLine($"Plaintext: {plaintext}");

            byte[] aesEncrypted = service.EncryptAES(plaintext);
            Console.WriteLine($"Encrypted (Base64): {Convert.ToBase64String(aesEncrypted)}");

            string aesDecrypted = service.DecryptAES(aesEncrypted);
            Console.WriteLine($"Decrypted: {aesDecrypted}");
            Console.WriteLine($"Kết quả: {(plaintext == aesDecrypted ? "THÀNH CÔNG ✓" : "THẤT BẠI ✗")}\n");

            // Test RSA
            Console.WriteLine("--- TEST RSA-1024 ---");
            string rsaPlaintext = "Dữ liệu RSA";
            Console.WriteLine($"Plaintext: {rsaPlaintext}");

            byte[] rsaEncrypted = service.EncryptRSA(rsaPlaintext);
            Console.WriteLine($"Encrypted length: {rsaEncrypted.Length} bytes");

            string rsaDecrypted = service.DecryptRSA(rsaEncrypted);
            Console.WriteLine($"Decrypted: {rsaDecrypted}");
            Console.WriteLine($"Kết quả: {(rsaPlaintext == rsaDecrypted ? "THÀNH CÔNG ✓" : "THẤT BẠI ✗")}\n");

            // Test Hybrid
            Console.WriteLine("--- TEST HYBRID (RSA + AES) ---");
            string hybridPlaintext = "Đây là dữ liệu lớn được mã hóa bằng phương pháp hybrid: RSA mã hóa key, AES mã hóa dữ liệu!";
            Console.WriteLine($"Plaintext: {hybridPlaintext}");

            var (encData, encKey, iv) = service.EncryptHybrid(hybridPlaintext);
            Console.WriteLine($"Encrypted data length: {encData.Length} bytes");
            Console.WriteLine($"Encrypted key length: {encKey.Length} bytes");

            string hybridDecrypted = service.DecryptHybrid(encData, encKey, iv);
            Console.WriteLine($"Decrypted: {hybridDecrypted}");
            Console.WriteLine($"Kết quả: {(hybridPlaintext == hybridDecrypted ? "THÀNH CÔNG ✓" : "THẤT BẠI ✗")}\n");

            // Hiển thị keys
            Console.WriteLine("--- THÔNG TIN KHÓA ---");
            Console.WriteLine("RSA Public Key:");
            Console.WriteLine(service.GetRSAPublicKey());
            Console.WriteLine("\nAES Key:");
            Console.WriteLine(service.GetAESKey());
        }
    }
}
