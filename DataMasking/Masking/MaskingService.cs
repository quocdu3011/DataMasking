using System;
using System.Linq;
using System.Text;
using DataMasking.Crypto;
using DataMasking.Key;

namespace DataMasking.Masking
{
    public class MaskingService
    {
        private RSAKeyPair rsaKeyPair;
        private AESKey aesKey;
        private Random random = new Random();

        // ===== FAKE DATA ARRAYS =====
        private static readonly string[] FakeNames = new[]
        {
            "Nguyễn Văn An", "Trần Thị Bích", "Lê Hoàng Dũng", "Phạm Minh Châu",
            "Hoàng Thị Lan", "Vũ Đức Mạnh", "Đặng Quốc Huy", "Bùi Thị Mai",
            "Đỗ Thanh Tùng", "Ngô Thị Hồng", "Dương Văn Phúc", "Lý Thị Ngọc",
            "Phan Văn Khải", "Trịnh Thị Hà", "Hà Minh Tuấn", "Võ Thị Thảo"
        };

        private static readonly string[] FakeEmails = new[]
        {
            "user123@gmail.com", "contact@yahoo.com", "info@outlook.com",
            "admin@hotmail.com", "support@company.vn", "test@domain.com",
            "hello@mail.com", "demo@example.com", "nguyen.van@gmail.com",
            "tran.thi@yahoo.com", "le.hoang@outlook.com", "pham.minh@mail.vn"
        };

        private static readonly string[] FakePhones = new[]
        {
            "0901234567", "0912345678", "0923456789", "0934567890",
            "0945678901", "0956789012", "0967890123", "0978901234",
            "0389012345", "0370123456", "0861234567", "0852345678"
        };

        private static readonly string[] FakeCreditCards = new[]
        {
            "4532-1234-5678-9012", "4916-2345-6789-0123", "4024-3456-7890-1234",
            "5412-4567-8901-2345", "5234-5678-9012-3456", "5567-6789-0123-4567",
            "3782-7890-1234-567", "6011-8901-2345-6789", "4111-1111-1111-1111"
        };

        private static readonly string[] FakeSSNs = new[]
        {
            "012345678901", "023456789012", "034567890123", "045678901234",
            "056789012345", "067890123456", "078901234567", "089012345678",
            "090123456789", "001234567890", "002345678901", "003456789012"
        };

        private static readonly string[] FakeAddresses = new[]
        {
            "123 Nguyễn Huệ, Q.1, TP.HCM", "456 Lê Lợi, Q.3, TP.HCM",
            "789 Trần Hưng Đạo, Q.5, TP.HCM", "12 Hai Bà Trưng, Q.1, TP.HCM",
            "34 Phạm Ngũ Lão, Q.1, TP.HCM", "56 Võ Văn Tần, Q.3, TP.HCM",
            "78 Điện Biên Phủ, Q.Bình Thạnh, TP.HCM", "90 Cách Mạng T8, Q.3, TP.HCM",
            "15 Hoàng Hoa Thám, Ba Đình, Hà Nội", "25 Kim Mã, Ba Đình, Hà Nội",
            "100 Trần Phú, Hà Đông, Hà Nội", "50 Nguyễn Trãi, Thanh Xuân, Hà Nội"
        };

        public MaskingService()
        {
            // Khởi tạo RSA key pair (1024-bit)
            rsaKeyPair = new RSAKeyPair(1024);

            // Khởi tạo AES key
            aesKey = new AESKey();
        }

        // ===== UNIFIED MASKING METHOD =====

        /// <summary>
        /// Áp dụng masking theo loại đã chọn cho một field cụ thể.
        /// fieldType: "name", "email", "phone", "creditcard", "ssn", "address"
        /// </summary>
        public string ApplyMasking(string value, string fieldType, MaskingType type)
        {
            if (string.IsNullOrEmpty(value)) return value;

            switch (type)
            {
                case MaskingType.CharacterMask:
                    return ApplyCharacterMask(value, fieldType);
                case MaskingType.Shuffle:
                    return ApplyShuffle(value);
                case MaskingType.FakeData:
                    return ApplyFakeData(fieldType);
                case MaskingType.NumericNoise:
                    return ApplyNumericNoise(value);
                default:
                    return ApplyCharacterMask(value, fieldType);
            }
        }

        // ===== 1. CHARACTER MASK (logic hiện tại) =====

        private string ApplyCharacterMask(string value, string fieldType)
        {
            switch (fieldType.ToLower())
            {
                case "name": return MaskName(value);
                case "email": return MaskEmail(value);
                case "phone": return MaskPhone(value);
                case "creditcard": return MaskCreditCard(value);
                case "ssn": return MaskSSN(value);
                case "address": return MaskAddress(value);
                default: return MaskGeneric(value);
            }
        }

        // ===== 2. SHUFFLE (Xáo trộn dữ liệu) =====

        private string ApplyShuffle(string value)
        {
            char[] chars = value.ToCharArray();
            // Fisher-Yates shuffle
            for (int i = chars.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                char temp = chars[i];
                chars[i] = chars[j];
                chars[j] = temp;
            }
            return new string(chars);
        }

        // ===== 3. FAKE DATA (Thay thế bằng dữ liệu giả) =====

        private string ApplyFakeData(string fieldType)
        {
            switch (fieldType.ToLower())
            {
                case "name": return FakeNames[random.Next(FakeNames.Length)];
                case "email": return FakeEmails[random.Next(FakeEmails.Length)];
                case "phone": return FakePhones[random.Next(FakePhones.Length)];
                case "creditcard": return FakeCreditCards[random.Next(FakeCreditCards.Length)];
                case "ssn": return FakeSSNs[random.Next(FakeSSNs.Length)];
                case "address": return FakeAddresses[random.Next(FakeAddresses.Length)];
                default: return "[DỮ LIỆU GIẢ]";
            }
        }

        // ===== 4. NUMERIC NOISE (Thêm nhiễu vào ký tự số) =====

        private string ApplyNumericNoise(string value)
        {
            char[] chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsDigit(chars[i]))
                {
                    int digit = chars[i] - '0';
                    int noise = random.Next(-3, 4); // Nhiễu từ -3 đến +3
                    digit = ((digit + noise) % 10 + 10) % 10; // Giữ trong khoảng 0-9
                    chars[i] = (char)('0' + digit);
                }
            }
            return new string(chars);
        }

        // ===== ORIGINAL MASKING METHODS (Character Mask) =====

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

            // Nếu địa chỉ ngắn (<= 15 ký tự), che hết 80% độ dài
            if (address.Length <= 15)
            {
                int maskLen = (int)(address.Length * 0.8);
                string maskedShort = new string('*', maskLen);
                string visibleShort = address.Substring(maskLen);
                return maskedShort + visibleShort;
            }

            // Nếu dài hơn 15 ký tự, chỉ hiện 15 ký tự cuối. Phần còn lại (phía trước) che bằng dấu *
            int visibleLength = 15;
            int maskedLength = address.Length - visibleLength;
            string maskedPart = new string('*', maskedLength);
            string visiblePart = address.Substring(maskedLength);
            
            return maskedPart + visiblePart;
        }

        // Che giấu generic (dùng cho field không rõ type)
        private string MaskGeneric(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= 2) return new string('*', value?.Length ?? 0);
            return value[0] + new string('*', value.Length - 2) + value[value.Length - 1];
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
