# HƯỚNG DẪN SỬ DỤNG BẢNG ĐIỂM ẢO

## Tổng quan
Tính năng Bảng điểm ảo cho phép sinh viên:
- Xem và chọn các môn học để tính CPA dự kiến
- Tự động bỏ qua môn Thể chất
- Lưu bảng điểm đã chọn lên server
- Xem bảng quy đổi điểm

## Yêu cầu
- Phải đăng nhập trước khi sử dụng
- Server phải được khởi động
- Có kết nối đến database kmalegend

## Cách sử dụng

### 1. Đăng nhập
- Từ form ScoreLookupForm, nhấn nút "📊 Bảng điểm ảo"
- Nếu chưa đăng nhập, hệ thống sẽ hiển thị form đăng nhập
- Nhập mã sinh viên và mật khẩu ACTVN Portal
- Sau khi đăng nhập thành công, form Bảng điểm ảo sẽ mở

### 2. Khởi động Server
- Nhấn nút "▶ START SERVER" ở góc trên bên phải
- Đợi cho đến khi trạng thái hiển thị "● RUNNING"
- Cửa sổ Server Log sẽ tự động mở để theo dõi

### 3. Tải điểm
- Nhấn nút "🔍 Tải điểm"
- Hệ thống sẽ:
  - Gửi request mã hóa (RSA + AES) đến server
  - Lấy danh sách điểm từ database
  - Hiển thị trong bảng với checkbox
  - Tự động bỏ chọn môn Thể chất
  - Tính toán GPA, CPA hiện tại

### 4. Chọn môn học
- Tích checkbox ở cột "Chọn" để chọn môn học tính vào CPA dự kiến
- Môn Thể chất sẽ bị vô hiệu hóa (màu xám)
- Môn trượt sẽ được đánh dấu màu đỏ
- Môn học kỳ hiện tại sẽ được đánh dấu màu vàng
- Nhấn "☑ Chọn tất cả" để chọn tất cả môn (trừ Thể chất)

### 5. Xem CPA dự kiến
- CPA dự kiến sẽ tự động cập nhật khi bạn chọn/bỏ chọn môn
- Màu sắc CPA dự kiến:
  - Xanh lá: CPA >= 3.6 (Xuất sắc)
  - Vàng: CPA >= 3.0 (Giỏi)
  - Cam: CPA >= 2.5 (Khá)
  - Đỏ: CPA < 2.5 (Trung bình)

### 6. Lưu lên Server
- Nhấn nút "💾 Lưu lên Server"
- Hệ thống sẽ lưu các môn đã chọn vào database
- Dữ liệu được lưu vào 2 bảng:
  - `score_batches`: Thông tin sinh viên và thời gian lưu
  - `score_items`: Chi tiết từng môn học đã chọn

### 7. Xem bảng quy đổi điểm
- Nhấn nút "📋 Bảng quy đổi"
- Xem bảng quy đổi điểm thang 10 sang thang 4 và điểm chữ

## Công thức tính điểm

### Quy đổi điểm sang thang 4
- 9.0 - 10.0 → 4.0 (A+)
- 8.5 - 8.9 → 3.8 (A)
- 7.8 - 8.4 → 3.5 (B+)
- 7.0 - 7.7 → 3.0 (B)
- 6.3 - 6.9 → 2.4 (C+)
- 5.5 - 6.2 → 2.0 (C)
- 4.8 - 5.4 → 1.5 (D+)
- 4.0 - 4.7 → 1.0 (D)
- 0.0 - 3.9 → 0.0 (F)

### Công thức tính CPA
```
CPA = Σ(Điểm thang 4 × Số tín chỉ) / Σ(Số tín chỉ)
```

### Điều kiện qua môn
- Điểm cuối kỳ >= 2.0
- Điểm tổng kết >= 4.0

### Loại trừ
- Môn Thể chất không tính vào CPA
- Chỉ tính các môn đã qua (passed)

## Cấu trúc Database

### Bảng score_batches
```sql
CREATE TABLE score_batches (
  batch_id BIGINT PRIMARY KEY AUTO_INCREMENT,
  student_code VARCHAR(255) NOT NULL,
  student_name VARCHAR(255) NOT NULL,
  student_class VARCHAR(255),
  last_updated DATETIME(6) NOT NULL
);
```

### Bảng score_items
```sql
CREATE TABLE score_items (
  item_id BIGINT PRIMARY KEY AUTO_INCREMENT,
  batch_id BIGINT NOT NULL,
  subject_name VARCHAR(255) NOT NULL,
  subject_credit INT NOT NULL,
  score_first FLOAT NOT NULL,
  score_second FLOAT NOT NULL,
  score_final FLOAT NOT NULL,
  score_overall FLOAT NOT NULL,
  score_text VARCHAR(255),
  is_selected BIT(1) NOT NULL,
  FOREIGN KEY (batch_id) REFERENCES score_batches(batch_id)
);
```

## Bảo mật
- Tất cả dữ liệu truyền giữa Client và Server đều được mã hóa
- Sử dụng RSA 1024-bit để trao đổi AES key
- Sử dụng AES-CBC để mã hóa dữ liệu
- Mật khẩu được cache trong session (không lưu vào database)

## Lưu ý
- Dữ liệu điểm được lấy từ database kmalegend
- Cần có quyền truy cập database để lưu bảng điểm ảo
- Session đăng nhập sẽ được giữ cho đến khi đăng xuất hoặc đóng ứng dụng
- Có thể mở nhiều form Bảng điểm ảo cùng lúc

## Troubleshooting

### Không tải được điểm
- Kiểm tra xem đã đăng nhập chưa
- Kiểm tra xem Server đã chạy chưa
- Kiểm tra kết nối database

### Không lưu được lên Server
- Kiểm tra quyền ghi vào database
- Kiểm tra xem có chọn ít nhất 1 môn chưa
- Xem log để biết chi tiết lỗi

### CPA dự kiến không đúng
- Kiểm tra xem đã chọn đúng các môn chưa
- Môn Thể chất không được tính
- Chỉ môn đã qua mới được tính
