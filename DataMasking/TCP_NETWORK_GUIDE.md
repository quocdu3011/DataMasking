# Hướng dẫn sử dụng Client-Server qua TCP

## Thay đổi chính

Dự án đã được cập nhật để Client và Server giao tiếp qua TCP socket thay vì gọi hàm trực tiếp.

## Kiến trúc mới

### Server (ServerService.cs)
- Khởi động TCP Listener trên port 8888
- Lắng nghe kết nối từ Client
- Nhận bytes qua TCP socket
- Giải mã dữ liệu và xử lý
- Gửi response về Client qua TCP

### Client (ClientService.cs)
- Kết nối đến Server qua TCP (127.0.0.1:8888)
- Mã hóa dữ liệu thành bytes
- Gửi bytes qua TCP socket
- Nhận response từ Server

## Cách sử dụng

### Bước 1: Khởi động Server
1. Mở ứng dụng
2. Click nút **"▶ Start Server"** (màu xanh lá)
3. Thấy thông báo "Server: Running on port 8888" (màu xanh)

### Bước 2: Gửi dữ liệu từ Client
1. Chuyển sang tab **"Client - Nhập & Gửi Dữ liệu"**
2. Nhập thông tin vào các trường
3. Click **"Gửi đến Server (Mã hóa)"**
4. Xem log truyền dữ liệu ở bên phải

### Bước 3: Dừng Server (khi cần)
- Click nút **"⏹ Stop Server"** (màu đỏ)

## Luồng dữ liệu

```
CLIENT                          NETWORK                         SERVER
------                          -------                         ------
1. Nhập dữ liệu
2. Mã hóa AES + RSA
3. Serialize thành JSON
4. Gửi bytes qua TCP    ------>  [TCP Socket]  ------>  5. Nhận bytes
                                                         6. Deserialize JSON
                                                         7. Giải mã RSA + AES
                                                         8. Lưu vào database
                                                         9. Tạo masked data
10. Nhận response       <------  [TCP Socket]  <------  10. Gửi response
11. Hiển thị kết quả
```

## Protocol truyền dữ liệu

### Request (Client → Server)
1. **4 bytes**: Kích thước packet (int32)
2. **N bytes**: JSON packet chứa:
   - `EncryptedData`: Dữ liệu đã mã hóa AES (Base64)
   - `EncryptedAESKey`: AES key đã mã hóa RSA (Base64)
   - `IV`: Initialization Vector cho AES (Base64)

### Response (Server → Client)
1. **4 bytes**: Kích thước response (int32)
2. **N bytes**: JSON response chứa:
   - `Success`: true/false
   - `RecordId`: ID bản ghi
   - `MaskedData`: Dữ liệu đã che giấu
   - `Message`: Thông báo

## Transmission Log

Xem chi tiết quá trình truyền dữ liệu ở ô **"KÊNH TRUYỀN (Transmission Log)"**:
- CLIENT: Các bước mã hóa và gửi
- SERVER: Các bước nhận và giải mã
- Hiển thị bytes được truyền qua mạng

## Lưu ý

- Server phải được Start trước khi Client gửi dữ liệu
- Mặc định Server chạy trên `127.0.0.1:8888` (localhost)
- Có thể thay đổi port trong code nếu cần
- Dữ liệu được mã hóa end-to-end (AES-256 + RSA-1024)

## Kiểm tra kết nối

Nếu gặp lỗi kết nối:
1. Kiểm tra Server đã Start chưa
2. Kiểm tra firewall có chặn port 8888 không
3. Xem log để biết chi tiết lỗi
