using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using DataMasking.Crypto;
using DataMasking.Key;

namespace DataMasking.Network
{
    public class StudentClientService
    {
        private RSAKeyPair serverPublicKey;
        private string serverHost;
        private int serverPort;

        public StudentClientService(RSAKeyPair serverPublicKey, string host = "127.0.0.1", int port = 8888)
        {
            this.serverPublicKey = serverPublicKey;
            this.serverHost = host;
            this.serverPort = port;
        }

        public async Task<StudentScoreResponse> SendScoreLookupRequestAsync(string studentCode, int maskingType)
        {
            try
            {
                TransmissionLogger.LogClient("=======================================================");
                TransmissionLogger.LogClient($"[CLIENT] Bắt đầu tra cứu điểm cho mã SV: {studentCode}");
                TransmissionLogger.LogClient($"[CLIENT] Phương thức masking: {GetMaskingTypeName(maskingType)}");

                using (TcpClient client = new TcpClient())
                {
                    TransmissionLogger.LogClient($"[CLIENT] Đang kết nối đến Server {serverHost}:{serverPort}...");
                    await client.ConnectAsync(serverHost, serverPort);
                    TransmissionLogger.LogClient("[CLIENT] Đã kết nối thành công!");

                    NetworkStream stream = client.GetStream();

                    // 1. Tạo AES key ngẫu nhiên
                    AESKey aesKey = new AESKey();
                    TransmissionLogger.LogClient($"[CLIENT] Đã tạo AES Key: {BitConverter.ToString(aesKey.Key).Substring(0, 47)}...");

                    // 2. Mã hóa AES key bằng RSA public key của server
                    RSA rsa = new RSA(serverPublicKey.N, serverPublicKey.E);
                    byte[] encryptedAESKey = rsa.Encrypt(aesKey.Key);
                    TransmissionLogger.LogClient($"[CLIENT] Đã mã hóa AES Key bằng RSA ({encryptedAESKey.Length} bytes)");

                    // 3. Gửi encrypted AES key
                    byte[] keyLengthBytes = BitConverter.GetBytes(encryptedAESKey.Length);
                    await stream.WriteAsync(keyLengthBytes, 0, 4);
                    await stream.WriteAsync(encryptedAESKey, 0, encryptedAESKey.Length);
                    TransmissionLogger.LogClient($"[CLIENT] Đã gửi Encrypted AES Key ({encryptedAESKey.Length} bytes)");

                    // 4. Tạo request JSON
                    string requestJson = $"{{\"action\":\"lookup\",\"studentCode\":\"{studentCode}\",\"maskingType\":{maskingType}}}";
                    TransmissionLogger.LogClient($"[CLIENT] Request JSON: {requestJson}");

                    // 5. Mã hóa request bằng AES
                    AES aes = new AES(aesKey.Key);
                    byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson);
                    byte[] encryptedRequest = aes.EncryptCBC(requestBytes, aesKey.IV);
                    string encryptedRequestHex = BitConverter.ToString(encryptedRequest).Replace("-", "");
                    TransmissionLogger.LogClient($"[CLIENT] Đã mã hóa request bằng AES ({encryptedRequest.Length} bytes)");
                    TransmissionLogger.LogClient($"[CLIENT] Encrypted Request (Hex): {encryptedRequestHex.Substring(0, Math.Min(100, encryptedRequestHex.Length))}...");

                    // 6. Gửi IV
                    await stream.WriteAsync(aesKey.IV, 0, aesKey.IV.Length);
                    TransmissionLogger.LogClient($"[CLIENT] Đã gửi IV ({aesKey.IV.Length} bytes)");

                    // 7. Gửi encrypted request
                    byte[] requestLengthBytes = BitConverter.GetBytes(encryptedRequest.Length);
                    await stream.WriteAsync(requestLengthBytes, 0, 4);
                    await stream.WriteAsync(encryptedRequest, 0, encryptedRequest.Length);
                    TransmissionLogger.LogClient($"[CLIENT] Đã gửi Encrypted Request ({encryptedRequest.Length} bytes)");

                    // 8. Nhận response từ server
                    TransmissionLogger.LogClient("[CLIENT] Đang chờ response từ Server...");
                    
                    byte[] responseLengthBytes = new byte[4];
                    await stream.ReadAsync(responseLengthBytes, 0, 4);
                    int responseLength = BitConverter.ToInt32(responseLengthBytes, 0);

                    byte[] encryptedResponse = new byte[responseLength];
                    int totalRead = 0;
                    while (totalRead < responseLength)
                    {
                        int read = await stream.ReadAsync(encryptedResponse, totalRead, responseLength - totalRead);
                        totalRead += read;
                    }
                    string encryptedResponseHex = BitConverter.ToString(encryptedResponse).Replace("-", "");
                    TransmissionLogger.LogClient($"[CLIENT] Đã nhận Encrypted Response ({responseLength} bytes)");
                    TransmissionLogger.LogClient($"[CLIENT] Encrypted Response (Hex): {encryptedResponseHex.Substring(0, Math.Min(100, encryptedResponseHex.Length))}...");

                    // 9. Giải mã response
                    byte[] decryptedResponse = aes.DecryptCBC(encryptedResponse, aesKey.IV);
                    string responseJson = Encoding.UTF8.GetString(decryptedResponse);
                    TransmissionLogger.LogClient($"[CLIENT] Đã giải mã response");
                    TransmissionLogger.LogClient($"[CLIENT] Response JSON: {responseJson.Substring(0, Math.Min(200, responseJson.Length))}...");

                    // 10. Parse response với case-insensitive
                    var jsonOptions = new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    StudentScoreResponse response = null;
                    try
                    {
                        response = System.Text.Json.JsonSerializer.Deserialize<StudentScoreResponse>(responseJson, jsonOptions);
                        
                        if (response == null)
                        {
                            TransmissionLogger.LogClient($"[CLIENT] Response deserialization returned null");
                            return new StudentScoreResponse
                            {
                                Success = false,
                                Message = "Lỗi parse response từ server"
                            };
                        }
                        
                        TransmissionLogger.LogClient($"[CLIENT] Response parsed successfully");
                    }
                    catch (Exception parseEx)
                    {
                        TransmissionLogger.LogClient($"[CLIENT] Parse error: {parseEx.Message}");
                        TransmissionLogger.LogClient($"[CLIENT] Full response: {responseJson}");
                        return new StudentScoreResponse
                        {
                            Success = false,
                            Message = "Lỗi parse response: " + parseEx.Message
                        };
                    }
                    
                    if (response.Success)
                    {
                        TransmissionLogger.LogClient($"[CLIENT] Tra cứu thành công! Tìm thấy {response.Scores?.Count ?? 0} môn học");
                    }
                    else
                    {
                        TransmissionLogger.LogClient($"[CLIENT] Tra cứu thất bại: {response.Message}");
                    }

                    TransmissionLogger.LogClient("=======================================================");
                    return response;
                }
            }
            catch (Exception ex)
            {
                TransmissionLogger.LogClient($"[CLIENT] LỖI: {ex.Message}");
                TransmissionLogger.LogClient("=======================================================");
                return new StudentScoreResponse
                {
                    Success = false,
                    Message = "Lỗi kết nối: " + ex.Message
                };
            }
        }

