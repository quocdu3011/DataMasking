using System;
using System.Text;
using System.Text.Json;
using System.Net.Sockets;
using System.Threading.Tasks;
using DataMasking.Crypto;
using DataMasking.Key;

namespace DataMasking.Network
{
    // Client - Mã hóa và gửi dữ liệu qua TCP
    public class ClientService
    {
        private RSAKeyPair serverPublicKey;
        private string serverHost;
        private int serverPort;

        public ClientService(RSAKeyPair serverPublicKey, string serverHost = "127.0.0.1", int serverPort = 8888)
        {
            this.serverPublicKey = serverPublicKey;
            this.serverHost = serverHost;
            this.serverPort = serverPort;
        }

        // Client gửi request qua TCP và nhận response
        public async Task<ServerResponse> SendSecureRequestAsync(string fullName, string email, string phone,
                                                                   string creditCard, string ssn, string address)
        {
            TcpClient client = null;
            NetworkStream stream = null;
            
            try
            {
                TransmissionLogger.Log($"CLIENT: Đang kết nối đến server {serverHost}:{serverPort}...");
                
                // Kết nối đến server
                client = new TcpClient();
                await client.ConnectAsync(serverHost, serverPort);
                stream = client.GetStream();
                
                TransmissionLogger.Log("CLIENT: Đã kết nối thành công!");
                
                // Tạo packet mã hóa
                TransmissionPacket packet = CreateSecurePacket(fullName, email, phone, creditCard, ssn, address);
                
                // Serialize packet thành JSON
                string packetJson = System.Text.Json.JsonSerializer.Serialize(packet);
                byte[] packetBytes = Encoding.UTF8.GetBytes(packetJson);
                
                // Gửi kích thước packet trước (4 bytes)
                byte[] sizeBytes = BitConverter.GetBytes(packetBytes.Length);
                await stream.WriteAsync(sizeBytes, 0, 4);
                
                // Gửi packet data
                await stream.WriteAsync(packetBytes, 0, packetBytes.Length);
                TransmissionLogger.Log($"CLIENT: Đã gửi {packetBytes.Length} bytes đến server");
                
                // Đọc response size
                byte[] responseSizeBuffer = new byte[4];
                await stream.ReadAsync(responseSizeBuffer, 0, 4);
                int responseSize = BitConverter.ToInt32(responseSizeBuffer, 0);
                
                TransmissionLogger.Log($"CLIENT: Đang nhận response ({responseSize} bytes)...");
                
                // Đọc response data
                byte[] responseBuffer = new byte[responseSize];
                int totalRead = 0;
                while (totalRead < responseSize)
                {
                    int read = await stream.ReadAsync(responseBuffer, totalRead, responseSize - totalRead);
                    if (read == 0) break;
                    totalRead += read;
                }
                
                // Deserialize response
                string responseJson = Encoding.UTF8.GetString(responseBuffer);
                ServerResponse response = System.Text.Json.JsonSerializer.Deserialize<ServerResponse>(responseJson);
                
                TransmissionLogger.Log($"CLIENT: Đã nhận response từ server");
                
                return response;
            }
            catch (Exception ex)
            {
                TransmissionLogger.Log($"CLIENT ERROR: {ex.Message}");
                return new ServerResponse
                {
                    Success = false,
                    Message = "Lỗi kết nối: " + ex.Message
                };
            }
            finally
            {
                stream?.Close();
                client?.Close();
            }
        }

        // Tạo packet mã hóa (logic cũ)
        private TransmissionPacket CreateSecurePacket(string fullName, string email, string phone,
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
