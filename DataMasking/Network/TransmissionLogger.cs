using System;
using System.Text;

namespace DataMasking.Network
{
    // Logger để hiển thị dữ liệu trên kênh truyền
    public class TransmissionLogger
    {
        public static event Action<string> OnLog;

        public static void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logMessage = $"[{timestamp}] {message}";
            OnLog?.Invoke(logMessage);
        }

        public static void LogClientSend(string encryptedData, string encryptedKey, string iv)
        {
            Log("═══════════════════════════════════════════════════════");
            Log("CLIENT → SERVER: Gửi dữ liệu đã mã hóa");
            Log("═══════════════════════════════════════════════════════");
            Log($"Encrypted Data (AES): {encryptedData.Substring(0, Math.Min(80, encryptedData.Length))}...");
            Log($"Encrypted AES Key (RSA): {encryptedKey.Substring(0, Math.Min(80, encryptedKey.Length))}...");
            Log($"IV: {iv}");
            Log($"Total Size: {(encryptedData.Length + encryptedKey.Length + iv.Length)} bytes");
            Log("═══════════════════════════════════════════════════════\n");
        }

        public static void LogServerReceive(string encryptedData, string encryptedKey)
        {
            Log("───────────────────────────────────────────────────────");
            Log("SERVER: Nhận dữ liệu từ kênh truyền");
            Log("───────────────────────────────────────────────────────");
            Log($"Received Encrypted Data: {encryptedData.Substring(0, Math.Min(80, encryptedData.Length))}...");
            Log($"Received Encrypted Key: {encryptedKey.Substring(0, Math.Min(80, encryptedKey.Length))}...");
            Log("───────────────────────────────────────────────────────\n");
        }

        public static void LogServerDecrypt(string decryptedData)
        {
            Log("───────────────────────────────────────────────────────");
            Log("SERVER: Giải mã dữ liệu thành công");
            Log("───────────────────────────────────────────────────────");
            Log($"Decrypted Data: {decryptedData.Substring(0, Math.Min(150, decryptedData.Length))}...");
            Log("───────────────────────────────────────────────────────\n");
        }

        public static void LogServerResponse(string maskedData)
        {
            Log("═══════════════════════════════════════════════════════");
            Log("SERVER → CLIENT: Trả về dữ liệu đã che giấu (masked)");
            Log("═══════════════════════════════════════════════════════");
            Log($"Masked Response: {maskedData}");
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
