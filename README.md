# LỬA MỘC – WEBSITE ĐẶT MÓN ĂN TRỰC TUYẾN

**Bài tập lớn môn Lập trình Web – Nhóm 12**

## 1. Giới thiệu

LỬA MỘC là hệ thống đặt món ăn trực tuyến dành cho mô hình nhà hàng kinh doanh giao hàng, cho phép khách hàng xem thực đơn, đặt món, thanh toán QR mô phỏng và theo dõi trạng thái đơn hàng.

Hệ thống hỗ trợ quản trị viên quản lý thực đơn, danh mục, combo, voucher, đơn hàng và giờ hoạt động.

## 2. Công nghệ sử dụng

- ASP.NET Core MVC (.NET 10)
- C#
- Entity Framework Core
- SQL Server
- HTML, CSS, JavaScript
- Visual Studio 2022
- SQL Server Management Studio (SSMS)

## 3. Hướng dẫn cài đặt

### Bước 1: Tải mã nguồn

Clone repository:

```bash
git clone https://github.com/Dwuonggg/BTL_LT_Web_Nhom_12.git
```

Hoặc chọn **Code → Download ZIP** trên GitHub và giải nén.

### Bước 2: Tạo cơ sở dữ liệu

1. Mở SQL Server Management Studio (SSMS).
2. Kết nối tới SQL Server.
3. Chọn **File → Open → File**.
4. Mở file `DatabaseScripts/LuaMocDB.sql`.
5. Nhấn **Execute (F5)** để tạo cơ sở dữ liệu và dữ liệu mẫu.

**Lưu ý:** Script dành cho SQL Server chưa có database `LuaMocDB`. Không chạy lại trên database đã có dữ liệu.

### Bước 3: Cấu hình kết nối

Mở file `appsettings.json` bên trong thư mục dự án ASP.NET Core.

Chỉnh chuỗi kết nối phù hợp với SQL Server trên máy:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=LuaMocDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Nếu sử dụng tên SQL Server khác, thay `localhost\\SQLEXPRESS` bằng tên server tương ứng.

 Bước 4: Chạy website

1. Mở file solution của dự án bằng Visual Studio 2022.
2. Khôi phục các NuGet Packages nếu được yêu cầu.
3. Chọn dự án ASP.NET Core MVC làm Startup Project.
4. Nhấn **F5** hoặc **Ctrl + F5** để chạy website.
5. Truy cập địa chỉ localhost do Visual Studio cung cấp.

4. Tài khoản và kiểm thử

**Quản trị viên**

- Tên đăng nhập: `admin`
- Mật khẩu: Admin\@123

**Khách hàng**

- Đăng ký tài khoản mới trên giao diện website.
- Đăng nhập và thực hiện các chức năng đặt món.

Thanh toán QR trong dự án sử dụng cơ chế mô phỏng nhằm phục vụ quá trình học tập và kiểm thử, không thực hiện giao dịch ngân hàng thực tế.

5. Các chức năng chính

**Khách hàng**

- Đăng ký, đăng nhập và khôi phục mật khẩu bằng OTP.
- Xem món ăn theo danh mục, xem combo.
- Thêm món vào giỏ hàng và đặt hàng.
- Sử dụng voucher.
- Thanh toán QR mô phỏng.
- Theo dõi trạng thái đơn hàng.

**Quản trị viên**

- Quản lý danh mục, món ăn và combo.
- Quản lý voucher và banner.
- Quản lý, xử lý đơn hàng.
- Thiết lập giờ hoạt động của cửa hàng.

6. Lưu ý

- Dữ liệu mẫu đã được chuẩn bị trong file SQL.
- Ảnh sản phẩm và banner cần được lưu kèm trong thư mục `wwwroot` của dự án.
- Cần cài đặt .NET SDK phù hợp với phiên bản dự án.

---

**Nhóm thực hiện:** Nhóm 12
**Đề tài:** Xây dựng website đặt món ăn trực tuyến LỬA MỘC
