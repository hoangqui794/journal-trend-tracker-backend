# Database Scripts — Journal Tracking System (5 Services)

## Tổng quan

| File | Database | Service | Gộp từ |
|------|----------|---------|--------|
| 01_identity_db.sql | identity_db | IdentityService | Identity |
| 02_paper_db.sql | paper_db | PaperService | Paper + Sync |
| 03_trend_db.sql | trend_db | TrendService | Trend |
| 04_user_db.sql | user_db | UserService | User + Notification |
| 05_admin_db.sql | admin_db | AdminService | Admin |

## Cách chạy

### Bước 1 — Tạo databases
```bash
psql -U postgres -c "CREATE DATABASE identity_db;"
psql -U postgres -c "CREATE DATABASE paper_db;"
psql -U postgres -c "CREATE DATABASE trend_db;"
psql -U postgres -c "CREATE DATABASE user_db;"
psql -U postgres -c "CREATE DATABASE admin_db;"
```

### Bước 2 — Chạy scripts
```bash
psql -U postgres -d identity_db -f 01_identity_db.sql
psql -U postgres -d paper_db    -f 02_paper_db.sql
psql -U postgres -d trend_db    -f 03_trend_db.sql
psql -U postgres -d user_db     -f 04_user_db.sql
psql -U postgres -d admin_db    -f 05_admin_db.sql
```

## Connection strings (appsettings.json)

```json
{
  "ConnectionStrings": {
    "IdentityDb": "Host=localhost;Database=identity_db;Username=postgres;Password=yourpassword",
    "PaperDb":    "Host=localhost;Database=paper_db;Username=postgres;Password=yourpassword",
    "TrendDb":    "Host=localhost;Database=trend_db;Username=postgres;Password=yourpassword",
    "UserDb":     "Host=localhost;Database=user_db;Username=postgres;Password=yourpassword",
    "AdminDb":    "Host=localhost;Database=admin_db;Username=postgres;Password=yourpassword"
  }
}
```

## Lưu ý — Không có FK xuyên DB

Microservice không dùng FOREIGN KEY giữa các database.
Các UUID tham chiếu sang DB khác được ghi chú rõ trong comment.

Ví dụ:
- `user_db.bookmarks.user_id` → ref `identity_db.users.id` (validate qua API call)
- `trend_db.trend_snapshots.keyword_id` → ref `paper_db.keywords.id`
- `admin_db.audit_logs.admin_user_id` → ref `identity_db.users.id`
