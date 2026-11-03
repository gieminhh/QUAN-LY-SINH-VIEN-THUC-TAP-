<div align="center">
  <img src="assets/logo_dhv.png" alt="Logo Trường Đại học Vinh" width="100">
  <h1>Hệ thống quản lý thực tập và đồ án sinh viên</h1>
  <p><strong>Trường Đại học Vinh · Nhóm N10 · Đề tài 36</strong></p>
  <p>Đồ án học phần Công nghệ phần mềm — Phiên bản Sprint 1</p>
  <p>ASP.NET Core MVC · .NET 10 · Entity Framework Core · MySQL 8</p>
</div>

---

<a id="gioi-thieu"></a>
## 1. Giới thiệu đề tài

Thực tập và đồ án là những hoạt động quan trọng trong quá trình đào tạo sinh viên, có sự phối hợp giữa phòng đào tạo, khoa/ngành, giảng viên, sinh viên và doanh nghiệp. Việc tổ chức các đợt, quản lý tài khoản và xác định đúng phạm vi trách nhiệm của từng bên là nền tảng để theo dõi quá trình thực hiện một cách thống nhất.

**Hệ thống quản lý thực tập và đồ án sinh viên** được nhóm N10 xây dựng trong khuôn khổ đề tài 36, học phần Công nghệ phần mềm. Đề tài hướng đến một website tập trung để hỗ trợ nhà trường tổ chức các đợt thực tập, đồ án và phân quyền truy cập theo vai trò, khoa, ngành hoặc từng đợt cụ thể. Bối cảnh và giao diện của ứng dụng được xây dựng theo Trường Đại học Vinh; đây là sản phẩm đồ án học phần.

Ở **Sprint 1**, nhóm tập trung vào nền tảng của hệ thống: xác thực người dùng, quản lý tài khoản, phân quyền theo phạm vi và quản lý đợt thực tập/đồ án. Các chức năng đăng ký đề tài, phân công hướng dẫn, nộp báo cáo và đánh giá kết quả thuộc định hướng phát triển tiếp theo, chưa được triển khai trong phiên bản này.

### Mục tiêu

- Tập trung thông tin tài khoản, vai trò và các đợt đào tạo trên cùng một hệ thống.
- Phân biệt quyền của quản trị viên, cán bộ đào tạo và cán bộ khoa/ngành.
- Giới hạn dữ liệu được xem và thao tác theo phạm vi được cấp.
- Hỗ trợ tạo, cập nhật, tìm kiếm và theo dõi trạng thái đợt thực tập/đồ án.
- Tạo nền tảng để mở rộng nghiệp vụ cho sinh viên, giảng viên và doanh nghiệp.

### Mục lục

