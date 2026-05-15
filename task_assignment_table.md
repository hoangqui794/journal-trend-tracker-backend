# 🚀 Bảng Phân Công Nhiệm Vụ 5 Microservices (Final)

> **Kiến trúc:** Microservices | **Framework:** .NET 8.0 (Clean Architecture) | **Database:** PostgreSQL
> **Gateway:** YARP | **Message:** HTTP trực tiếp giữa services | **Auth:** JWT + Google OAuth 2.0

Mỗi người làm chủ 1 Service và 1 Database riêng biệt.
**Cấm tuyệt đối** truy cập trực tiếp vào Database của người khác — giao tiếp qua HTTP API call.

---

## 🧑‍💻 Thành viên 1 (P1): `IdentityService` & API Gateway

**Database phụ trách:** `identity_db`
**Bảng:** `users`, `refresh_tokens`
**Vai trò:** Bảo mật, xác thực, phân quyền và định tuyến toàn bộ request qua YARP Gateway.

### API cần code

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/identity/register` | Đăng ký tài khoản mới (BCrypt hash password) |
| POST | `/api/identity/login` | Đăng nhập email/password, trả JWT + Refresh Token |
| POST | `/api/identity/refresh` | Làm mới Access Token khi hết hạn |
| POST | `/api/identity/logout` | Thu hồi Refresh Token (đăng xuất) |
| GET  | `/api/identity/auth/google` | Redirect sang trang đăng nhập Google |
| GET  | `/api/identity/auth/google/callback` | Nhận callback từ Google, cấp JWT |
| GET  | `/api/identity/users/{id}` | Lấy thông tin user — **internal only** (các service khác gọi) |
| PUT  | `/api/identity/users/{id}/status` | Khoá / Mở khoá tài khoản — **internal only** (AdminService gọi) |

### Task Gateway (YARP)
- Cấu hình routing tất cả `/api/*` đến đúng service theo port
- Middleware xác thực JWT tại Gateway trước khi forward request
- Cấu hình CORS, rate limiting

### Lưu ý
- Dùng thư viện `Google.Apis.Auth` để verify Google ID Token
- Access Token TTL: 15 phút | Refresh Token TTL: 7 ngày
- 3 role: `researcher`, `lecturer`/`student`, `admin`

---

## 🧑‍💻 Thành viên 2 (P2): `PaperService` & Sync Worker

**Database phụ trách:** `paper_db`
**Bảng:** `papers`, `authors`, `keywords`, `journals`, `paper_authors`, `paper_keywords`, `api_sync_jobs`, `sync_cursors`, `sync_errors`
**Vai trò:** Trái tim hệ thống — tìm kiếm bài báo, đồng bộ dữ liệu từ API ngoài, lưu lịch sử tìm kiếm.

### API cần code

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET  | `/api/papers` | Tìm kiếm bài báo (`?keyword=&year=&journalId=&authorId=&source=&page=&pageSize=`) |
| GET  | `/api/papers/{id}` | Chi tiết 1 bài báo kèm tác giả, từ khóa, journal |
| GET  | `/api/papers/keywords` | Danh sách từ khóa gợi ý (dùng cho bộ lọc + autocomplete) |
| GET  | `/api/papers/journals` | Danh sách tạp chí |
| GET  | `/api/papers/authors` | Danh sách tác giả |
| POST | `/api/papers/search-history` | Lưu lịch sử tìm kiếm của user (tự gọi nội bộ sau mỗi search) |
| GET  | `/api/papers/sync-jobs` | Xem lịch sử sync — **internal only** (AdminService gọi) |

### Sync Worker (BackgroundService)
- Chạy **cronjob lúc 00:00 mỗi đêm** gọi OpenAlex API và Semantic Scholar API
- Kéo metadata bài báo mới về lưu vào `paper_db`
- Ghi log vào `api_sync_jobs`, lưu cursor vào `sync_cursors` để tiếp tục nếu bị gián đoạn
- Sau khi sync xong → **gọi HTTP sang UserService** (`POST /api/users/notifications/trigger`) để trigger notification cho user đang follow keyword có bài mới

### Lưu ý
- Gọi `POST /api/papers/search-history` tự động sau mỗi request search (không cần FE gọi)
- Chỉ lưu metadata (title, abstract, keywords, year, authors, journal) — không lưu full-text
- Dùng `sync_cursors` để lưu offset/page token, tránh sync lại từ đầu khi bị lỗi giữa chừng

---

## 🧑‍💻 Thành viên 3 (P3): `TrendService`

**Database phụ trách:** `trend_db`
**Bảng:** `trend_snapshots`, `journal_trend_snapshots`, `search_history`, `report_cache`
**Vai trò:** Tổng hợp số liệu, vẽ biểu đồ dashboard, export báo cáo.

### API cần code

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/trends/keywords/{keywordId}` | Số bài báo theo từng năm của 1 keyword → FE vẽ Line Chart |
| GET | `/api/trends/journals/{journalId}` | Số bài báo theo từng năm của 1 journal → FE vẽ Line Chart |
| GET | `/api/trends/top-keywords` | Top 10 keyword nổi bật nhất (theo số bài + tăng trưởng) |
| GET | `/api/trends/top-authors` | Top 10 tác giả có nhiều bài xuất bản nhất |
| GET | `/api/trends/top-journals` | Top 10 tạp chí phổ biến nhất |
| GET | `/api/trends/overview` | Số liệu tổng quan: tổng bài báo, tổng tác giả, tổng keyword |
| GET | `/api/trends/hot-topics` | Các chủ đề đang nổi bật dựa trên search_history |
| GET | `/api/trends/reports/export` | Export báo cáo xu hướng ra file CSV hoặc Excel (`?format=csv&keywordId=`) |

### Lưu ý quan trọng
- **KHÔNG** đọc thẳng `paper_db` — gọi API sang PaperService để lấy data
- Tính toán xong lưu kết quả vào `trend_snapshots` để dùng lại (cache)
- `search_history` trong `trend_db` nhận data từ PaperService gửi sang sau mỗi lần user search
- Dùng `report_cache` để cache kết quả export, tránh tính lại nhiều lần
- Dùng thư viện `ClosedXML` để xuất file Excel

---

## 🧑‍💻 Thành viên 4 (P4): `UserService` (Tương tác & Thông báo)

**Database phụ trách:** `user_db`
**Bảng:** `user_profiles`, `bookmarks`, `follows`, `notifications`, `email_queue`
**Vai trò:** Xử lý tương tác cá nhân của người dùng, quản lý thông báo và gửi email.

### API cần code

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET  | `/api/users/profile` | Lấy thông tin Profile người dùng |
| PUT  | `/api/users/profile` | Cập nhật Profile (bio, institution, research fields) |
| POST | `/api/users/bookmarks` | Thêm bookmark (paper / keyword / journal) |
| DELETE | `/api/users/bookmarks/{id}` | Xóa bookmark |
| GET  | `/api/users/bookmarks` | Danh sách bookmark của user (có filter theo entity_type) |
| POST | `/api/users/follows/keywords/{keywordId}` | Follow 1 keyword |
| POST | `/api/users/follows/journals/{journalId}` | Follow 1 journal |
| DELETE | `/api/users/follows/{id}` | Unfollow |
| GET  | `/api/users/follows` | Danh sách đang follow |
| GET  | `/api/users/notifications` | Danh sách thông báo (có filter is_read) |
| PUT  | `/api/users/notifications/{id}/read` | Đánh dấu 1 thông báo đã đọc |
| PUT  | `/api/users/notifications/read-all` | Đánh dấu tất cả đã đọc |
| POST | `/api/users/notifications/trigger` | **Internal only** — PaperService gọi để tạo notification khi có bài mới |

### Lưu ý
- Khi nhận trigger từ PaperService: tìm tất cả user đang follow keyword đó → tạo notification trong `notifications` + thêm vào `email_queue`
- Background job nhỏ trong UserService xử lý `email_queue` → gửi email thật qua SMTP (dùng `MailKit`)
- `user_id` trong `user_db` không có FK sang `identity_db` — validate user tồn tại bằng cách gọi `GET /api/identity/users/{id}`

---

## 🧑‍💻 Thành viên 5 (P5): `AdminService` & DevOps

**Database phụ trách:** `admin_db`
**Bảng:** `api_sources`, `system_settings`, `audit_logs`
**Vai trò:** Quản trị hệ thống và triển khai toàn bộ dự án bằng Docker.

### API cần code

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET  | `/api/admin/users` | Danh sách người dùng — gọi sang IdentityService |
| PUT  | `/api/admin/users/{id}/toggle` | Khoá / Mở khoá tài khoản — gọi sang IdentityService |
| GET  | `/api/admin/api-sources` | Danh sách nguồn đồng bộ (OpenAlex, SemanticScholar...) |
| PUT  | `/api/admin/api-sources/{id}/toggle` | Bật / Tắt đồng bộ 1 nguồn |
| GET  | `/api/admin/sync-jobs` | Xem lịch sử sync — gọi sang PaperService |
| GET  | `/api/admin/settings` | Lấy cấu hình hệ thống |
| PUT  | `/api/admin/settings` | Lưu cấu hình mới |
| GET  | `/api/admin/logs` | Xem audit logs — lịch sử thao tác của admin |

### Task DevOps (Docker)
- Viết `docker-compose.yml` chạy đồng thời:
  - 5 PostgreSQL (mỗi service 1 container, 1 port riêng)
  - 5 .NET Web API
  - 1 YARP Gateway
- Cấu hình `.env` cho tất cả connection strings và secrets
- Viết `README.md` hướng dẫn clone và chạy dự án bằng 1 lệnh `docker compose up`
- Đảm bảo health check cho từng service

---

## 🗂️ Tổng hợp

### Database

| Service | DB | Bảng chính |
|---|---|---|
| IdentityService | identity_db | users, refresh_tokens |
| PaperService | paper_db | papers, authors, keywords, journals, paper_authors, paper_keywords, sync_jobs |
| TrendService | trend_db | trend_snapshots, journal_trend_snapshots, search_history, report_cache |
| UserService | user_db | user_profiles, bookmarks, follows, notifications, email_queue |
| AdminService | admin_db | api_sources, system_settings, audit_logs |

### Giao tiếp giữa các Service

| Từ | → Đến | Mục đích | Khi nào |
|---|---|---|---|
| YARP Gateway | IdentityService | Xác thực JWT | Mọi request có Bearer token |
| PaperService | UserService | Trigger notification bài mới | Sau mỗi lần sync thành công |
| PaperService | TrendService | Gửi search_history | Sau mỗi lần user search |
| TrendService | PaperService | Lấy data tính trend | Khi tính snapshot |
| UserService | IdentityService | Validate user tồn tại | Khi tạo profile mới |
| AdminService | IdentityService | Quản lý user (khoá/mở) | Khi admin thao tác |
| AdminService | PaperService | Xem sync job logs | Khi admin xem dashboard |

### Checklist Functional Requirements

| Yêu cầu đề | Service xử lý | Trạng thái |
|---|---|---|
| User authentication & authorization | P1 | ✅ |
| Google OAuth login | P1 | ✅ |
| Search papers by keyword/author/journal | P2 | ✅ |
| View paper details | P2 | ✅ |
| Track publication trends | P3 | ✅ |
| Display charts & dashboard | P3 | ✅ |
| View trending research topics | P3 | ✅ |
| Save bookmarks | P4 | ✅ |
| Follow journals/topics | P4 | ✅ |
| Receive notifications | P4 | ✅ |
| Generate analytical reports | P3 | ✅ |
| Export report (CSV/Excel) | P3 | ✅ |
| Search history tracking | P2 | ✅ |
| Sync data from external APIs | P2 | ✅ |
| Manage users & system config (Admin) | P5 | ✅ |
