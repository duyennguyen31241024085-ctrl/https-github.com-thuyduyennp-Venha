# Framebox

Framebox là cửa hàng phim minh họa cho bài tập cá nhân phát triển ứng dụng thương mại điện tử. Kho phim gồm năm tựa phim hư cấu, ba thể loại và ảnh bìa riêng. Checkout thu thập thông tin giao hàng và lưu đơn, không thu tiền trực tuyến.

## Công nghệ và chức năng

- ASP.NET Core MVC và Razor trên .NET 10, ASP.NET Core Identity, EF Core 10, Bootstrap.
- SQL Server cho cấu hình Somee. SQLite cho bản demo local không cần tài khoản hosting.
- Tìm, lọc, xem chi tiết và quản lý phim, thể loại. Tải hoặc thay ảnh JPEG/PNG tối đa 5 MB, dùng ảnh mặc định khi chưa tải.
- Giỏ Session với thêm, cập nhật số lượng, xóa và tính tiền theo giá trong CSDL.
- Checkout kiểm tra dữ liệu, lưu đơn, chi tiết có đơn giá và địa chỉ giao hàng, rồi hiển thị mã xác nhận. SMTP Gmail tùy chọn gửi thông báo sau khi ghi đơn.

## Cấu trúc

| Đường dẫn | Nội dung |
| --- | --- |
| `SourceCode/` | Project MVC, cấu hình mẫu và tài nguyên tĩnh |
| `SourceCode/Controllers/` | Phim, thể loại, giỏ và checkout |
| `SourceCode/Models/`, `SourceCode/Data/` | Model, DbContext, migration SQL Server và bộ dữ liệu demo |
| `SourceCode/Views/` | Giao diện Razor |
| `Report/` | Báo cáo Word và ảnh kiểm thử |

## Chạy demo local

Cài .NET 10 SDK. Mở thư mục `SourceCode` bằng VS Code và cài C# Dev Kit nếu muốn debug. Từ thư mục `ASP-CORE`:

```sh
cd SourceCode
dotnet --version
dotnet restore
dotnet build
dotnet run --launch-profile http
```

Mở địa chỉ HTTP được in ở dòng `Now listening on`. CSDL `framebox.db` và dữ liệu mẫu tự tạo khi chưa cấu hình SQL Server. Chạy ở Development với hồ sơ `http` để dùng chức năng quản lý phim/thể loại trong demo. Dùng dữ liệu giả khi thử checkout. Thông tin bìa website lấy từ `Student:Id` và `Student:Name`.

## Chạy với SQL Server

Tạo SQL Server database trên Somee và chép chuỗi kết nối. Thêm `TrustServerCertificate=True` nếu hosting dùng chứng chỉ mà máy local không nhận diện. Từ `SourceCode`:

```sh
dotnet tool install --global dotnet-ef --version 10.0.15
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<SQL_SERVER_CONNECTION_STRING>"
dotnet user-secrets set "Student:Name" "<HO_VA_TEN>"
dotnet ef database update -- --environment Development
dotnet run --launch-profile http
```

Migration `InitialCreate` tạo bảng Identity, Movies, Genres, HoaDons, ChiTietHoaDons và ShippingDetails. Đặt `Catalog:SeedDemo` thành `true` nếu muốn nạp bộ dữ liệu demo vào CSDL trống. Có thể dùng `dotnet ef migrations script --idempotent -- --environment Development` để kiểm tra SQL EF Core sinh ra. Trong Production, đặt `Catalog__EditorUserId` thành `Id` của tài khoản quản lý đã xác nhận trong bảng `AspNetUsers`. Các tài khoản khác vẫn có thể duyệt kho phim và đặt hàng.

Để gửi mail thật, tạo App Password trong tài khoản Gmail đã bật xác minh hai bước. Đặt `Email:Address` và `Email:AppPassword` trong User Secrets cho local hoặc `Email__Address` và `Email__AppPassword` trong môi trường hosting. Giữ chuỗi kết nối thật và App Password ngoài Git.

## Publish lên Somee

Từ `SourceCode`, chạy `dotnet publish -c Release -o ./publish`. Nén **nội dung** trong `publish` để `web.config` nằm ở gốc ZIP. Tạo site trên Somee, cấu hình SQL Server và biến môi trường trên hosting, rồi upload và giải nén ZIP qua File Manager. Nếu gói hosting thiếu .NET 10, thử bản `dotnet publish -c Release -r win-x64 --self-contained true -o ./publish` và kiểm tra giới hạn dung lượng. Kiểm tra bảng được migrate, quyền ghi tại `wwwroot/images/uploads`, URL HTTPS, checkout và SMTP trên hosting trước khi ghi kết quả vào báo cáo.

Bản `publish.zip` đi kèm được build cho IIS Windows x64 và cần runtime ASP.NET Core 10 trên máy chủ. Có thể tạo lại bằng lệnh:

```sh
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish
```
