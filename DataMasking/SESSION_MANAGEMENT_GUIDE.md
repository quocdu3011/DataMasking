# 🔐 Hướng dẫn quản lý Session

## Tổng quan

Hệ thống quản lý session cho phép cache thông tin đăng nhập ACTVN Portal để tránh phải đăng nhập lại nhiều lần.

## Tính năng

### 1. Cache Credentials
- Sau khi đăng nhập thành công lần đầu, tài khoản và mật khẩu được cache trong memory
- Lần sau bấm "Xem lịch học" sẽ tự động load mà không cần đăng nhập lại
- Cache chỉ tồn tại trong phiên làm việc hiện tại (không lưu vào disk)

### 2. Nút Đăng xuất (Smart Visibility)
- **Vị trí**: Cạnh nút "Xem lịch học" và "Bảng điểm ảo"
- **Màu đỏ** để dễ nhận biết
- **Chỉ hiển thị khi đã đăng nhập** - Tránh gây nhầm lẫn
- **Tự động ẩn** khi chưa đăng nhập hoặc sau khi đăng xuất
- Click để xóa cache và đăng xuất

### 3. Auto Re-login
- Khi đóng form lịch học và mở lại, tự động load bằng credentials đã cache
- Không cần nhập lại username/password
- Nếu session hết hạn, tự động hiển thị form đăng nhập

## Cách sử dụng

### Lần đầu đăng nhập:
1. Bấm "📅 Xem lịch học"
2. Nhập username và password
3. Bấm "Đăng nhập"
4. Credentials được cache tự động
5. **Nút "🚪 Đăng xuất" xuất hiện**

### Lần sau:
1. Bấm "📅 Xem lịch học"
2. Tự động load lịch (không cần đăng nhập lại)
3. Form lịch học hiển thị ngay
4. **Nút "🚪 Đăng xuất" vẫn hiển thị**

### Đăng xuất:
1. Bấm "🚪 Đăng xuất"
2. Xác nhận đăng xuất
3. Cache bị xóa
4. **Nút "🚪 Đăng xuất" tự động ẩn**
5. Lần sau cần đăng nhập lại

## UI States

### Chưa đăng nhập:
```
[📅 Xem lịch học] [📊 Bảng điểm ảo]
```
Nút đăng xuất KHÔNG hiển thị

### Đã đăng nhập:
```
[📅 Xem lịch học] [📊 Bảng điểm ảo] [🚪 Đăng xuất]
```
Nút đăng xuất HIỂN THỊ (màu đỏ)

## Bảo mật

### Lưu trữ:
- Credentials chỉ lưu trong RAM (memory)
- KHÔNG lưu vào file hoặc database
- Tự động xóa khi thoát ứng dụng

### Mã hóa:
- Khi gửi lên server, credentials được mã hóa bằng AES + RSA
- Không truyền plain text qua network
- Tuân thủ chuẩn bảo mật

### Session timeout:
- Nếu server trả về lỗi (session hết hạn), tự động logout
- Yêu cầu đăng nhập lại
- Đảm bảo không dùng credentials cũ khi đã hết hạn

## API SessionManager

### Login
```csharp
Utils.SessionManager.Login(username, password);
```

### Logout
```csharp
Utils.SessionManager.Logout();
```

### Check login status
```csharp
bool isLoggedIn = Utils.SessionManager.IsLoggedIn;
bool hasCached = Utils.SessionManager.HasCachedCredentials();
```

### Get credentials
```csharp
string username = Utils.SessionManager.Username;
string password = Utils.SessionManager.Password;
```

## Flow diagram

```
[Bấm "Xem lịch học"]
        |
        v
[Có cache?] --No--> [Hiển thị form login]
        |                    |
       Yes                   v
        |            [Nhập username/password]
        |                    |
        v                    v
[Load với cache]      [Đăng nhập]
        |                    |
        v                    v
[Thành công?] --No--> [Hiển thị lỗi]
        |
       Yes
        |
        v
[Cache credentials]
        |
        v
[Hiển thị lịch học]
```

## Tips

1. **Đăng xuất khi đổi tài khoản**: Nhớ đăng xuất trước khi đăng nhập tài khoản khác
2. **Bảo mật**: Không để người khác dùng máy khi đã đăng nhập
3. **Thoát ứng dụng**: Cache tự động xóa khi thoát app
4. **Lỗi session**: Nếu gặp lỗi "session hết hạn", đăng xuất và đăng nhập lại

## Troubleshooting

### Không tự động load lịch?
- Kiểm tra đã đăng nhập thành công chưa
- Thử đăng xuất và đăng nhập lại
- Kiểm tra kết nối server

### Lỗi "session hết hạn"?
- Server đã hết session
- Bấm "Đăng xuất" và đăng nhập lại
- Credentials cũ không còn hợp lệ

### Muốn xóa cache?
- Bấm nút "🚪 Đăng xuất"
- Hoặc thoát ứng dụng
