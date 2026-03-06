using System;
using System.Text;
using DataMasking.Crypto;
using DataMasking.Key;

namespace DataMasking.Masking
{
    public class MaskingService
    {
        private RSAKeyPair rsaKeyPair;
        private AESKey aesKey;

        public MaskingService()
        {
            // Khởi tạo RSA key pair (1024-bit)
            rsaKeyPair = new RSAKeyPair(1024);
            
            // Khởi tạo AES key
            aesKey = new AESKey();
        }

        // ===== DATA MASKING METHODS =====

        // Che giấu tên (chỉ hiện chữ cái đầu)
        public string MaskName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            
            string[] parts = name.Split(' ');
            StringBuilder masked = new StringBuilder();
            
            foreach (string part in parts)
            {
                if (part.Length > 0)
                {
                    masked.Append(part[0]);
                    masked.Append(new string('*', part.Length - 1));
                    masked.Append(" ");
                }
            }
            
            return masked.ToString().Trim();
        }

        // Che giấu email
        public string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@")) return email;
            
            string[] parts = email.Split('@');
            string username = parts[0];
            string domain = parts[1];
            
            if (username.Length <= 2)
                return new string('*', username.Length) + "@" + domain;
            
            return username[0] + new string('*', username.Length - 2) + username[username.Length - 1] + "@" + domain;
        }

        // Che giấu số điện thoại
        public string MaskPhone(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return phone;
            
            string digits = phone.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "");
            
            if (digits.Length < 4) return new string('*', digits.Length);
            
            return new string('*', digits.Length - 4) + digits.Substring(digits.Length - 4);
        }

        // Che giấu thẻ tín dụng
        public string MaskCreditCard(string creditCard)
        {
            if (string.IsNullOrEmpty(creditCard)) return creditCard;
            
            string digits = creditCard.Replace("-", "").Replace(" ", "");
            
            if (digits.Length < 4) return new string('*', digits.Length);
            
            return new string('*', digits.Length - 4) + digits.Substring(digits.Length - 4);
        }

        // Che giấu SSN (Social Security Number)
        public string MaskSSN(string ssn)
        {
            if (string.IsNullOrEmpty(ssn)) return ssn;
            
            string digits = ssn.Replace("-", "");
            
            if (digits.Length < 4) return new string('*', digits.Length);
            
            return new string('*', digits.Length - 4) + digits.Substring(digits.Length - 4);
        }

        // Che giấu địa chỉ
        public string MaskAddress(string address)
        {
            if (string.IsNullOrEmpty(address)) return address;
            
            // Nếu địa chỉ ngắn (< 20 ký tự), che hết
            if (address.Length < 20)
            {
                return new string('*', address.Length);
            }
            
            // Nếu có dấu phẩy, chỉ hiện phần cuối
            if (address.Contains(','))
            {
                string[] parts = address.Split(',');
                // Lấy 2 phần cuối
                if (parts.Length >= 2)
                {
                    string visible = parts[parts.Length - 2].Trim() + ", " + parts[parts.Length - 1].Trim();
                    return "*****, " + visible;
                }
            }
            
            // Nếu không có dấu phẩy, chỉ hiện 10 ký tự cuối
            int visibleLength = Math.Min(10, address.Length / 3);
            string masked = new string('*', address.Length - visibleLength);
            string visible = address.Substring(address.Length - visibleLength);
            return masked + visible;
        }

        // ===== ENCRYPTION METHODS =====

        // Mã hóa AES
        public byte[] EncryptAES(string plaintext)
        {
            byte[] data = Encoding.UTF8.GetBytes(plaintext);
            AES aes = new AES(aesKey.Key);
            return aes.EncryptCBC(data, aesKey.IV);
        }

        // Giải mã AES
        public string DecryptAES(byte[] ciphertext)
        {
            AES aes = new AES(aesKey.Key);
            byte[] decrypted = aes.DecryptCBC(ciphertext, aesKey.IV);
            return Encoding.UTF8.GetString(decrypted);
        }

        // Mã hóa RSA
        public byte[] EncryptRSA(string plaintext)
        {
            RSA rsa = new RSA(rsaKeyPair.N, rsaKeyPair.E);
            byte[] data = Encoding.UTF8.GetBytes(plaintext);
            return rsa.EncryptLarge(data);
        }

        // Giải mã RSA
        public string DecryptRSA(byte[] ciphertext)
        {
            RSA rsa = new RSA(rsaKeyPair.N, rsaKeyPair.E, rsaKeyPair.D);
            byte[] decrypted = rsa.DecryptLarge(ciphertext);
            return Encoding.UTF8.GetString(decrypted);
        }

        // Mã hóa hybrid (RSA + AES)
        public (byte[] encryptedData, byte[] encryptedKey, byte[] iv) EncryptHybrid(string plaintext)
        {
            // Tạo AES key mới
            AESKey tempKey = new AESKey();
            
            // Mã hóa dữ liệu bằng AES
            AES aes = new AES(tempKey.Key);
            byte[] encryptedData = aes.EncryptCBC(Encoding.UTF8.GetBytes(plaintext), tempKey.IV);
            
            // Mã hóa AES key bằng RSA
            RSA rsa = new RSA(rsaKeyPair.N, rsaKeyPair.E);
            byte[] encryptedKey = rsa.Encrypt(tempKey.Key);
            
            return (encryptedData, encryptedKey, tempKey.IV);
        }

        // Giải mã hybrid
        public string DecryptHybrid(byte[] encryptedData, byte[] encryptedKey, byte[] iv)
        {
            // Giải mã AES key bằng RSA
            RSA rsa = new RSA(rsaKeyPair.N, rsaKeyPair.E, rsaKeyPair.D);
            byte[] aesKeyBytes = rsa.Decrypt(encryptedKey);
            
            // Đảm bảo key đúng kích thước
            byte[] key = new byte[32];
            Array.Copy(aesKeyBytes, key, Math.Min(32, aesKeyBytes.Length));
            
            // Giải mã dữ liệu bằng AES
            AES aes = new AES(key);
            byte[] decrypted = aes.DecryptCBC(encryptedData, iv);
            
            return Encoding.UTF8.GetString(decrypted);
        }

        // Mã hóa toàn bộ record thành JSON
        public string EncryptRecord(string fullName, string email, string phone, 
                                    string creditCard, string ssn, string address, string method = "AES")
        {
            string json = $"{{\"fullName\":\"{fullName}\",\"email\":\"{email}\",\"phone\":\"{phone}\"," +
                         $"\"creditCard\":\"{creditCard}\",\"ssn\":\"{ssn}\",\"address\":\"{address}\"}}";
            
            if (method == "AES")
            {
                byte[] encrypted = EncryptAES(json);
                return Convert.ToBase64String(encrypted);
            }
            else if (method == "RSA")
            {
                byte[] encrypted = EncryptRSA(json);
                return Convert.ToBase64String(encrypted);
            }
            else // Hybrid
            {
                var (data, key, iv) = EncryptHybrid(json);
                return Convert.ToBase64String(data) + "|" + 
                       Convert.ToBase64String(key) + "|" + 
                       Convert.ToBase64String(iv);
            }
        }

        // Giải mã record
        public string DecryptRecord(string encryptedData, string method = "AES")
        {
            try
            {
                if (method == "AES")
                {
                    byte[] data = Convert.FromBase64String(encryptedData);
                    return DecryptAES(data);
                }
                else if (method == "RSA")
                {
                    byte[] data = Convert.FromBase64String(encryptedData);
                    return DecryptRSA(data);
                }
                else // Hybrid
                {
                    string[] parts = encryptedData.Split('|');
                    byte[] data = Convert.FromBase64String(parts[0]);
                    byte[] key = Convert.FromBase64String(parts[1]);
                    byte[] iv = Convert.FromBase64String(parts[2]);
                    return DecryptHybrid(data, key, iv);
                }
            }
            catch
            {
                return "Lỗi giải mã";
            }
        }

        // Lấy thông tin khóa
        public string GetRSAPublicKey()
        {
            return $"N: {rsaKeyPair.N}\nE: {rsaKeyPair.E}";
        }

        public string GetRSAPrivateKey()
        {
            return $"N: {rsaKeyPair.N}\nD: {rsaKeyPair.D}";
        }

        public string GetAESKey()
        {
            return $"Key: {BitConverter.ToString(aesKey.Key)}\nIV: {BitConverter.ToString(aesKey.IV)}";
        }
    }
}

