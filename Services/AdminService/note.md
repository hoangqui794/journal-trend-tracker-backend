1. `GET /api/admin/users`  
Công dụng: Lấy danh sách user để Admin quản lý.  
Thực tế: API này gọi sang `IdentityService` (`/api/identity/users`), không đọc DB local.

2. `PUT /api/admin/users/{id}/toggle`  
Công dụng: Khóa/mở khóa tài khoản user (`active` <-> `locked`).  
Thực tế:  
- Gọi `IdentityService` lấy status hiện tại của user.  
- Gọi tiếp API update status bên `IdentityService`.  
- Ghi `audit_logs` trong `admin_db`.

3. `GET /api/admin/api-sources`  
Công dụng: Xem danh sách nguồn dữ liệu học thuật (OpenAlex, SemanticScholar, Crossref...), trạng thái bật/tắt, chu kỳ sync.  
Thực tế: Đọc bảng `api_sources` trong `admin_db`.

4. `PUT /api/admin/api-sources/{id}/toggle`  
Công dụng: Bật/tắt một nguồn sync dữ liệu.  
Thực tế:  
- Đảo `is_active` trong `api_sources`.  
- Ghi log thao tác vào `audit_logs`.

5. `GET /api/admin/sync-jobs`  
Công dụng: Xem lịch sử job đồng bộ dữ liệu bài báo.  
Thực tế: API này gọi sang `PaperService` (`/api/papers/sync-jobs`), không đọc DB local.

6. `GET /api/admin/settings`  
Công dụng: Lấy toàn bộ cấu hình hệ thống (giới hạn search, lịch snapshot, email_from...).  
Thực tế: Đọc bảng `system_settings`.

7. `PUT /api/admin/settings`  
Công dụng: Tạo mới/cập nhật cấu hình hệ thống.  
Input: danh sách object `{ key, value, description }`.  
Thực tế:  
- Nếu `key` chưa có thì tạo mới.  
- Nếu đã có thì update.  
- Mỗi thay đổi đều ghi `audit_logs`.

8. `GET /api/admin/logs?limit=100`  
Công dụng: Xem nhật ký thao tác admin (ai làm gì, lúc nào, thay đổi trước/sau).  
Thực tế: Đọc bảng `audit_logs`, sắp xếp mới nhất trước, `limit` tối đa 500.

Ghi chú quan trọng:
- Các API gọi service khác (`/users`, `/users/{id}/toggle`, `/sync-jobs`) sẽ lỗi nếu `IdentityService` hoặc `PaperService` chưa chạy hoặc chưa có endpoint tương ứng.
- Khi service đích không truy cập được, hệ thống hiện trả `503` thay vì văng `500` raw exception.