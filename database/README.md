# Khôi phục CSDL MySQL

> Bản SQL nền bên dưới có 13 bảng Sprint 1. Với bản tích hợp mới, chạy các file `migrations/*.sql` theo thứ tự tên từ `000` đến `003` vào cùng database, tổng cộng 29 bảng (không tính bảng lưu trữ cũ nếu có). Xem [hướng dẫn nâng cấp](../docs/BACKLOG_LOCAL.md).

`ql_thuctap_doan_demo.sql` chứa đủ cấu trúc 13 bảng và dữ liệu demo được xuất ngày 03/11/2026: tài khoản, vai trò, phạm vi, khoa, ngành, năm học, học kỳ và đợt đào tạo.

## Nhập bằng MySQL Workbench

1. Kết nối MySQL Server 8.0 bằng tài khoản có quyền tạo database.
2. Tạo một database trống:

   ```sql
   CREATE DATABASE ql_thuctap_doan CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
   ```

   Nếu tên này đã có dữ liệu, tạo tên khác (ví dụ `ql_thuctap_doan_demo`) và cập nhật chuỗi kết nối ứng dụng tương ứng.
3. Chọn **Server → Data Import → Import from Self-Contained File**, chọn `ql_thuctap_doan_demo.sql`.
4. Chọn database vừa tạo ở **Default Target Schema**, chọn **Dump Structure and Data**, rồi **Start Import**.
5. Refresh danh sách Schemas. Phải có 13 bảng.

File có lệnh `DROP TABLE IF EXISTS`, vì vậy chỉ nhập vào database trống dành cho bản demo. File không có `USE` hoặc `CREATE DATABASE`, database đích do người nhập chọn.

## Nâng cấp hồ sơ sinh viên và quota giảng viên

Sao lưu database và toàn bộ `InternshipManagement.Web/App_Data` trước khi nâng cấp. Chọn đúng schema rồi chạy lần lượt:

1. `migrations/000_prepare_team_schema.sql`
2. `migrations/001_sprint2_profiles_topics.sql`
3. `migrations/002_backlog_sprint1_2.sql`
4. `migrations/003_team_integration.sql`

Bản SQL nền chỉ nhập vào database trống; không nhập đè bản nền lên database đang dùng. Các migration có thể chạy lại. `000` đổi tên bảng quota cũ dùng user ID thành `lecturer_campaign_quotas_legacy`; `003` chuyển dữ liệu sang hồ sơ/chỉ tiêu chuẩn hóa. Các cột hồ sơ cũ và bảng lưu trữ vẫn được giữ để đối chiếu. **Không chạy** `legacy/upgrade_student_profiles_and_lecturer_quotas.sql` với bản mới; đây là script lịch sử của nhánh nhóm.

Nếu một giảng viên cũ có quota thuộc nhiều khoa hoặc chưa có quota, cán bộ phải liên kết hồ sơ/khoa trước khi chuyển phần còn lại; migration không tự chọn một khoa tùy ý. Phân công thực tập cũ thiếu mentor đã xác minh cũng cần đối soát, không tự coi là phân công hợp lệ. Sau nâng cấp xem [hướng dẫn tích hợp](../docs/TEAM_INTEGRATION.md) để kiểm tra dữ liệu còn trong bảng lưu trữ.

CV mới lưu trong `App_Data/Uploads`. CV PDF cũ vẫn được đọc từ `App_Data/CVs` qua các đường dẫn tải có kiểm tra quyền; không được đặt hai thư mục này trong `wwwroot`. Kích thước tệp cũ trong metadata là 0 (chưa xác định), tệp vật lý vẫn được giữ nguyên.

Hồ sơ nhanh tại `/Profiles` và hồ sơ chi tiết tại `/StudentProfile`, `/LecturerProfile` dùng cùng dữ liệu. `/Profiles/Manage` giữ quy trình gửi và duyệt vượt chỉ tiêu của nhóm; `/Assignments` hỗ trợ cán bộ có quyền phân công/phê duyệt trực tiếp với lý do. Cả hai dùng chung kiểm tra thời hạn, điều kiện, khoa/ngành, mentor và số phân công thực tế. Yêu cầu chờ duyệt chưa tạo `student_assignments`.

## Tài khoản trong bản SQL

- `admin` / `Admin@123`: quản trị viên.
- `cb_daotao` / `DaoTao@123`: cán bộ đào tạo.
- `cb_kinhte` / `KinhTe@123`: cán bộ khoa Kinh tế.
- `giangvien_demo` / `GiangVien@2026`: giảng viên.
- `doanhnghiep_demo` / `DoanhNghiep@2026`: doanh nghiệp.
- `student` / `Student@123`: sinh viên.
- Tài khoản thử nghiệm khác trong bản xuất dùng `Demo@2026`; vai trò và trạng thái hoạt động giữ theo dữ liệu nguồn.

Đây là mật khẩu demo của **bản xuất**, không phải mật khẩu đang dùng trên máy tác giả. Hash mật khẩu được tạo mới theo ASP.NET Core Identity; security stamp cũng được thay mới. Token đăng nhập, liên kết đăng nhập ngoài và số điện thoại không được đưa vào bản xuất. Không dùng các tài khoản demo này cho môi trường thật.

## SQL tham khảo được cung cấp

`reference/sprint1_ddl_partial.sql` là bản DDL người dùng cung cấp, giữ nguyên để tham khảo. File mới có 10 bảng, thiếu `terms`, `campaigns`, `user_role_scopes`; không dùng file này thay cho bản đầy đủ ở trên. Không nhập cả hai file vào cùng database.
