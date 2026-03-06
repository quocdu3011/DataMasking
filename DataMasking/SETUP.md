# Hướng dẫn Cài đặt và Chạy Dự án

## Bước 1: Cài đặt MySQL

### Windows
1. Tải MySQL Installer từ: https://dev.mysql.com/downloads/installer/
2. Chọn "MySQL Installer for Windows"
3. Chạy installer và chọn "Developer Default"
4. Trong quá trình cài đặt:
   - Đặt password cho root user (hoặc để trống)
   - Ghi nhớ port (mặc định: 3306)
5. Hoàn tất cài đặt

### Kiểm tra MySQL đã cài đặt
```bash
mysql --version
```

## Bước 2: Tạo Database

### Cách 1: Sử dụng MySQL Command Line
```bash
mysql -u root -p
```

Sau đó chạy:
```sql
CREATE DATABASE datamasking_db;
USE datamasking_db;
```

### Cách 2: Sử dụng MySQL Workbench
1. Mở MySQL Workbench
2. Kết nối đến localhost
3. Chạy query:
```sql
CREATE DATABASE datamasking_db;
```

## Bước 3: Cài đặt .NET SDK

1. Tải .NET 8.0 SDK từ: https://dotnet.microsoft.com/download
2. Cài đặt và kiểm tra:
```bash
dotnet --version
```

## Bước 4: Restore và Build Project

```bash
# Di chuyển vào thư mục project
cd DataMasking

# Restore packages
dotnet restore

# Build project
dotnet build
```

## Bước 5: Cấu hình Connection String

Khi chạy ứng dụng, cập nhật connection string theo cấu hình MySQL của bạn:

```
Server=localhost;Port=3306;Database=datamasking_db;Uid=root;Pwd=your_password;
```

Thay đổi:
- `localhost`: Địa chỉ MySQL server
- `3306`: Port của MySQL (mặc định là 3306)
- `datamasking_db`: Tên database
- `root`: Username
- `your_password`: Password của bạn (để trống nếu không có password)

## Bước 6: Chạy Ứng dụng

```bash
dotnet run
```

Hoặc mở bằng Visual Studio và nhấn F5.

## Bước 7: Sử dụng Ứng dụng

1. **Khởi tạo Database**
   - Click nút "Khởi tạo DB"
   - Chương trình sẽ tự động tạo các bảng cần thiết

2. **Thêm dữ liệu mẫu**
   - Click "Thêm dữ liệu mẫu"
   - 3 bản ghi mẫu sẽ được thêm vào

3. **Nhập dữ liệu mới**
   - Tab "Nhập Dữ liệu"
   - Điền form và click "Thêm vào Database"

4. **Xem dữ liệu**
   - Tab "Dữ liệu Gốc": Xem toàn bộ dữ liệu
   - Tab "Dữ liệu Che giấu": Xem dữ liệu đã mask
   - Tab "Dữ liệu Mã hóa": Xem dữ liệu đã mã hóa

## Xử lý Lỗi Thường Gặp

### Lỗi: "Unable to connect to any of the specified MySQL hosts"
- Kiểm tra MySQL service đang chạy
- Windows: Services → MySQL80 → Start
- Kiểm tra port 3306 không bị chặn

### Lỗi: "Access denied for user 'root'@'localhost'"
- Kiểm tra username và password
- Reset password MySQL nếu cần

### Lỗi: "Unknown database 'datamasking_db'"
- Chạy lại: `CREATE DATABASE datamasking_db;`
- Hoặc click "Khởi tạo DB" trong ứng dụng

### Lỗi: Package MySql.Data không tìm thấy
```bash
dotnet add package MySql.Data --version 8.3.0
dotnet restore
```

## Kiểm tra Kết nối MySQL

Tạo file test đơn giản:

```csharp
using MySql.Data.MySqlClient;

string connStr = "Server=localhost;Port=3306;Database=datamasking_db;Uid=root;Pwd=;";
try
{
    using (MySqlConnection conn = new MySqlConnection(connStr))
    {
        conn.Open();
        Console.WriteLine("Kết nối thành công!");
    }
}
catch (Exception ex)
{
    Console.WriteLine("Lỗi: " + ex.Message);
}
```

## Cấu trúc Database

Sau khi khởi tạo, database sẽ có 2 bảng:

### sensitive_data
```sql
id              INT (Primary Key, Auto Increment)
full_name       VARCHAR(100)
email           VARCHAR(100)
phone           VARCHAR(20)
credit_card     VARCHAR(19)
ssn             VARCHAR(11)
address         TEXT
created_at      TIMESTAMP
```

### encrypted_data
```sql
id                  INT (Primary Key, Auto Increment)
original_id         INT (Foreign Key → sensitive_data.id)
encrypted_content   LONGTEXT
encryption_type     VARCHAR(20)
created_at          TIMESTAMP
```

## Dữ liệu Mẫu

Sau khi click "Thêm dữ liệu mẫu":

| Họ tên | Email | Điện thoại | Thẻ tín dụng | SSN | Địa chỉ |
|--------|-------|------------|--------------|-----|---------|
| Nguyễn Văn An | nguyenvanan@email.com | 0901234567 | 4532-1234-5678-9010 | 123-45-6789 | 123 Đường Lê Lợi, Q1, TP.HCM |
| Trần Thị Bình | tranthibinh@email.com | 0912345678 | 5425-2345-6789-0123 | 234-56-7890 | 456 Đường Nguyễn Huệ, Q1, TP.HCM |
| Lê Văn Cường | levancuong@email.com | 0923456789 | 4916-3456-7890-1234 | 345-67-8901 | 789 Đường Hai Bà Trưng, Q3, TP.HCM |

## Hỗ trợ

Nếu gặp vấn đề, kiểm tra:
1. MySQL service đang chạy
2. Connection string đúng
3. Database đã được tạo
4. .NET SDK đã cài đặt
5. Packages đã được restore

## Video Demo

Các bước demo:
1. Khởi động ứng dụng
2. Khởi tạo database
3. Thêm dữ liệu mẫu
4. Xem dữ liệu gốc
5. Xem dữ liệu che giấu (masked)
6. Mã hóa dữ liệu (AES/RSA/Hybrid)
7. Xem dữ liệu đã mã hóa
