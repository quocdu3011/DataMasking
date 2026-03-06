using System;
using System.Text;
using System.Text.Json;
using DataMasking.Crypto;
using DataMasking.Key;
using DataMasking.Database;
using DataMasking.Masking;

namespace DataMasking.Network
{
    // Mô phỏng Server - Nhận và giải mã dữ liệu
    public class ServerService
    {
        private RSAKeyPair serverKeyPair;
        private DatabaseManager dbManager;
        private MaskingService maskingService;

        public ServerService(RSAKeyPair serverKeyPair, DatabaseManager dbManager)
        {
            this.serverKeyPair = serverKeyPair;
            this.dbManager = dbManager;
            this.maskingService = new MaskingService();
        }

        // Server nhận và xử lý request
        public ServerResponse ProcessSecureRequest(TransmissionPacket packet)
        {
            TransmissionLogger.LogServerReceive(packet.EncryptedData, packet.EncryptedAESKey);
            
            try
            {
                // Bước 1: Giải mã AES key bằng RSA Private Key
                TransmissionLogger.Log("SERVER: Đang giải mã AES key bằng RSA Private Key...");
                RSA rsa = new RSA(serverKeyPair.N, serverKeyPair.E, serverKeyPair.D);
                byte[] encryptedKeyBytes = Convert.FromBase64String(packet.EncryptedAESKey);
                byte[] aesKeyBytes = rsa.Decrypt(encryptedKeyBytes);
                
                TransmissionLogger.Log($"SERVER: Đã giải mã AES key (length: {aesKeyBytes.Length} bytes)");
                
                // Đảm bảo key đúng kích thước 32 bytes
                byte[] aesKey = new byte[32];
                if (aesKeyBytes.Length >= 32)
                {
                    Array.Copy(aesKeyBytes, aesKey, 32);
                }
                else
                {
                    // Nếu key ngắn hơn, copy toàn bộ và pad với 0
                    Array.Copy(aesKeyBytes, aesKey, aesKeyBytes.Length);
                }
                
                // Bước 2: Giải mã dữ liệu bằng AES key
                TransmissionLogger.Log("SERVER: Đang giải mã dữ liệu bằng AES-256...");
                byte[] encryptedDataBytes = Convert.FromBase64String(packet.EncryptedData);
                byte[] ivBytes = Convert.FromBase64String(packet.IV);
                
                TransmissionLogger.Log($"SERVER: Encrypted data size: {encryptedDataBytes.Length} bytes, IV size: {ivBytes.Length} bytes");
                
                AES aes = new AES(aesKey);
                byte[] decryptedBytes = aes.DecryptCBC(encryptedDataBytes, ivBytes);
                
                TransmissionLogger.Log($"SERVER: Decrypted bytes length: {decryptedBytes.Length}");
                
                // Kiểm tra dữ liệu giải mã có hợp lệ không
                string decryptedJson = Encoding.UTF8.GetString(decryptedBytes);
                
                // Log an toàn (tránh ký tự lỗi)
                string safeLog = decryptedJson.Length > 150 ? decryptedJson.Substring(0, 150) + "..." : decryptedJson;
                TransmissionLogger.LogServerDecrypt(safeLog);
                
                // Bước 3: Parse JSON
                var data = JsonSerializer.Deserialize<SensitiveDataModel>(decryptedJson);
                
                // Bước 4: Lưu vào database
                TransmissionLogger.Log("SERVER: Đang lưu dữ liệu vào database...");
                int id = dbManager.InsertSensitiveData(
                    data.fullName, data.email, data.phone,
                    data.creditCard, data.ssn, data.address
                );
                TransmissionLogger.Log($"SERVER: Đã lưu vào database với ID: {id}");
                
                // Bước 5: Tạo response với dữ liệu masked
                ServerResponse response = new ServerResponse
                {
                    Success = true,
                    RecordId = id,
                    MaskedData = new MaskedDataModel
                    {
                        FullName = maskingService.MaskName(data.fullName),
                        Email = maskingService.MaskEmail(data.email),
                        Phone = maskingService.MaskPhone(data.phone),
                        CreditCard = maskingService.MaskCreditCard(data.creditCard),
                        SSN = maskingService.MaskSSN(data.ssn),
                        Address = maskingService.MaskAddress(data.address)
                    },
                    Message = "Dữ liệu đã được lưu thành công"
                };
                
                // Log response
                string maskedJson = JsonSerializer.Serialize(response.MaskedData, new JsonSerializerOptions { WriteIndented = true });
                TransmissionLogger.LogServerResponse(maskedJson);
                
                return response;
            }
            catch (Exception ex)
            {
                TransmissionLogger.Log($"SERVER ERROR: {ex.Message}");
                return new ServerResponse
                {
                    Success = false,
                    Message = "Lỗi xử lý dữ liệu: " + ex.Message
                };
            }
        }
    }

    // Model dữ liệu nhạy cảm
    public class SensitiveDataModel
    {
        public string fullName { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string creditCard { get; set; }
        public string ssn { get; set; }
        public string address { get; set; }
    }

    // Model dữ liệu đã che giấu
    public class MaskedDataModel
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string CreditCard { get; set; }
        public string SSN { get; set; }
        public string Address { get; set; }
    }

    // Response từ server
    public class ServerResponse
    {
        public bool Success { get; set; }
        public int RecordId { get; set; }
        public MaskedDataModel MaskedData { get; set; }
        public string Message { get; set; }
    }
}
