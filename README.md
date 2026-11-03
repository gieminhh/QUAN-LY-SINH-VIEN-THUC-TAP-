# Quản lý thực tập & đồ án sinh viên — N10, đề tài 36

Ứng dụng ASP.NET Core MVC của Trường Đại học Vinh, triển khai Sprint 1 với MySQL 8.

## Phạm vi hiện tại

- Đăng nhập, đăng xuất, khóa tài khoản và phân luồng theo vai trò.
- Quản trị tài khoản, gán vai trò và phạm vi toàn trường / khoa / ngành / đợt.
- Tạo, sửa, xem, lọc và chuyển trạng thái đợt thực tập / đồ án.
- Cán bộ khoa chỉ thấy các đợt và lựa chọn ngành trong phạm vi được cấp.
- Trang chủ hiển thị theo vai trò. Nghiệp vụ riêng của sinh viên, giảng viên và doanh nghiệp chưa triển khai; giao diện ghi rõ trạng thái này.

## Chạy dự án

Yêu cầu: .NET SDK 10, MySQL Server 8.0.16 trở lên. Dự án dùng EF Core 10.0.14 và Oracle MySQL provider 10.0.12.

1. Clone repo rồi nhập `database/ql_thuctap_doan_demo.sql` vào database trống theo [hướng dẫn CSDL](database/README.md).
2. Tạo file `InternshipManagement.Web/appsettings.Development.json` với cấu hình của máy:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Port=3306;Database=ql_thuctap_doan;Uid=YOUR_MYSQL_USER;Pwd=YOUR_MYSQL_PASSWORD;CharSet=utf8mb4;"
     }
   }
   ```

   File này được Git bỏ qua. Cũng có thể dùng biến môi trường `ConnectionStrings__DefaultConnection`, đặc biệt khi chạy ngoài môi trường Development.
3. Bật dịch vụ MySQL Server, rồi chạy tại thư mục repo:

   ```powershell
   dotnet restore InternshipManagement.slnx
   dotnet run --project InternshipManagement.Web --launch-profile http
   ```

4. Mở http://localhost:5064. Tài khoản demo được liệt kê trong [database/README.md](database/README.md).

Trên máy phát triển hiện tại, MySQL80 dùng cổng 3306; MySQL/MariaDB của XAMPP dùng cổng 3307. Database của đồ án nằm ở MySQL80. Bản SQL đi kèm dành cho MySQL 8, chưa kiểm tra tương thích MariaDB.

## Thư mục

- `InternshipManagement.Web/`: ứng dụng MVC, Identity, dịch vụ phân quyền và giao diện Razor.
- `database/`: cấu trúc và dữ liệu demo đầy đủ, hướng dẫn import, SQL tham khảo.
- `index.html`, `css/`, `js/`, `assets/`: giao diện HTML tham khảo ban đầu.
- `run_acceptance_tests.py`: kịch bản nghiệm thu cũ; cần Python và thư viện `requests`.

Các tài liệu cá nhân trong `WORKSPACE/`, dự án nháp `test/`, file cấu hình máy, thư mục build và log không được đưa vào repo.

## Kiểm tra

```powershell
dotnet build InternshipManagement.slnx
```

Kịch bản `run_acceptance_tests.py` có thao tác xóa/tạo lại tài khoản và đợt demo. Chỉ chạy với **bản CSDL dùng riêng cho kiểm thử**, đồng thời cấu hình ứng dụng và script trỏ cùng database `ql_thuctap_doan` trên máy/instance kiểm thử. Script hiện vẫn có một số điều kiện kiểm tra chưa chặt, không dùng kết quả của nó để khẳng định đã kiểm thử toàn bộ phân quyền.

Trước khi chạy cần đặt `MYSQL_PWD`, `MYSQL_USER`, `MYSQL_HOST`, `MYSQL_PORT`, `MYSQL_EXE` (nếu mysql chưa có trong PATH), `TEST_BASE_URL` và `ALLOW_DESTRUCTIVE_DEMO_TESTS=1`. Không lưu mật khẩu thật vào mã nguồn.
