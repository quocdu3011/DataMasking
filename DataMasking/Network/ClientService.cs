using System;
using System.Text;
using DataMasking.Crypto;
using DataMasking.Key;

namespace DataMasking.Network
{
    // Mô phỏng Client - Mã hóa dữ liệu trước khi gửi
    public class ClientService
    {
        private RSAKeyPair serverPublicKey;

        public ClientService(RSAKeyPair serverPublicKey)
        {
            this.serverPublicKey = serverPublicKey;
        }

        // Client tạo request và mã hóa dữ liệu
        public TransmissionPacket CreateSecureRequest(string fullName, string email, string phone,
                                                       string creditCard, string ssn, string address)
        {
            TransmissionLogger.Log("CLIENT: Bắt đầu chuẩn bị dữ liệu để gửi...");
            
            // Tạo JSON từ dữ liệu
            string jsonData = $"{{\"fullName\":\"{fullName}\",\"email\":\"{email}\",\"phone\":\"{phone}\"," +
                             $"\"creditCard\":\"{creditCard}\",\"ssn\":\"{ssn}\",\"address\":\"{address}\"}}";
            
            TransmissionLogger.Log($"CLIENT: Dữ liệu gốc (plaintext): {jsonData}");
            
            // Bước 1: Client tạo AES key ngẫu nhiên
            AESKey aesKey = new AESKey();
            TransmissionLogger.Log($"CLIENT: Đã tạo AES key ngẫu nhiên (32 bytes)");
            TransmissionLogger.Log($"CLIENT: AES Key (Base64): {Convert.ToBase64String(aesKey.Key)}");
            
            // Bước 2: Mã hóa dữ liệu bằng AES
            AES aes = new AES(aesKey.Key);
            byte[] encryptedData = aes.EncryptCBC(System.Text.Encoding.UTF8.GetBytes(jsonData), aesKey.IV);
            TransmissionLogger.Log($"CLIENT: Đã mã hóa dữ liệu bằng AES-256 (CBC mode) - Size: {encryptedData.Length} bytes");
            
            // Bước 3: Mã hóa AES key bằng RSA Public Key của server
            RSA rsa = new RSA(serverPublicKey.N, serverPublicKey.E);
            byte[] encryptedKey = rsa.Encrypt(aesKey.Key);
            TransmissionLogger.Log($"CLIENT: Đã mã hóa AES key bằng RSA Public Key của server - Size: {encryptedKey.Length} bytes");
            
            // Tạo packet để truyền
            TransmissionPacket packet = new TransmissionPacket
            {
                EncryptedData = Convert.ToBase64String(encryptedData),
                EncryptedAESKey = Convert.ToBase64String(encryptedKey),
                IV = Convert.ToBase64String(aesKey.IV)
            };
            
            // Log dữ liệu trên kênh truyền
            TransmissionLogger.LogClientSend(packet.EncryptedData, packet.EncryptedAESKey, packet.IV);
            
            return packet;
        }
    }

    // Packet dữ liệu truyền trên kênh công khai
    public class TransmissionPacket
    {
        public string EncryptedData { get; set; }      // Dữ liệu đã mã hóa bằng AES
        public string EncryptedAESKey { get; set; }    // AES key đã mã hóa bằng RSA
        public string IV { get; set; }                 // Initialization Vector cho AES
    }
}
