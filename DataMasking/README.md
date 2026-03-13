# Data Masking System - Hệ thống Che giấu & Mã hóa Dữ liệu trên Kênh truyền

Dự án triển khai hệ thống bảo vệ dữ liệu nhạy cảm khi truyền trên kênh công khai, sử dụng mã hóa RSA và AES được triển khai hoàn toàn từ đầu.

## Mục đích

Bảo vệ dữ liệu nhạy cảm khi truyền trên kênh công khai bằng cách:
1. **Client-Side Encryption**: Mã hóa dữ liệu trước khi gửi
2. **Secure Transmission**: Truyền dữ liệu đã mã hóa qua kênh công khai
3. **Server-Side Decryption**: Giải mã và lưu trữ an toàn
4. **Data Masking**: Che giấu thông tin nhạy cảm trong response

## Luồng hoạt động (Client-Server)

### 🔵 CLIENT (Người gửi)
1. Nhập dữ liệu nhạy cảm (họ tên, email, thẻ tín dụng, SSN...)
2. Tạo AES key ngẫu nhiên (256-bit)
3. Mã hóa dữ liệu bằng AES-256 (CBC mode)
4. Mã hóa AES key bằng RSA Public Key của server
5. Gửi packet qua kênh truyền:
   - Encrypted Data (AES)
   - Encrypted AES Key (RSA)
   - IV (Initialization Vector)

### 📡 KÊNH TRUYỀN (Public Channel)
- Dữ liệu truyền đi hoàn toàn đã được mã hóa
- Không thể đọc được nội dung gốc
- An toàn trước tấn công Man-in-the-Middle

### 🔴 SERVER (Người nhận)
1. Nhận packet từ kênh truyền
2. Giải mã AES key bằng RSA Private Key
3. Giải mã dữ liệu bằng AES key
4. Lưu dữ liệu gốc vào database
5. Tạo response với dữ liệu đã che giấu (masked)
6. Gửi response về client

### ✅ RESPONSE
- Client nhận dữ liệu đã được che giấu
- Xác nhận dữ liệu đã được lưu thành công
- Hiển thị thông tin an toàn (masked)

## Tính năng chính

### 1. Tra cứu điểm sinh viên (Score Lookup)
- Tra cứu điểm theo mã sinh viên
- Hiển thị GPA, CPA, tổng tín chỉ
- Bảng quy đổi điểm thang 10 sang thang 4
- Thống kê phân bố điểm
- Tính toán CPA mục tiêu
- Data masking cho thông tin sinh viên

### 2. Bảng điểm ảo (Virtual Transcript) ⭐ MỚI
- **Yêu cầu đăng nhập**: Phải đăng nhập ACTVN Portal trước khi sử dụng
- **Tải điểm mã hóa**: Query mã sinh viên từ client xuống server với mã hóa RSA + AES
- **Chọn môn tính CPA**: Tích checkbox để chọn môn học tính vào CPA dự kiến
- **Tự động bỏ qua Thể chất**: Môn Thể chất không được tính vào CPA
- **Nút chọn tất cả**: Chọn tất cả môn học (trừ Thể chất) một lần
- **Tính CPA dự kiến**: Tự động tính toán CPA dựa trên các môn đã chọn
- **Bảng quy đổi điểm**: Xem bảng quy đổi điểm thang 10 sang thang 4
- **Lưu lên server**: Lưu bảng điểm đã chọn vào database (bảng `score_batches` và `score_items`)
- **Màu sắc trực quan**:
  - Xanh lá: CPA >= 3.6 (Xuất sắc)
  - Vàng: CPA >= 3.0 (Giỏi)
  - Cam: CPA >= 2.5 (Khá)
  - Đỏ: CPA < 2.5 (Trung bình)

### 3. Xem lịch học (Timetable)
- Đăng nhập ACTVN Portal
- Lấy lịch học từ server
- Hiển thị dạng lịch tuần hoặc calendar
- Cache credentials để sử dụng lại

