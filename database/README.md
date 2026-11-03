# Khôi phục CSDL MySQL

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
