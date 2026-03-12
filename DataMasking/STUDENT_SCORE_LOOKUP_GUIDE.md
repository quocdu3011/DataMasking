# 🎓 Hệ thống Tra cứu Điểm thi Sinh viên

## Tổng quan
Hệ thống tra cứu điểm thi sinh viên với tính năng Data Masking để bảo vệ thông tin cá nhân khi tra cứu online.

## Cấu hình Database

### Thông tin kết nối
- **Server**: 36.50.54.109
- **Port**: 3306
- **Database**: kmalegend
- **Username**: anonymous
- **Password**: 1

### Cấu trúc Database

#### Bảng `student`
```sql
CREATE TABLE student (
    student_id BIGINT PRIMARY KEY AUTO_INCREMENT,
    student_code VARCHAR(50) NOT NULL,
    student_name VARCHAR(255) NOT NULL,
    student_class VARCHAR(100)
);
```

#### Bảng `subject`
```sql
CREATE TABLE subject (
    subject_id BIGINT PRIMARY KEY AUTO_INCREMENT,
    subject_name VARCHAR(255) NOT NULL,
    subject_credits BIGINT
);
```

#### Bảng `score`
```sql
CREATE TABLE score (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    student_id BIGINT,
    subject_id BIGINT,
    score_text VARCHAR(10),
    score_first FLOAT NOT NULL,
    score_second FLOAT NOT NULL,
    score_final FLOAT NOT NULL,
    score_over_rall FLOAT NOT NULL,
    semester VARCHAR(50),
    FOREIGN KEY (student_id) REFERENCES student(student_id),
    FOREIGN KEY (subject_id) REFERENCES subject(subject_id)
);
```

## Tính năng

### 1. Kiến trúc Client-Server
- Server chạy trên port 8888
- Giao tiếp qua TCP với mã hóa RSA + AES
- Server xử lý tra cứu và tính toán GPA/CPA
- Client gửi request và nhận response đã được mã hóa

### 2. Bảo mật
- **RSA 1024-bit**: Mã hóa AES key
- **AES 256-bit**: Mã hóa dữ liệu truyền tải
- **Hybrid Encryption**: Kết hợp RSA + AES cho hiệu suất tối ưu
- **Data Masking**: Che giấu thông tin sinh viên (tên và lớp)

### 3. Tra cứu điểm theo mã sinh viên
- Nhập mã sinh viên vào ô tìm kiếm
- Chọn phương thức Data Masking
- Nhấn nút "Tra cứu"

### 4. Data Masking</Hệ thống hỗ trợ 4 phương thức masking cho thông tin sinh viên (tên và lớp):

1. **Che mặt nạ ký tự**: Ẩn một phần ký tự bằng dấu *
2. **Xáo trộn dữ liệu**: Đảo ngẫu nhiên vị trí các ký tự
3. **Thay thế bằng dữ liệu giả**: Thay thế bằng dữ liệu giả mạo
4. **Thêm nhiễu vào ký tự số**: Thêm nhiễu vào các ký tự số

### 5. Hiển thị điểm
Hệ thống hiển thị đầy đủ thông tin điểm:
- Học kỳ
- Môn học
- Tín chỉ
- Điểm chuyên cần (CC)
- Điểm giữa kỳ (GK)
- Điểm cuối kỳ (CK)
- Điểm tổng kết (TK)
- Điểm chữ

### 6. Tính toán GPA và CPA

#### Logic qua/trượt môn
- **Trượt**: Điểm cuối kỳ < 2.0
- **Trượt**: Điểm cuối kỳ >= 2.0 nhưng điểm tổng kết < 4.0
- **Qua**: Điểm cuối kỳ >= 2.0 VÀ điểm tổng kết >= 4.0

#### GPA (Grade Point Average) - Điểm trung bình học kỳ
- Tính cho học kỳ hiện tại (semester cuối cùng)
- **Không tính** môn Thể chất
- Công thức: `GPA = Σ(Điểm × Tín chỉ) / Σ(Tín chỉ)`

#### CPA (Cumulative Point Average) - Điểm trung bình tích lũy
- Tính cho tất cả các học kỳ
- **Không tính** môn Thể chất
- Công thức: `CPA = Σ(Điểm × Tín chỉ) / Σ(Tín chỉ)`

#### Tổng tín chỉ hoàn thành
- Tính tất cả các môn đã qua (điểm >= 4.0)
- **Có tính** môn Thể chất

### 5. Quy đổi điểm sang thang điểm 4
| Điểm số | Điểm chữ | Điểm 4 |
|---------|----------|--------|
| 8.5-10  | A        | 4.0    |
| 8.0-8.4 | B+       | 3.5    |
| 7.0-7.9 | B        | 3.0    |
| 6.5-6.9 | C+       | 2.5    |
| 5.5-6.4 | C        | 2.0    |
| 5.0-5.4 | D+       | 1.5    |
| 4.0-4.9 | D        | 1.0    |
| < 4.0   | F        | 0.0    |

## Cách sử dụng

1. Khởi động ứng dụng
2. Nhập mã sinh viên (ví dụ: CT050101)
3. Chọn phương thức masking mong muốn
4. Nhấn "Tra cứu"
5. Xem thông tin sinh viên (đã được masking), điểm số, GPA, CPA

## Lưu ý

- Thông tin sinh viên (tên và lớp) được masking để bảo vệ quyền riêng tư khi tra cứu online
- Môn Thể chất không được tính vào GPA/CPA nhưng vẫn tính vào tổng tín chỉ hoàn thành
- GPA chỉ tính cho học kỳ hiện tại (semester cuối cùng)
- CPA tính cho toàn bộ quá trình học tập

## Thay đổi so với hệ thống cũ

- ✅ Ẩn LoginForm (tạm thời không sử dụng)
- ✅ Thay thế Main form bằng ScoreLookupForm
- ✅ Kết nối database "kmalegend" thay vì "datamasking_db"
- ✅ Sử dụng các entity Student, Subject, Score
- ✅ Tính toán GPA/CPA tự động
- ✅ Data masking cho thông tin sinh viên