### 4. Data Masking (Che giấu dữ liệu)
- **Họ tên**: Nguyễn Văn An → N****** V** A*
- **Email**: user@example.com → u***r@example.com
- **Số điện thoại**: 0901234567 → ******4567
- **Thẻ tín dụng**: 4532-1234-5678-9010 → ************9010
- **SSN**: 123-45-6789 → *******6789
- **Địa chỉ**: Chỉ hiển thị thành phố/quận

### 2. Mã hóa (Encryption)

#### AES-256 (Advanced Encryption Standard)
- Triển khai đầy đủ từ S-Box, Inverse S-Box
- Các phép biến đổi: SubBytes, ShiftRows, MixColumns, AddRoundKey
- CBC mode với PKCS7 Padding
- Key expansion cho AES-256

#### RSA-1024 (Rivest-Shamir-Adleman)
- Sinh số nguyên tố bằng Miller-Rabin
- Tính khóa công khai (N, E) và khóa riêng (D)
- Lũy thừa modulo tối ưu
- Hỗ trợ mã hóa dữ liệu lớn

#### Hybrid Encryption
- RSA mã hóa khóa AES
- AES mã hóa dữ liệu thực tế
- Hiệu quả cho dữ liệu lớn

### 3. Quản lý Database (MySQL)
- Lưu trữ dữ liệu nhạy cảm
- Lưu trữ dữ liệu đã mã hóa
- Quản lý CRUD operations

## Cấu trúc dự án

```
DataMasking/
├── Crypto/
│   ├── AES.cs           # Triển khai AES-256 từ S-Box
│   ├── RSA.cs           # Triển khai RSA-1024
│   └── CryptoDemo.cs    # Demo console
├── Key/
│   └── KeyPair.cs       # Quản lý khóa RSA và AES
├── Masking/
│   └── MaskingService.cs # Service che giấu dữ liệu
├── Network/
│   ├── ClientService.cs      # Client - Mã hóa và gửi
│   ├── ServerService.cs      # Server - Nhận và giải mã
│   └── TransmissionLogger.cs # Log kênh truyền
├── Database/
│   └── DatabaseManager.cs # Quản lý MySQL
├── Utils/
│   └── BigMath.cs       # Toán học số lớn
├── Main.cs              # Giao diện Windows Form
└── Program.cs           # Entry point
```

## Ví dụ Transmission Log

```
[10:30:15.123] CLIENT: Bắt đầu chuẩn bị dữ liệu để gửi...
[10:30:15.125] CLIENT: Dữ liệu gốc (plaintext): {"fullName":"Nguyễn Văn An",...}
[10:30:15.130] CLIENT: Đã tạo AES key ngẫu nhiên
[10:30:15.145] CLIENT: Đã mã hóa dữ liệu bằng AES-256 (CBC mode)
[10:30:15.180] CLIENT: Đã mã hóa AES key bằng RSA Public Key của server
[10:30:15.181] ═══════════════════════════════════════════════════════
[10:30:15.181] CLIENT → SERVER: Gửi dữ liệu đã mã hóa
[10:30:15.181] ═══════════════════════════════════════════════════════
[10:30:15.181] Encrypted Data (AES): xK9mP2vL8nQ... [truncated]
[10:30:15.181] Encrypted AES Key (RSA): 7yH3jK9... [truncated]
[10:30:15.181] IV: mN4pQ8rT2vW...
[10:30:15.181] Total Size: 1024 bytes
[10:30:15.281] ───────────────────────────────────────────────────────
[10:30:15.281] SERVER: Nhận dữ liệu từ kênh truyền
[10:30:15.281] ───────────────────────────────────────────────────────
[10:30:15.282] SERVER: Đang giải mã AES key bằng RSA Private Key...
[10:30:15.320] SERVER: Đã giải mã AES key thành công
[10:30:15.321] SERVER: Đang giải mã dữ liệu bằng AES-256...
[10:30:15.335] SERVER: Giải mã dữ liệu thành công
[10:30:15.336] SERVER: Đang lưu dữ liệu vào database...
[10:30:15.450] SERVER: Đã lưu vào database với ID: 5
[10:30:15.451] ═══════════════════════════════════════════════════════
[10:30:15.451] SERVER → CLIENT: Trả về dữ liệu đã che giấu (masked)
[10:30:15.451] ═══════════════════════════════════════════════════════
[10:30:15.451] Masked Response: {"FullName":"N****** V** A*",...}
```

