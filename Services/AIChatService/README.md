# AIChatService - Trợ lý AI Hỏi Đáp (Nhiệm vụ 4)

Dịch vụ cung cấp khả năng trò chuyện thông minh với AI (RAG) dựa trên nội dung các bài báo khoa học và tài liệu học thuật.

## 🚀 Tính năng chính
- Quản lý phiên trò chuyện (Chat Sessions).
- Lưu trữ lịch sử tin nhắn (Chat History).
- Tích hợp **Google Gemini 3.1 Flash Lite Preview** để trả lời câu hỏi.
- Hỗ trợ **RAG (Retrieval-Augmented Generation)**: AI trả lời dựa trên ngữ cảnh tài liệu.

## 🏗️ Kiến trúc hệ thống
Dịch vụ được xây dựng theo kiến trúc 3 lớp (3-Layer Architecture) kết hợp Repository Pattern:
- **Presentation Layer**: `Controllers` (API Endpoints) & `DTOs`.
- **Business Logic Layer**: `ChatService` (Xử lý nghiệp vụ & Logic AI).
- **Data Access Layer**: `ChatRepository`, `Models` & `AIChatDbContext`.

## 🛠️ Công nghệ sử dụng
- **.NET 8.0** (Web API)
- **Entity Framework Core**
- **PostgreSQL** (Database chính)
- **InMemory Database** (Dùng cho quá trình Test nhanh)
- **Google Gemini API** (Mô hình ngôn ngữ lớn)

## 📡 Danh sách API
| Method | Endpoint | Mô tả |
| :--- | :--- | :--- |
| `POST` | `/api/chat/sessions` | Tạo phiên chat mới (Gắn DocumentId) |
| `GET` | `/api/chat/sessions` | Lấy danh sách phiên chat của User |
| `GET` | `/api/chat/sessions/{id}/messages` | Lấy lịch sử tin nhắn của phiên |
| `POST` | `/api/chat/ask` | Gửi câu hỏi và nhận phản hồi từ AI |
| `DELETE` | `/api/chat/sessions/{id}` | Xóa lịch sử phiên chat |

## 🧪 Hướng dẫn Test nhanh (In-Memory)
Hiện tại, dự án đang cấu hình sử dụng **In-Memory Database** để bạn có thể chạy ngay mà không cần cài đặt SQL:
1. Mở terminal tại thư mục này.
2. Chạy lệnh: `dotnet run`
3. Truy cập Swagger: `https://localhost:[PORT]/swagger`
4. Dùng `ChatRequest` có kèm trường `context` để test khả năng đọc tài liệu của AI.

## 🔗 Lưu ý tích hợp (Dành cho nhóm)
1. **Auth Integration**: Cần thay thế `MockUserId` bằng thông tin từ `ClaimsPrincipal` sau khi có JWT từ AuthService.
2. **Document Integration**: Cần kết nối với `DocumentService` để tự động lấy `Abstract` của tài liệu khi người dùng không gửi kèm `context` thủ công.
3. **Database**: Khi triển khai thực tế, hãy đổi từ `UseInMemoryDatabase` sang `UseNpgsql` trong file `Program.cs`.

---
*Người thực hiện: Thành viên 4*