- [Giới thiệu đề tài](#gioi-thieu)
- [Chức năng Sprint 1](#chuc-nang)
- [Vai trò và phạm vi phân quyền](#phan-quyen)
- [Công nghệ và tổ chức ứng dụng](#cong-nghe)
- [Cơ sở dữ liệu](#co-so-du-lieu)
- [Cài đặt và chạy dự án](#cai-dat)
- [Tài khoản demo](#tai-khoan)
- [Kịch bản trình diễn](#demo)
- [Cấu trúc thư mục](#cau-truc)
- [Kiểm thử và kết quả kiểm tra](#kiem-thu)
- [Lỗi thường gặp](#loi-thuong-gap)
- [Giới hạn và hướng phát triển](#phat-trien)

<a id="chuc-nang"></a>
## 2. Chức năng Sprint 1

### US-01 — Đăng nhập và đăng xuất

- Đăng nhập bằng tên tài khoản hoặc email và mật khẩu.
- Xác thực bằng ASP.NET Core Identity; mật khẩu được lưu dưới dạng hash.
- Kiểm tra trạng thái hoạt động của tài khoản trước khi đăng nhập.
- Khóa đăng nhập tạm thời 15 phút sau 5 lần nhập sai mật khẩu.
- Điều hướng người dùng đến màn hình phù hợp với vai trò sau khi đăng nhập.
- Chuyển người chưa đăng nhập về trang đăng nhập khi truy cập trang được bảo vệ.
- Hiển thị trang từ chối truy cập khi người dùng không đủ quyền.

### US-02 — Quản lý tài khoản

Chức năng dành cho quản trị viên:

- Xem danh sách, tìm kiếm, lọc theo vai trò/trạng thái và phân trang tài khoản.
- Tạo tài khoản, sửa thông tin, đặt lại mật khẩu và gán nhiều vai trò.
- Khóa hoặc mở lại trạng thái hoạt động của tài khoản.
- Ngăn quản trị viên tự vô hiệu hóa tài khoản đang sử dụng.
- Ngăn gỡ vai trò Admin của quản trị viên duy nhất còn lại.

### US-03 — Phân quyền theo phạm vi

- Gán và thu hồi phạm vi cho từng vai trò của người dùng.
- Hỗ trợ bốn mức: toàn trường, khoa, ngành và đợt.
- Lọc danh sách đợt và thống kê trang chủ theo phạm vi quản lý.
- Kiểm tra quyền tại máy chủ khi xem chi tiết hoặc chỉnh sửa đợt.
- Lọc lựa chọn ngành trên form tạo/sửa và bộ lọc danh sách.

### US-05 — Quản lý đợt thực tập và đồ án

- Quản lý hai loại đợt: **đồ án tốt nghiệp** (`PROJECT`) và **thực tập doanh nghiệp** (`INTERNSHIP`).
- Lưu mã đợt, tên đợt, học kỳ, ngành, hệ đào tạo, thời gian, trạng thái và người tạo.
- Kiểm tra mã đợt trùng và ngày kết thúc trước ngày bắt đầu.
- Tạo mới với trạng thái **Bản nháp** (`DRAFT`).
- Hỗ trợ các trạng thái **Đang mở** (`OPEN`), **Đã đóng** (`CLOSED`), **Lưu trữ** (`ARCHIVED`).
- Tìm kiếm theo mã/tên; lọc theo loại, trạng thái, học kỳ, ngành và phân trang.

Các trạng thái trên đang được hỗ trợ để cập nhật đợt; phiên bản hiện tại chưa áp đặt đầy đủ một quy trình chuyển trạng thái một chiều.

<a id="phan-quyen"></a>
## 3. Vai trò và phạm vi phân quyền

**Vai trò** xác định nhóm chức năng được truy cập. **Phạm vi** xác định dữ liệu mà người dùng được quản lý trong nhóm chức năng đó.

### Sáu vai trò trong hệ thống

- **Quản trị viên — `Admin`:** quản lý tài khoản, vai trò, phạm vi và các đợt trong toàn hệ thống.
- **Cán bộ đào tạo — `TrainingManager`:** quản lý đợt trong phạm vi được cấp. Tài khoản `cb_daotao` trong bản demo có phạm vi toàn trường.
- **Cán bộ khoa/ngành — `FacultyManager`:** quản lý đợt thuộc khoa, ngành hoặc đợt được phân công; không được vào trang quản lý tài khoản.
- **Giảng viên — `Lecturer`:** đăng nhập và xem trang chủ theo vai trò; chức năng hướng dẫn, theo dõi và đánh giá chưa triển khai.
- **Sinh viên — `Student`:** đăng nhập và xem trang chủ theo vai trò; chức năng đăng ký, nộp báo cáo và xem kết quả chưa triển khai.
- **Doanh nghiệp — `Company`:** đăng nhập và xem trang chủ theo vai trò; chức năng tiếp nhận và đánh giá thực tập chưa triển khai.

### Bốn mức phạm vi

- `GLOBAL`: quản lý toàn trường.
- `FACULTY`: quản lý các ngành và đợt thuộc một khoa.
- `MAJOR`: quản lý các đợt thuộc một ngành.
- `CAMPAIGN`: quản lý một đợt cụ thể; không tự cấp quyền tạo đợt khác trong cùng ngành.

**Ví dụ trong bản demo:** `cb_daotao` thấy đợt `DA_CNTT_K63` thuộc Khoa Công nghệ Thông tin. Tài khoản `cb_kinhte` được cấp phạm vi Khoa Kinh tế & Quản trị Kinh doanh nên không thấy đợt này; form tạo chỉ có ngành Quản trị Kinh doanh.

Giảng viên, sinh viên và doanh nghiệp không có quyền truy cập `/Users` hoặc `/Campaigns` trong Sprint 1. Việc gán thêm phạm vi dữ liệu không thay thế yêu cầu về vai trò truy cập chức năng.

<a id="cong-nghe"></a>
## 4. Công nghệ và tổ chức ứng dụng

- **Ngôn ngữ và nền tảng:** C#, ASP.NET Core MVC, .NET 10 (`net10.0`).
- **Truy cập dữ liệu:** Entity Framework Core; `Microsoft.EntityFrameworkCore.Design` phiên bản `10.0.14`.
- **Kết nối MySQL:** `MySql.EntityFrameworkCore` của Oracle, phiên bản `10.0.12`.
- **Xác thực:** `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, phiên bản `10.0.14`.
- **Cơ sở dữ liệu:** MySQL 8, InnoDB, bộ ký tự `utf8mb4`.
- **Giao diện:** Razor Views, HTML, CSS, JavaScript, Bootstrap `5.3.3` và Bootstrap Icons `1.11.3`.

Ứng dụng tổ chức theo mô hình MVC. Controller tiếp nhận yêu cầu và xử lý nghiệp vụ; ViewModel chứa dữ liệu của từng màn hình; Razor View hiển thị kết quả. `ApplicationDbContext` ánh xạ dữ liệu với MySQL. `UserScopeService` tập trung logic kiểm tra và lọc dữ liệu theo phạm vi; Identity xử lý tài khoản, vai trò và xác thực bằng cookie.

Giao diện dùng layout chung cho thanh điều hướng, menu tài khoản và nội dung trang. Các mục chưa triển khai được hiển thị rõ trạng thái thay vì giả lập chức năng.

<a id="co-so-du-lieu"></a>
## 5. Cơ sở dữ liệu

Bản xuất đầy đủ nằm tại **[database/ql_thuctap_doan_demo.sql](database/ql_thuctap_doan_demo.sql)**. Dữ liệu demo được xuất ngày **03/11/2026**, gồm **13 bảng, 7 tài khoản và 1 đợt đào tạo**.

### Nhóm xác thực — 7 bảng

- `AspNetUsers`: thông tin tài khoản và các trường xác thực.
- `AspNetRoles`: danh mục vai trò.
- `AspNetUserRoles`: liên kết tài khoản với vai trò.
- `AspNetUserClaims`, `AspNetRoleClaims`: thông tin claim theo tài khoản/vai trò.
- `AspNetUserLogins`, `AspNetUserTokens`: cấu trúc lưu đăng nhập ngoài và token theo Identity; bản xuất không kèm dữ liệu đăng nhập này.

### Nhóm nghiệp vụ — 6 bảng

- `faculties`: khoa.
- `majors`: ngành đào tạo, liên kết với khoa.
- `academic_years`: năm học.
- `terms`: học kỳ, liên kết với năm học.
- `campaigns`: đợt thực tập/đồ án, liên kết với ngành, học kỳ và người tạo.
- `user_role_scopes`: phạm vi quản lý của một cặp tài khoản–vai trò, người cấp và thời gian cấp.

Các quan hệ chính: một khoa có nhiều ngành; một năm học có nhiều học kỳ; một ngành và một học kỳ có thể có nhiều đợt. Một tài khoản có thể có nhiều vai trò và nhiều phạm vi. CSDL có khóa ngoại, ràng buộc duy nhất và kiểm tra ngày để hỗ trợ tính toàn vẹn dữ liệu.

**Dữ liệu mẫu:** 3 khoa, 4 ngành, 2 năm học, 5 học kỳ; đợt `DA_CNTT_K63` thuộc ngành Hệ thống Thông tin đang ở trạng thái `OPEN`.

Mật khẩu trong bản xuất được thay bằng mật khẩu demo; không dùng lại hash mật khẩu cá nhân trên máy phát triển. Chi tiết khôi phục và dữ liệu được lược bỏ xem tại **[hướng dẫn CSDL](database/README.md)**.

> File [reference/sprint1_ddl_partial.sql](database/reference/sprint1_ddl_partial.sql) là DDL tham khảo chỉ có 10 bảng. Dùng bản đầy đủ nêu trên để chạy ứng dụng; không nhập hai file vào cùng database.

<a id="cai-dat"></a>
## 6. Cài đặt và chạy dự án

### 6.1. Chuẩn bị môi trường

- Git để tải mã nguồn.
- **.NET SDK 10**, không chỉ .NET Runtime.
- **MySQL Server 8.0.16 trở lên** và tài khoản có quyền tạo/nhập CSDL.
- MySQL Workbench hoặc MySQL command-line client.
- Kết nối Internet khi khôi phục NuGet package và tải Bootstrap/biểu tượng từ CDN.

Môi trường đã dùng để kiểm tra: Windows, .NET 10, MySQL Server 8.0. Bản SQL chưa được xác nhận tương thích với MariaDB của XAMPP.

### 6.2. Tải mã nguồn

```powershell
git clone https://github.com/gieminhh/cnpm-n10-prj36.git
cd cnpm-n10-prj36
```

Repo đang ở chế độ riêng tư; tài khoản GitHub cần được cấp quyền truy cập để clone.

### 6.3. Nhập CSDL

1. Bật MySQL Server và kết nối bằng Workbench.
2. Tạo một database trống:

   ```sql
   CREATE DATABASE ql_thuctap_doan
     CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
   ```

3. Chọn **Server → Data Import → Import from Self-Contained File**.
4. Chọn `database/ql_thuctap_doan_demo.sql`.
5. Chọn `ql_thuctap_doan` ở **Default Target Schema**, chọn **Dump Structure and Data**, rồi **Start Import**.
6. Refresh Schemas và kiểm tra:

   ```sql
   USE ql_thuctap_doan;
   SHOW TABLES;
   SELECT COUNT(*) AS so_tai_khoan FROM AspNetUsers;
   SELECT code, name, status FROM campaigns;
   ```

Kết quả với bản SQL đi kèm: 13 bảng, 7 tài khoản và đợt `DA_CNTT_K63`.

> File SQL có lệnh xóa/tạo lại bảng. Chỉ nhập vào database trống dành cho bản demo. Nếu `ql_thuctap_doan` đã có dữ liệu cần giữ, tạo database tên khác và dùng cùng tên đó trong cấu hình ứng dụng.

Repo hiện không có bộ EF Core Migrations để tạo đầy đủ schema. Cần nhập SQL trước khi chạy; bước seed lúc khởi động chỉ bổ sung dữ liệu mặc định còn thiếu, không thay thế việc tạo bảng.

### 6.4. Cấu hình kết nối

Tạo file `InternshipManagement.Web/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=ql_thuctap_doan;Uid=YOUR_MYSQL_USER;Pwd=YOUR_MYSQL_PASSWORD;CharSet=utf8mb4;"
  }
}
```

Thay `YOUR_MYSQL_USER` và `YOUR_MYSQL_PASSWORD` bằng thông tin MySQL của máy đang chạy. Nếu tạo database khác, sửa cả `Database`; nếu MySQL dùng cổng khác, sửa `Port`.

File cấu hình Development đã được `.gitignore` loại khỏi commit. `appsettings.json` trong repo chỉ chứa giá trị mẫu. Khi chạy ngoài môi trường Development, cấu hình kết nối qua biến môi trường `ConnectionStrings__DefaultConnection` hoặc cơ chế quản lý cấu hình của môi trường triển khai.

**Phân biệt hai thông tin đăng nhập:** tài khoản MySQL dùng để ứng dụng kết nối CSDL; tài khoản `admin`, `student`… dùng để đăng nhập website.

### 6.5. Khởi chạy

Chạy tại thư mục gốc repo:

```powershell
dotnet restore InternshipManagement.slnx
dotnet build InternshipManagement.slnx
dotnet run --project InternshipManagement.Web --launch-profile http
```

Mở **[http://localhost:5064](http://localhost:5064)** sau khi xuất hiện dòng `Now listening on`. Đăng nhập bằng một tài khoản demo ở mục tiếp theo. Nhấn **Ctrl+C** tại cửa sổ chạy ứng dụng để dừng web; MySQL là dịch vụ riêng.

<a id="tai-khoan"></a>
## 7. Tài khoản demo

Các thông tin sau áp dụng cho **database vừa khôi phục từ file SQL của repo**:

- **Quản trị viên:** `admin` / `Admin@123` — quản lý tài khoản, phạm vi và đợt toàn hệ thống.
- **Cán bộ đào tạo:** `cb_daotao` / `DaoTao@123` — quản lý đợt toàn trường trong bản demo.
- **Cán bộ khoa:** `cb_kinhte` / `KinhTe@123` — phạm vi Khoa Kinh tế & Quản trị Kinh doanh.
- **Giảng viên:** `giangvien_demo` / `GiangVien@2026`.
- **Doanh nghiệp:** `doanhnghiep_demo` / `DoanhNghiep@2026`.
- **Sinh viên:** `student` / `Student@123`.

Bản xuất còn một tài khoản thử nghiệm `test123`, dùng mật khẩu `Demo@2026`; không cần dùng tài khoản này trong kịch bản trình diễn chính.

Nếu đã đổi mật khẩu trên một database đang sử dụng, mật khẩu ở đây không tự thay thế mật khẩu đã đổi. Các tài khoản và mật khẩu công khai này chỉ phục vụ demo, không dùng cho hệ thống thực tế.

<a id="demo"></a>
## 8. Kịch bản trình diễn trước giảng viên

Thời lượng gợi ý: **10–15 phút**, sử dụng bản CSDL demo. Đăng xuất trước mỗi lần chuyển tài khoản.

### Bước 1 — Xác thực người dùng

Mở `/Users` khi chưa đăng nhập để kiểm tra chuyển hướng về trang đăng nhập. Thử sai mật khẩu một lần và quan sát thông báo lỗi, sau đó đăng nhập bằng `admin`.

**Kết quả cần thấy:** người chưa xác thực không vào được trang quản trị; tài khoản hợp lệ được đưa đến màn hình phù hợp.

### Bước 2 — Quản trị tài khoản

Dùng `admin` để xem danh sách, tìm kiếm và lọc tài khoản. Tạo một tài khoản demo mới, gán vai trò, mở trang phân quyền phạm vi. Thử khóa rồi mở lại chính tài khoản demo đó và kiểm tra đăng nhập khi bị khóa.

**Kết quả cần thấy:** dữ liệu được lưu, vai trò/phạm vi hiển thị đúng, tài khoản bị vô hiệu hóa không đăng nhập được. Mở lại tài khoản sau khi thử.

### Bước 3 — Quản lý đợt toàn trường

Đăng nhập `cb_daotao`, xem đợt CNTT có sẵn. Tạo đợt với một mã mới, chọn ngành, học kỳ và thời gian. Kiểm tra thông báo khi ngày kết thúc trước ngày bắt đầu hoặc dùng mã đã có. Lưu dữ liệu hợp lệ rồi chuyển đợt từ **Bản nháp** sang **Đang mở**.

**Kết quả cần thấy:** dữ liệu sai bị từ chối, đợt hợp lệ xuất hiện trong danh sách và tìm/lọc được.

### Bước 4 — Phân quyền khoa/ngành

Đăng nhập `cb_kinhte`, kiểm tra không thấy đợt `DA_CNTT_K63`. Mở form tạo để xem lựa chọn ngành Quản trị Kinh doanh. Thử truy cập trực tiếp `/Campaigns/Details/2` của đợt CNTT có sẵn trong bản SQL.

**Kết quả cần thấy:** dữ liệu ngoài khoa không xuất hiện; truy cập trực tiếp đợt ngoài phạm vi bị từ chối. Để demo mức ngành, Admin có thể tạo một tài khoản `FacultyManager` riêng và chỉ cấp `MAJOR` cho một ngành.

### Bước 5 — Các vai trò còn lại

Lần lượt đăng nhập giảng viên, doanh nghiệp và sinh viên. Quan sát tên, nhãn vai trò và nội dung trang chủ. Thử truy cập trực tiếp `/Users` hoặc `/Campaigns`.

**Kết quả cần thấy:** không xuất hiện link quản lý dành cho cán bộ; truy cập trực tiếp bị từ chối. Các nghiệp vụ riêng chưa có phải được giới thiệu là phần dự kiến phát triển.

<a id="cau-truc"></a>
## 9. Cấu trúc thư mục

```text
cnpm-n10-prj36/
├── InternshipManagement.slnx          # Solution .NET
├── InternshipManagement.Web/
│   ├── Controllers/                  # Đăng nhập, tài khoản, đợt, trang chủ
│   ├── Data/                         # DbContext và dữ liệu khởi tạo
│   ├── Models/                       # Thực thể và hằng số vai trò/trạng thái
│   ├── Services/                     # Kiểm tra và lọc phạm vi quản lý
│   ├── ViewModels/                   # Dữ liệu cho form và danh sách
│   ├── Views/                        # Giao diện Razor và layout chung
│   ├── wwwroot/                      # CSS, JavaScript, ảnh và thư viện
│   ├── Properties/launchSettings.json
│   ├── appsettings.json              # Cấu hình mẫu, không chứa mật khẩu thật
│   └── Program.cs                    # Đăng ký dịch vụ và cấu hình ứng dụng
├── database/
│   ├── ql_thuctap_doan_demo.sql       # Đầy đủ schema và dữ liệu demo
│   ├── reference/                    # SQL tham khảo chưa đầy đủ
│   └── README.md                     # Hướng dẫn khôi phục CSDL
├── assets/, css/, js/, index.html    # Giao diện HTML tham khảo ban đầu
├── run_acceptance_tests.py           # Kịch bản nghiệm thu hiện có
├── .gitignore
└── README.md
```

Giao diện đang chạy thực tế nằm trong `Views/` và `wwwroot/` của ứng dụng MVC. File `index.html` ở gốc chỉ là bản tham khảo ban đầu.

<a id="kiem-thu"></a>
## 10. Kiểm thử và kết quả kiểm tra

### Các kiểm tra đã thực hiện

- Build ứng dụng thành công, không có lỗi hoặc cảnh báo trong lần kiểm tra trước khi đưa repo lên GitHub.
- Nhập bản SQL vào database tạm: xác nhận đủ 13 bảng, 7 tài khoản và 1 đợt.
- Đăng nhập bằng sáu tài khoản demo chính trên database đã khôi phục.
- Kiểm tra lựa chọn ngành của Admin, cán bộ đào tạo và cán bộ khoa.
- Kiểm tra việc từ chối ngành ngoài phạm vi khi gửi form tạo đợt.
- Kiểm tra trang chủ theo vai trò và việc từ chối truy cập trang quản lý của giảng viên, doanh nghiệp, sinh viên.

Đây là kết quả kiểm tra của bản hiện tại, không phải cam kết mọi trường hợp nghiệp vụ hoặc mọi môi trường đã được kiểm thử.

### Kịch bản tự động hiện có

`run_acceptance_tests.py` dùng Python và `requests`, kết hợp gọi website và MySQL client. Script có thao tác **xóa/tạo lại tài khoản, đợt demo và thay đổi trạng thái tài khoản**; chỉ chạy trên môi trường kiểm thử riêng.

Script đang dùng cố định tên database `ql_thuctap_doan` trong các câu SQL. Vì vậy cần dùng một MySQL instance dành riêng cho kiểm thử, rồi cấu hình cả ứng dụng và script trỏ đến cùng instance đó; chỉ đổi tên database trong cấu hình ứng dụng là chưa đủ.

Chuẩn bị thư viện:

```powershell
python -m pip install requests
```

Các biến môi trường cần cấu hình: `MYSQL_HOST`, `MYSQL_PORT`, `MYSQL_USER`, `MYSQL_PWD`, `TEST_BASE_URL`; đặt thêm `MYSQL_EXE` nếu `mysql` chưa có trong PATH. Sau khi xác nhận đang dùng môi trường kiểm thử riêng:

```powershell
$env:ALLOW_DESTRUCTIVE_DEMO_TESTS = "1"
python run_acceptance_tests.py
```

Script hiện còn một số điều kiện kiểm tra chưa chặt và chưa bao phủ đầy đủ các trường hợp kết hợp vai trò/phạm vi. Cần kết hợp với kịch bản kiểm tra thủ công ở trên; repo chưa có bộ unit test hoặc CI tự động.

<a id="loi-thuong-gap"></a>
## 11. Lỗi thường gặp

### Không kết nối được `localhost:3306`

Kiểm tra MySQL Server có đang chạy và cổng trong chuỗi kết nối có khớp hay không. Trên Windows, mở **Services → MySQL80 → Start** bằng quyền quản trị nếu dịch vụ được cài dưới tên này. Có thể đặt Startup type thành Automatic để tự chạy cùng Windows.

Trên máy phát triển của dự án, **MySQL80 dùng 3306**, còn **MySQL/MariaDB của XAMPP dùng 3307**. Bật MySQL của XAMPP không đồng nghĩa đã bật MySQL80 chứa CSDL đồ án.

### `Access denied for user`

Kiểm tra `Uid`, `Pwd` và quyền của tài khoản MySQL đối với database đã nhập. Không nhập tài khoản đăng nhập website vào chuỗi kết nối CSDL.

### `Unknown database` hoặc thiếu bảng

Kiểm tra tên database trong cấu hình, database đích khi import và đủ 13 bảng. Không dùng file DDL tham khảo 10 bảng để chạy ứng dụng. Web có thể vẫn mở màn hình đăng nhập khi seed dữ liệu thất bại, vì vậy cần xem cả log khởi động.

### Cổng web `5064` đang được sử dụng

Dừng phiên chạy đồ án cũ bằng Ctrl+C, hoặc chạy bằng cổng khác:

```powershell
dotnet run --project InternshipManagement.Web --launch-profile http --urls http://localhost:5065
```

Nếu đổi cổng khi kiểm thử, cập nhật `TEST_BASE_URL` tương ứng.

### Tài khoản đăng nhập được nhưng không thấy đợt

Kiểm tra vai trò và phạm vi đã cấp. `cb_kinhte` không thấy đợt CNTT là kết quả phân quyền đúng. Giảng viên, sinh viên và doanh nghiệp không có quyền vào trang quản lý đợt ở Sprint 1.

### Sai mật khẩu demo hoặc bị khóa tạm thời

Mật khẩu trong README áp dụng cho bản SQL đi kèm. Nếu đã thay đổi mật khẩu trên database đang dùng, sử dụng mật khẩu mới hoặc nhờ Admin đặt lại. Sau 5 lần nhập sai, tài khoản bị khóa tạm thời 15 phút; không thử liên tục.

### Giao diện mất định dạng hoặc biểu tượng

Kiểm tra kết nối Internet tới CDN được tham chiếu trong layout. Có các thư viện tĩnh trong repo, nhưng layout hiện vẫn tải Bootstrap và Bootstrap Icons từ CDN.

<a id="phat-trien"></a>
## 12. Giới hạn và hướng phát triển

### Giới hạn hiện tại

- Chưa có nghiệp vụ đăng ký đề tài, phân công giảng viên/doanh nghiệp, nộp báo cáo, chấm điểm hay tổng hợp kết quả.
- Các mục hồ sơ người học, thông báo học vụ, thời khóa biểu và lịch thi chưa triển khai; liên kết quên mật khẩu chưa có quy trình khôi phục qua email.
- Nhãn năm học/học kỳ ở header vẫn là thông tin cố định, chưa có màn hình cấu hình học kỳ hiện hành.
- Chưa hoàn thiện toàn bộ ràng buộc chuyển trạng thái và quy tắc bất biến cho đợt đã lưu trữ.
- Chưa có quy trình triển khai production, kiểm thử tải hoặc đánh giá bảo mật toàn diện. Chính sách mật khẩu hiện được cấu hình để phục vụ demo.

### Định hướng các sprint tiếp theo

1. Cấu hình các giai đoạn của đợt và điều kiện tham gia.
2. Quản lý danh sách sinh viên đủ điều kiện, hỗ trợ nhập dữ liệu hàng loạt.
3. Đăng ký đề tài; phân công giảng viên hướng dẫn và doanh nghiệp tiếp nhận.
4. Theo dõi tiến độ, nộp báo cáo, nhận xét và đánh giá kết quả.
5. Hoàn thiện thông báo, thống kê và giao diện nghiệp vụ riêng theo vai trò.
6. Bổ sung kiểm thử tự động, hoàn thiện phân quyền và chuẩn bị cấu hình triển khai thực tế.

---

**Nhóm thực hiện:** N10 — Đề tài 36, học phần Công nghệ phần mềm, Trường Đại học Vinh.

**Mã nguồn:** [gieminhh/cnpm-n10-prj36](https://github.com/gieminhh/cnpm-n10-prj36).

**Phạm vi tài liệu:** phiên bản Sprint 1 và bản CSDL demo ngày 03/11/2026.
