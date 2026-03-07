# E-Commerce Razor Pages Shop (3-Layer Architecture & Real-Time)

Dự án Website Thương mại điện tử được xây dựng trên nền tảng ASP.NET Core Razor Pages, kết hợp SignalR cho các tính năng thời gian thực (Real-time). Dự án áp dụng chặt chẽ Mô hình 3 lớp (Three-Layer Architecture) để đảm bảo code sạch, dễ bảo trì và dễ mở rộng, cùng với SQL Server để quản lý cơ sở dữ liệu.

## 🚀 Công nghệ & Kiến trúc

### Công nghệ sử dụng
* Backend: ASP.NET Core Razor Pages (.NET 8)
* Real-time Communication: ASP.NET Core SignalR
* Database: SQL Server
* Frontend: Razor syntax (`.cshtml`), HTML5, CSS3, Bootstrap, JavaScript/jQuery.
* Architecture: 3-Layer Architecture (Web - BLL - DAL).

### 🏗️ Mô hình 3 lớp trong dự án
Hệ thống được thiết kế phân rã thành 3 tầng:

1.  Presentation Layer (Web / UI):
    * Sử dụng Razor Pages (`.cshtml` và `PageModel` trong `.cshtml.cs`) thay vì Controllers/Views.
    * Chứa các SignalR Hubs để duy trì kết nối WebSocket cho Chat và cập nhật dữ liệu.
2.  Business Logic Layer (BLL / Service):
    * Xử lý nghiệp vụ phức tạp: Tính toán đơn hàng, logic gửi mã xác thực (`ValidEmailByOtp`), kiểm tra user (`Validate user`).
    * Điều phối luồng dữ liệu Real-time (Ví dụ: Khi có người mua, BLL gọi SignalR Hub để broadcast cập nhật số lượng sản phẩm).
3.  Data Access Layer (DAL / Repository):
    * Giao tiếp trực tiếp với SQL Server.
    * Khởi tạo và cấu trúc Database dựa trên `CREATE DATABASE ShopDB.txt`.

## ✨ Tính năng nổi bật

### ⚡ Tính năng Real-time
* Chat Trực Tuyến (User - Admin): Hỗ trợ khách hàng nhắn tin trực tiếp với quản trị viên theo thời gian thực mà không cần tải lại trang.
* Cập nhật Sản phẩm Real-time: Khi có thay đổi về giá, trạng thái tồn kho hoặc có người vừa mua hàng, thông tin sản phẩm trên giao diện người dùng sẽ tự động đồng bộ ngay lập tức.

### 🛒 Chức năng E-Commerce Cốt lõi
* Xác thực người dùng:
    * Đăng ký, Đăng nhập an toàn.
    * Xác thực tài khoản qua Email OTP.
* Mua sắm & Đặt hàng:
    * Thêm/Sửa/Xóa sản phẩm trong giỏ hàng.
    * Checkout với tính năng chọn địa chỉ động (Dropdown Tỉnh/Thành phố, Quận/Huyện).
* Quản trị hệ thống (Admin): Quản lý sản phẩm, đơn hàng và hỗ trợ khách hàng qua Chat.

## 📂 Dữ liệu khởi tạo

Trong thư mục gốc chứa các file quan trọng để thiết lập Database:
* `CREATE DATABASE ShopDB.txt`: Script SQL chứa cấu trúc các bảng (Tables, Relations).
* `Data.txt`: Chứa dữ liệu mẫu (Seed Data) để test ứng dụng.

## 🛠️ Hướng dẫn cài đặt & Chạy dự án

### 1. Thiết lập Database
1. Mở SQL Server Management Studio (SSMS).
2. Chạy nội dung trong file `CREATE DATABASE ShopDB.txt` để tạo CSDL `ShopDB`.
3. Chạy script trong `Data.txt` để thêm dữ liệu ban đầu.

### 2. Cấu hình ứng dụng
Mở file `appsettings.json` và cập nhật chuỗi kết nối Database:

"ConnectionStrings": {
  "DefaultConnection": "server=(local); database=ShopDB; uid=sa; pwd=12345; TrustServerCertificate=True; Trusted_Connection=True;"
}