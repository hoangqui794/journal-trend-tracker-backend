# Scientific Journal Publication Trend Tracking System

Hệ thống theo dõi xu hướng xuất bản bài báo khoa học. Dự án được xây dựng theo kiến trúc **Microservices** trên nền tảng **.NET 8.0** kết hợp với **PostgreSQL** để lưu trữ dữ liệu.

---

## 1. Context (Bối cảnh)
Trong bối cảnh số lượng bài báo khoa học và journal học thuật ngày càng gia tăng, việc theo dõi xu hướng nghiên cứu, chủ đề nổi bật và sự phát triển của các lĩnh vực học thuật trở nên khó khăn đối với giảng viên, sinh viên và nhà nghiên cứu. Các nền tảng học thuật hiện nay chủ yếu hỗ trợ tìm kiếm bài báo nhưng chưa tập trung nhiều vào việc phân tích xu hướng công bố theo thời gian và trực quan hóa dữ liệu nghiên cứu.

## 2. Problems (Vấn đề)
- Khó theo dõi sự thay đổi và phát triển của các chủ đề nghiên cứu theo thời gian do số lượng bài báo khoa học ngày càng lớn.
- Việc tìm kiếm bài báo trên các nền tảng học thuật hiện nay chủ yếu dựa trên keyword, chưa hỗ trợ phân tích xu hướng nghiên cứu một cách trực quan.
- Giảng viên, sinh viên và nhà nghiên cứu mất nhiều thời gian để xác định các chủ đề đang nổi bật hoặc có tiềm năng nghiên cứu.

## 3. Primary Actors (Đối tượng sử dụng chính)
- **Researcher (Nhà nghiên cứu):** Phân tích xu hướng nghiên cứu, theo dõi journal và keyword chuyên sâu, khám phá các chủ đề mới nổi, và xem thống kê công bố theo thời gian.
- **Lecturer / Student (Giảng viên / Sinh viên):** Tìm kiếm bài báo tham khảo, khám phá các chủ đề phổ biến, lưu bài báo hoặc keyword quan tâm, và xem dashboard xu hướng cơ bản.
- **System Administrator (Quản trị hệ thống):** Quản lý tài khoản người dùng, cấu hình nguồn dữ liệu API, cập nhật dữ liệu bài báo và quản lý hệ thống.

---

## 4. Functional Requirements (Yêu cầu chức năng)

1. **Authentication (Xác thực)**
   - Đăng ký tài khoản, Đăng nhập, Đăng xuất.
   - Quên mật khẩu, Cập nhật Profile cá nhân.

2. **Bookmark & Follow (Tương tác)**
   - Bookmark các bài báo hay để đọc sau.
   - Follow (Theo dõi) Journal hoặc Keyword cụ thể.
   - Nhận Notification (thông báo) khi có bài báo mới xuất bản thuộc Keyword/Journal đã follow.

3. **Search & Filter (Tìm kiếm & Bộ lọc)**
   - Tìm kiếm bài báo theo Title, Author, Keyword.
   - Lọc bài báo theo Năm xuất bản, Journal, Lĩnh vực nghiên cứu.

4. **Dashboard & Trends (Thống kê & Xu hướng)**
   - Xem biểu đồ xu hướng keyword / số lượng bài báo xuất bản theo thời gian.
   - Xem bảng xếp hạng (Top) các bài báo và tác giả nổi bật nhất.

5. **Admin (Quản trị hệ thống)**
   - Quản lý người dùng và phân quyền.
   - Quản lý cấu hình nguồn dữ liệu API học thuật (Bật/tắt đồng bộ).
   - Xem Audit Logs ghi vết thao tác hệ thống.

---

## 5. Notes (Ghi chú hệ thống - Tích hợp API học thuật)
- Hệ thống sử dụng dữ liệu công khai từ các nguồn học thuật như **Semantic Scholar, OpenAlex hoặc Crossref** thông qua API miễn phí.
- Chỉ thu thập siêu dữ liệu (metadata) của bài báo: `Title`, `Abstract`, `Authors`, `Publication Year`, `Journal`.
- Không xử lý toàn văn (full-text) của bài báo do giới hạn bản quyền và dung lượng dữ liệu.
- Tần suất cập nhật dữ liệu chạy tự động theo chu kỳ định kỳ (Background Worker / Cronjob mỗi ngày/tuần), **KHÔNG yêu cầu realtime**.

---

## 6. Kiến trúc Hệ Thống (5 Microservices)

Dự án áp dụng triệt để mô hình **Database-per-service** (Mỗi service chạy một DB PostgreSQL độc lập, không có khóa ngoại ràng buộc lẫn nhau).

1. **P1: Identity Service (`identity_db`)**
   - Phụ trách Xác thực (Login/Register), cấp phát Token (JWT). Kết hợp cấu hình API Gateway để phân luồng request.
2. **P2: Paper Service (`paper_db`)**
   - Lưu trữ metadata bài báo, xử lý các nghiệp vụ Tìm kiếm/Lọc dữ liệu. Viết Worker tự động Sync dữ liệu từ Semantic Scholar.
3. **P3: Trend Service (`trend_db`)**
   - Chịu trách nhiệm tổng hợp, tính toán dữ liệu và trả về số liệu cho Dashboard vẽ Biểu đồ.
4. **P4: User Service (`user_db`)**
   - Quản lý hành vi User (Bookmark, Follow) và hệ thống đẩy thông báo Notification.
5. **P5: Admin Service (`admin_db`)**
   - Cấu hình API Sources, quản lý Audit Logs. Chịu trách nhiệm cấu hình Docker/DevOps khi deploy.

---

## 7. Yêu cầu cài đặt
- [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Docker & Docker Compose](https://www.docker.com/products/docker-desktop/)
- IDE: Visual Studio 2022 hoặc Visual Studio Code.

---

## 8. Hướng dẫn khởi chạy (Local)

### Bước 1: Khởi tạo Database
Tất cả 5 database được cấu hình tự động chạy thông qua Docker Compose:
```bash
docker-compose up -d
```

### Bước 2: Khởi chạy Microservices
Mở Solution `.sln` bằng Visual Studio và cấu hình **Multiple Startup Projects** hoặc dùng Terminal chạy lệnh trong từng thư mục:
```bash
dotnet run
```