## Cài đặt

### Yêu cầu
- .NET 8.0 hoặc cao hơn
- MySQL Server 8.0+
- Visual Studio 2022

### Cài đặt MySQL
1. Tải và cài đặt MySQL Server
2. Tạo database:
```sql
CREATE DATABASE datamasking_db;
```

### Cài đặt package
```bash
dotnet add package MySql.Data
```

### Chạy ứng dụng
```bash
dotnet restore
dotnet build
dotnet run
```

## Hướng dẫn sử dụng

### Bước 1: Kết nối Database
1. Mở ứng dụng
2. Nhập connection string (mặc định: `Server=localhost;Port=3306;Database=datamasking_db;Uid=root;Pwd=;`)
3. Click "Khởi tạo DB" để tạo bảng
4. Click "Thêm dữ liệu mẫu" để test (tùy chọn)

### Bước 2: Gửi dữ liệu từ Client
1. Tab "Client - Nhập & Gửi Dữ liệu"
2. Điền thông tin:
   - Họ và tên
   - Email
   - Số điện thoại
   - Thẻ tín dụng
   - SSN
   - Địa chỉ
3. Click "Gửi đến Server (Mã hóa)"
4. Quan sát "KÊNH TRUYỀN" bên phải để thấy:
   - Client mã hóa dữ liệu
   - Dữ liệu được truyền (đã mã hóa)
   - Server nhận và giải mã
   - Server lưu vào DB
   - Response trả về (masked)

### Bước 3: Xem kết quả
- **Response ngay trên form**: Dữ liệu đã che giấu
- **Tab "Server - Dữ liệu Gốc"**: Xem dữ liệu thật trong DB
- **Tab "Server - Dữ liệu Masked"**: Xem dữ liệu đã che giấu
- **Transmission Log**: Xem toàn bộ quá trình mã hóa/giải mã

## Chi tiết kỹ thuật

### Data Masking Algorithms
```csharp
// Che giấu tên
"Nguyễn Văn An" → "N****** V** A*"

// Che giấu email
"user@example.com" → "u***r@example.com"

// Che giấu số điện thoại
"0901234567" → "******4567"

// Che giấu thẻ tín dụng
"4532-1234-5678-9010" → "************9010"
```

### AES-256 Implementation
- **S-Box**: Bảng thay thế 16x16 chuẩn AES
- **Key Expansion**: 256-bit → 15 round keys
- **Encryption**: 14 rounds + final round
- **Mode**: CBC (Cipher Block Chaining)
- **Padding**: PKCS7

### RSA-1024 Implementation
- **Prime Generation**: Miller-Rabin (10 rounds)
- **Key Size**: 1024-bit (512-bit primes)
- **Public Exponent**: 65537
- **Modular Exponentiation**: Square-and-multiply

### Database Schema
```sql
-- Bảng dữ liệu gốc
CREATE TABLE sensitive_data (
    id INT AUTO_INCREMENT PRIMARY KEY,
    full_name VARCHAR(100),
    email VARCHAR(100),
    phone VARCHAR(20),
    credit_card VARCHAR(19),
    ssn VARCHAR(11),
    address TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Bảng dữ liệu mã hóa
CREATE TABLE encrypted_data (
    id INT AUTO_INCREMENT PRIMARY KEY,
    original_id INT,
    encrypted_content LONGTEXT,
    encryption_type VARCHAR(20),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (original_id) REFERENCES sensitive_data(id)
);
```

