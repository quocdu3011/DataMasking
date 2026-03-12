# 📅 Hướng dẫn sử dụng Calendar View

## Tổng quan

Calendar View là giao diện lịch học hiện đại với 2 chế độ xem:
- **Month View** (Xem tháng) - Mặc định
- **Week View** (Xem tuần)

## Tính năng chính

### 1. Month View (Xem tháng)
- Hiển thị toàn bộ tháng với các ngày có lịch học
- Mỗi môn học được hiển thị dưới dạng badge màu sắc
- Click vào badge để xem chi tiết
- Ngày hôm nay được highlight màu xanh
- Nếu có quá nhiều môn trong 1 ngày, hiển thị "+X môn khác"

### 2. Week View (Xem tuần)
- Hiển thị lịch theo tuần (Thứ 2 - Chủ nhật)
- Chia theo 4 khung giờ chính:
  - 07:00 - 09:35
  - 09:35 - 12:30
  - 12:30 - 15:00
  - 15:00 - 18:00
- Event cards hiển thị đầy đủ thông tin
- Click vào card để xem chi tiết

### 3. Navigation (Điều hướng)
- **◀ / ▶**: Chuyển tháng/tuần trước/sau
- **Hôm nay**: Quay về tháng/tuần hiện tại
- **📅 Tháng**: Chuyển sang Month View
- **📆 Tuần**: Chuyển sang Week View

### 4. Xuất file ICS
- Click nút **💾 Xuất ICS**
- Chọn vị trí lưu file
- File ICS có thể import vào:
  - Google Calendar
  - Microsoft Outlook
  - Apple Calendar
  - Các ứng dụng lịch khác

### 5. Xem JSON
- Click nút **🔍 JSON** để xem raw data
- Có thể copy JSON để debug hoặc phân tích

## Màu sắc

- Mỗi môn học có màu riêng (tự động generate)
- Màu được tạo dựa trên mã môn học
- Hover vào event để màu sáng lên

## Chi tiết event

Click vào bất kỳ event nào để xem popup chi tiết:
- Tên môn học
- Mã môn
- Ngày (với tên thứ)
- Thời gian
- Tiết học
- Phòng học
- Giảng viên

## Bảo mật

- Ngày sinh được mã hóa: `**/**/năm`
- Chỉ hiển thị năm sinh, ẩn ngày và tháng

## Shortcuts

- **Maximized**: Form tự động maximize khi mở
- **Responsive**: Tự động điều chỉnh kích thước theo màn hình
- **Scroll**: Tự động scroll nếu nội dung quá dài

## Tips

1. Dùng Month View để xem tổng quan
2. Dùng Week View để xem chi tiết lịch tuần
3. Xuất ICS để đồng bộ với các ứng dụng lịch khác
4. Click vào "+X môn khác" để xem danh sách đầy đủ
