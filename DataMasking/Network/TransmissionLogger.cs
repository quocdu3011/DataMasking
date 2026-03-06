using System;
using System.Text;

namespace DataMasking.Network
{
    // Logger để hiển thị dữ liệu trên kênh truyền
    public class TransmissionLogger
    {
        // Event chung (backward compatible)
        public static event Action<string> OnLog;

        // Event riêng cho Client và Server
        public static event Action<string> OnClientLog;
        public static event Action<string> OnServerLog;

        public static void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logMessage = $"[{timestamp}] {message}";

            // Gửi đến event chung
            OnLog?.Invoke(logMessage);

            // Phân loại và gửi đến event riêng
            if (message.StartsWith("SERVER") || message.StartsWith("───") ||
                message.Contains("SERVER"))
            {
                OnServerLog?.Invoke(logMessage);
            }

            if (message.StartsWith("CLIENT") || message.StartsWith("═══") ||
                message.Contains("CLIENT"))
            {
                OnClientLog?.Invoke(logMessage);
            }

            // Log separator lines gửi cả hai nếu không rõ thuộc bên nào
            if (message.StartsWith("═══") || message.StartsWith("───"))
            {
                // Đã xử lý ở trên qua Contains
            }
        }

        public static void LogClientSend(string encryptedData, string encryptedKey, string iv)
        {
            Log("═══════════════════════════════════════════════════════");
            Log("CLIENT → SERVER: Gửi dữ liệu đã mã hóa");
            Log("═══════════════════════════════════════════════════════");
            Log($"CLIENT: Encrypted Data (AES): {encryptedData.Substring(0, Math.Min(80, encryptedData.Length))}...");
            Log($"CLIENT: Encrypted AES Key (RSA): {encryptedKey.Substring(0, Math.Min(80, encryptedKey.Length))}...");
            Log($"CLIENT: IV: {iv}");
            Log($"CLIENT: Total Size: {(encryptedData.Length + encryptedKey.Length + iv.Length)} bytes");
            Log("═══════════════════════════════════════════════════════\n");
        }

        public static void LogServerReceive(string encryptedData, string encryptedKey)
        {
            Log("───────────────────────────────────────────────────────");
            Log("SERVER: Nhận dữ liệu từ kênh truyền");
            Log("───────────────────────────────────────────────────────");
            Log($"SERVER: Received Encrypted Data: {encryptedData.Substring(0, Math.Min(80, encryptedData.Length))}...");
            Log($"SERVER: Received Encrypted Key: {encryptedKey.Substring(0, Math.Min(80, encryptedKey.Length))}...");
            Log("───────────────────────────────────────────────────────\n");
        }

        public static void LogServerDecrypt(string decryptedData)
        {
            Log("───────────────────────────────────────────────────────");
            Log("SERVER: Giải mã dữ liệu thành công");
            Log("───────────────────────────────────────────────────────");
            Log($"SERVER: Decrypted Data: {decryptedData.Substring(0, Math.Min(150, decryptedData.Length))}...");
            Log("───────────────────────────────────────────────────────\n");
        }

        public static void LogServerResponse(string maskedData)
        {
            Log("═══════════════════════════════════════════════════════");
            Log("SERVER → CLIENT: Trả về dữ liệu đã che giấu (masked)");
            Log("═══════════════════════════════════════════════════════");
            Log($"SERVER: Masked Response: {maskedData}");
            Log("═══════════════════════════════════════════════════════\n");
        }

        private static string TruncateForDisplay(string data, int maxLength = 100)
        {
            if (string.IsNullOrEmpty(data)) return "";
            if (data.Length <= maxLength) return data;
            return data.Substring(0, maxLength) + "... [truncated]";
        }
    }
}