## Use Cases

### 1. Truyền dữ liệu nhạy cảm qua Internet
**Vấn đề**: Cần gửi thông tin cá nhân, thẻ tín dụng qua mạng công khai

**Giải pháp**:
- Client mã hóa dữ liệu bằng Hybrid Encryption (RSA + AES)
- Dữ liệu trên kênh truyền hoàn toàn bị mã hóa
- Chỉ server có private key mới giải mã được
- An toàn trước tấn công nghe lén (eavesdropping)

### 2. Hiển thị dữ liệu cho người dùng
**Vấn đề**: Cần hiển thị thông tin nhưng không lộ toàn bộ

**Giải pháp**:
- Server trả về dữ liệu đã masked
- Người dùng thấy đủ thông tin để nhận diện
- Thông tin nhạy cảm được che giấu

### 3. Lưu trữ an toàn
**Vấn đề**: Database có thể bị tấn công

**Giải pháp**:
- Dữ liệu đã được giải mã mới lưu vào DB
- Có thể thêm layer mã hóa database
- Audit log đầy đủ

## Điểm nổi bật

✅ **Mã hóa end-to-end**: Dữ liệu được mã hóa từ client đến server

✅ **Hybrid Encryption**: Kết hợp RSA (bất đối xứng) và AES (đối xứng) cho hiệu quả tối ưu

✅ **Transmission Log**: Hiển thị chi tiết quá trình mã hóa/giải mã

✅ **Data Masking**: Che giấu thông tin nhạy cảm trong response

✅ **Không dùng thư viện**: Tất cả thuật toán được code từ đầu (S-Box, RSA, AES)

✅ **Mô phỏng thực tế**: Client-Server architecture rõ ràng

## Lưu ý bảo mật

⚠️ **Đây là dự án học tập**. Trong môi trường production:
- Sử dụng thư viện mã hóa đã được kiểm chứng
- RSA key size ≥ 2048-bit
- Sử dụng OAEP padding cho RSA
- Random number generator an toàn (RNGCryptoServiceProvider)
- Quản lý khóa chặt chẽ (Key Management System)
- Audit log cho mọi thao tác

## Demo

Chương trình bao gồm:
- ✅ Giao diện Windows Form với Client-Server simulation
- ✅ Kết nối MySQL
- ✅ Transmission Log real-time
- ✅ Data Masking cho 6 loại dữ liệu
- ✅ Hybrid Encryption (RSA + AES)
- ✅ CRUD operations
- ✅ Dữ liệu mẫu để test

### Screenshot mô tả:
```
┌─────────────────────────────────────────────────────────────────────┐
│  [Client Tab]              │  [Transmission Log]                    │
│  ┌──────────────────────┐  │  CLIENT: Mã hóa dữ liệu...            │
│  │ Họ tên: [________]   │  │  CLIENT → SERVER: Gửi...              │
│  │ Email:  [________]   │  │  Encrypted Data: xK9mP2vL8...         │
│  │ Phone:  [________]   │  │  SERVER: Nhận dữ liệu...              │
│  │ ...                  │  │  SERVER: Giải mã thành công...        │
│  │ [Gửi đến Server]     │  │  SERVER → CLIENT: Response...         │
│  └──────────────────────┘  │  Masked Data: N****** V** A*          │
│                             │                                        │
│  Response:                  │                                        │
│  ✓ Thành công! ID: 5        │                                        │
│  Họ tên: N****** V** A*     │                                        │
│  Email: n***@email.com      │                                        │
└─────────────────────────────────────────────────────────────────────┘
```

## Tác giả

Dự án Data Masking System - Bảo vệ dữ liệu nhạy cảm khi truyền trên kênh công khai

