# Connectify - Mạng Xã Hội Thời Gian Thực (ASP.NET Core MVC & SignalR)

> 🌐 **Website trực tuyến (Live Demo):** [http://socialweb.runasp.net/Auth/Login](http://socialweb.runasp.net/Auth/Login)  
> 
> 🔑 **Tài khoản dùng thử (mặc định):**
> * **Người dùng mẫu 1**: `vietanh` / Mật khẩu: `123456`
> * **Người dùng mẫu 2**: `lanhuong` / Mật khẩu: `123456`
> * **Người dùng mẫu 3**: `quanghuy` / Mật khẩu: `123456`

Connectify là một ứng dụng mạng xã hội thu nhỏ được xây dựng trên nền tảng **ASP.NET Core 6.0 MVC**, kết hợp **Entity Framework Core** để tương tác với cơ sở dữ liệu **Microsoft SQL Server**, và sử dụng **SignalR** để cập nhật trạng thái bài đăng, bình luận cũng như lượt thích hoàn toàn theo thời gian thực (Real-time). Giao diện ứng dụng được thiết kế tối ưu và hiện đại bằng **Bootstrap 5**.

---

## 🚀 Các Tính Năng Nổi Bật
*   **🌐 Trải nghiệm Trực tuyến**: Đã deploy lên môi trường Cloud tại [http://socialweb.runasp.net/Auth/Login](http://socialweb.runasp.net/Auth/Login).
*   **Đăng ký, Đăng nhập & Bảo mật**: Quản lý tài khoản an toàn thông qua mã hoá mật khẩu SHA-256, lưu trữ phiên đăng nhập (Session) và chức năng **Quên mật khẩu / Khôi phục mật khẩu qua Email (OTP)**.
*   **Đăng trạng thái đa phương tiện**: Chia sẻ bài viết với văn bản, hỗ trợ tải lên **nhiều hình ảnh** (tối đa 5 ảnh) và **video clip ngắn**.
*   **Tương tác thời gian thực (SignalR)**: Thích/thả tim bài viết, đăng bình luận tức thời hiển thị ngay cho mọi người đang online.
*   **Chỉnh sửa & Quản lý bài viết**: Chỉnh sửa nội dung bài viết đã đăng, xoá bài viết, **Lưu bài viết yêu thích** và xem lại ở trang **Bài viết đã lưu**.
*   **Hồ sơ cá nhân & Thống kê tương tác**: Cập nhật ảnh đại diện, tiểu sử; các ô thống kê (bài viết, bạn bè) có thể bấm trực tiếp để xem danh sách; hỗ trợ xem trang cá nhân công khai của người dùng khác.
*   **Quản lý Bạn bè & Nhắn tin**: Đề xuất kết bạn, gửi/hủy yêu cầu kết bạn, nhắn tin trò chuyện 1-1 theo thời gian thực (hỗ trợ gửi ảnh trong tin nhắn).
*   **Báo cáo vi phạm & Bảng quản trị Admin**: Báo cáo bài viết vi phạm nội dung; Quản trị viên theo dõi thống kê toàn hệ thống, kiểm duyệt báo cáo, khoá/mở khoá tài khoản người dùng với lý do chi tiết.

---

## 🛠 Yêu Cầu Hệ Thống
Trước khi cài đặt, hãy đảm bảo máy tính của bạn đã cài sẵn:
1.  **[.NET SDK 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)** hoặc mới hơn.
2.  **[Microsoft SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)** (Hỗ trợ LocalDB hoặc SQL Server Express).
3.  Công cụ dòng lệnh **Entity Framework Core CLI**:
    ```bash
    dotnet tool install --global dotnet-ef --version 6.0.25
    ```

---

## ⚙️ Cài Đặt & Cấu Hình Cơ Sở Dữ Liệu

### Bước 1: Clone (Tải) mã nguồn về máy
```bash
git clone https://github.com/lehoanglh0503-sketch/SocialWebNetWorking.git
cd SocialWebNetWorking/Social\ Website
```

### Bước 2: Cấu hình chuỗi kết nối Database
Mở tệp `appsettings.json` trong thư mục dự án và kiểm tra chuỗi kết nối SQL Server của bạn tại khóa `"DefaultConnection"`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=SocialWebsiteDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```
*(Nếu sử dụng phiên bản SQL Server Express hoặc máy chủ từ xa, vui lòng thay đổi giá trị `Server` và thông tin tài khoản cho phù hợp).*

### Bước 3: Tạo và cập nhật cơ sở dữ liệu
Chạy lệnh sau tại thư mục chứa file `.csproj` để khởi tạo cấu trúc bảng biểu cơ sở dữ liệu trong SQL Server:
```bash
dotnet ef database update
```
*Lưu ý: Hệ thống sẽ tự động chạy tiến trình `SeedData` khi dự án khởi động lần đầu để tạo sẵn 3 người dùng mẫu cùng các bài viết và bình luận liên quan:*
*   **Tài khoản 1**: `vietanh` (mật khẩu: `123456`)
*   **Tài khoản 2**: `lanhuong` (mật khẩu: `123456`)
*   **Tài khoản 3**: `quanghuy` (mật khẩu: `123456`)

---

## 💻 Hướng Dẫn Khởi Chạy Ứng Dụng

Chạy lệnh sau để khởi động dự án:
```bash
dotnet run --launch-profile Social_Website
```
Ứng dụng sẽ bắt đầu chạy và lắng nghe kết nối tại:
*   **HTTPS**: `https://localhost:7117`
*   **HTTP**: `http://localhost:5224`

Mở trình duyệt bất kỳ và truy cập `http://localhost:5224/` để trải nghiệm.

---

## ⚡️ Trải Nghiệm Tính Năng Thời Gian Thực (SignalR)
Để kiểm tra tính năng thời gian thực hoạt động:
1.  Mở trình duyệt Chrome bình thường, đăng nhập tài khoản `vietanh`.
2.  Mở thêm một trình duyệt khác ở chế độ ẩn danh (hoặc Microsoft Edge), đăng nhập tài khoản `lanhuong`.
3.  Khi tài khoản `vietanh` đăng một bài viết mới hoặc bình luận/thích bài đăng, các hoạt động này sẽ **lập tức xuất hiện mượt mà** trên màn hình của `lanhuong` mà hoàn toàn không cần tải lại trang!