        private string GetMaskingTypeName(int type)
        {
            string[] names = { "Che mặt nạ ký tự", "Xáo trộn dữ liệu", "Thay thế dữ liệu giả", "Thêm nhiễu số" };
            return type >= 0 && type < names.Length ? names[type] : "Unknown";
        }

        public async Task<Models.ActvnScheduleResponse> SendActvnScheduleRequestAsync(string username, string password)
        {
            try
            {
                TransmissionLogger.LogClient("=======================================================");
                TransmissionLogger.LogClient($"[CLIENT] ACTVN Schedule Request - Username: {username}");

                using (TcpClient client = new TcpClient())
                {
                    TransmissionLogger.LogClient($"[CLIENT] Connecting to Server {serverHost}:{serverPort}...");
                    await client.ConnectAsync(serverHost, serverPort);
                    TransmissionLogger.LogClient("[CLIENT] Connected successfully!");

                    NetworkStream stream = client.GetStream();

                    // 1. Create random AES key
                    AESKey aesKey = new AESKey();
                    TransmissionLogger.LogClient($"[CLIENT] Generated AES Key");

                    // 2. Encrypt AES key with RSA
                    RSA rsa = new RSA(serverPublicKey.N, serverPublicKey.E);
                    byte[] encryptedAESKey = rsa.Encrypt(aesKey.Key);
                    TransmissionLogger.LogClient($"[CLIENT] Encrypted AES Key with RSA ({encryptedAESKey.Length} bytes)");

                    // 3. Send encrypted AES key
                    byte[] keyLengthBytes = BitConverter.GetBytes(encryptedAESKey.Length);
                    await stream.WriteAsync(keyLengthBytes, 0, 4);
                    await stream.WriteAsync(encryptedAESKey, 0, encryptedAESKey.Length);
                    TransmissionLogger.LogClient($"[CLIENT] Sent Encrypted AES Key");

                    // 4. Generate IV and send
                    byte[] iv = aesKey.IV;
                    await stream.WriteAsync(iv, 0, iv.Length);
                    TransmissionLogger.LogClient($"[CLIENT] Sent IV");

                    // 5. Create request
                    var request = new Models.ActvnScheduleRequest
                    {
                        Action = "actvn_schedule",
                        Username = username,
                        Password = password
                    };

                    string requestJson = System.Text.Json.JsonSerializer.Serialize(request, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    });
                    TransmissionLogger.LogClient($"[CLIENT] Request JSON: {requestJson}");

                    // 6. Encrypt request
                    AES aes = new AES(aesKey.Key);
                    byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson);
                    byte[] encryptedRequest = aes.EncryptCBC(requestBytes, iv);
                    TransmissionLogger.LogClient($"[CLIENT] Encrypted request ({encryptedRequest.Length} bytes)");

                    // 7. Send encrypted request
                    byte[] requestLengthBytes = BitConverter.GetBytes(encryptedRequest.Length);
                    await stream.WriteAsync(requestLengthBytes, 0, 4);
                    await stream.WriteAsync(encryptedRequest, 0, encryptedRequest.Length);
                    TransmissionLogger.LogClient($"[CLIENT] Sent encrypted request");

                    // 8. Receive response
                    byte[] responseLengthBytes = new byte[4];
                    await stream.ReadAsync(responseLengthBytes, 0, 4);
                    int responseLength = BitConverter.ToInt32(responseLengthBytes, 0);

                    byte[] encryptedResponse = new byte[responseLength];
                    int totalRead = 0;
                    while (totalRead < responseLength)
                    {
                        int read = await stream.ReadAsync(encryptedResponse, totalRead, responseLength - totalRead);
                        totalRead += read;
                    }
                    TransmissionLogger.LogClient($"[CLIENT] Received encrypted response ({responseLength} bytes)");

                    // 9. Decrypt response
                    byte[] decryptedResponse = aes.DecryptCBC(encryptedResponse, iv);
                    string responseJson = Encoding.UTF8.GetString(decryptedResponse);
                    TransmissionLogger.LogClient($"[CLIENT] Decrypted response");

                    var response = System.Text.Json.JsonSerializer.Deserialize<Models.ActvnScheduleResponse>(responseJson, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    TransmissionLogger.LogClient($"[CLIENT] Response: Success={response.Success}, Events={response.Events?.Count ?? 0}");
                    TransmissionLogger.LogClient("=======================================================");

                    return response;
                }
            }
            catch (Exception ex)
            {
                TransmissionLogger.LogClient($"[CLIENT] Error: {ex.Message}");
                TransmissionLogger.LogClient("=======================================================");
                return new Models.ActvnScheduleResponse
                {
                    Success = false,
                    Message = $"Lỗi kết nối: {ex.Message}",
                    Events = new System.Collections.Generic.List<Models.ActvnCalendarEvent>()
                };
            }
        }
    }

    public class StudentScoreResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public StudentInfo Student { get; set; }
        public System.Collections.Generic.List<ScoreInfo> Scores { get; set; }
        public double GPA { get; set; }
        public double CPA { get; set; }
        public long TotalCredits { get; set; }
        public string LastSemester { get; set; }
    }

    public class StudentInfo
    {
        public string StudentCode { get; set; }
        public string StudentName { get; set; }
        public string StudentClass { get; set; }
    }

    public class ScoreInfo
    {
        public string Semester { get; set; }
        public string SubjectName { get; set; }
        public long SubjectCredits { get; set; }
        public float ScoreFirst { get; set; }
        public float ScoreSecond { get; set; }
        public float ScoreFinal { get; set; }
        public float ScoreOverall { get; set; }
        public string ScoreText { get; set; }
        public bool IsPassed { get; set; }
    }
}
