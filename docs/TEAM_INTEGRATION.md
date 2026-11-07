# Tích hợp Sprint 1–2 và đóng góp của nhóm

Bản tích hợp ghép phần backlog đang làm ở local với `main` tại commit `147c377`, bao gồm PR #77, #78, #79, #80 và các commit bổ sung trên GitHub. Lịch sử được nối bằng merge commit; không ghi đè `main`, không squash hoặc thay tên tác giả các commit của thành viên.

## Phần của các nhánh nhóm được đưa vào

- PR #77 của `huyquang2005`: màn hình hồ sơ nhanh, danh sách hồ sơ theo đợt, thiết lập chỉ tiêu, gửi yêu cầu vượt chỉ tiêu và duyệt/từ chối có người duyệt, thời điểm và lý do. Kiểm tra chữ ký tệp PDF được giữ cho màn hình hồ sơ nhanh.
- PR #78 và các commit tiếp theo của `nguyenhoanganh2`: màn hình nhập Excel, xem trước từng dòng, thông tin số điện thoại và sinh mật khẩu ngẫu nhiên. Bổ sung các action còn thiếu, màn hình kết quả, phiếu xác nhận được mã hóa gắn với quản trị viên và hết hạn sau 15 phút. Dữ liệu từ các ô ẩn không được dùng làm nguồn tin cậy để tạo tài khoản.
- PR #79 của `huyenkhanh-diamond`: các màn hình khoa, ngành, năm học, học kỳ, tìm kiếm/phân trang, thêm/sửa và kích hoạt/vô hiệu hóa. Bổ sung kiểm tra năm học bao phủ học kỳ hiện có và giữ lựa chọn khoa cũ khi khoa đã ngừng hoạt động.
- PR #80 của `nguyenhoanganh2`: ghép mô hình đề tài với mô hình đã có quy trình duyệt, lịch sử và tệp. `Requirements` là bí danh của `KnowledgeRequirements`; dùng một trạng thái yêu cầu chỉnh sửa và bảng `topic_approval_history`, tránh tham chiếu lớp `TopicReview` chưa có.

Các số PR/issue trên GitHub khác cách đánh số US trong README ban đầu. Tài liệu này ghi phần đã tích hợp, không tự xác nhận mọi tiêu chí của các issue đã hoàn tất và không tự đóng issue.

## Quy tắc dùng chung sau tích hợp

Hồ sơ chi tiết (`/StudentProfile`, `/LecturerProfile`) và hồ sơ nhanh (`/Profiles`) đọc/ghi cùng các bảng chuẩn hóa. Sinh viên chỉ sửa kỹ năng, sở thích và CV; lớp, ngành, mã sinh viên và GPA không nhận từ biểu mẫu tự sửa. Hồ sơ nhanh hỗ trợ PDF tối đa 5 MB và kiểm tra chữ ký `%PDF-`; màn hình hồ sơ chi tiết giữ hỗ trợ PDF/DOC/DOCX tối đa 10 MB.

`/LecturerQuotas` và `/Profiles/Manage` cùng cập nhật `lecturer_campaign_quotas`. `/Assignments` và `/Profiles/AddAssignment` cùng gọi `StudentAssignmentService`, kiểm tra thời hạn MATCHING, điều kiện tham gia, ngành/khoa, vai trò đang hoạt động, mentor doanh nghiệp đã xác minh và phân công trùng. Số sinh viên đang hướng dẫn tính từ `student_assignments`. Giao dịch kiểm soát cả phân công đồng thời và việc hạ chỉ tiêu.

Màn hình hồ sơ/quota của nhóm gửi yêu cầu vượt chỉ tiêu vào `lecturer_campaign_assignments`. Chờ duyệt chưa tạo phân công thực tế. Khi duyệt, hệ thống kiểm tra lại điều kiện và thời hạn, ghi phân công cùng người duyệt; duyệt lặp không tạo thêm phân công. Màn hình `/Assignments` cho cán bộ có phạm vi quản lý phê duyệt trực tiếp bằng lý do cụ thể.

Hai mẫu nhập Excel được hỗ trợ:

- `/Users/ImportExcel`: A–E là `UserName, Email, FullName, PhoneNumber, Role`. Mật khẩu được sinh ngẫu nhiên, hiển thị ở trang kết quả cho quản trị viên, không lưu dưới dạng rõ trong CSDL. Trang kết quả không cho trình duyệt lưu cache; người dùng cần đổi mật khẩu sau khi nhận tài khoản.
- `/UserImport`: A–E là `UserName, Email, FullName, Role, InitialPassword`, có file mẫu tải xuống. Trang kết quả không hiển thị mật khẩu.

Cả hai dùng chung giới hạn 2 MB/500 dòng, kiểm tra từng dòng, chặn công thức, xác nhận bằng phiếu được bảo vệ và tạo người dùng/vai trò/phạm vi trong giao dịch. Phiếu được kiểm tra lại với dữ liệu hiện có; gửi lại không tạo trùng tài khoản.

## Nâng cấp CSDL

Sao lưu database và toàn bộ `InternshipManagement.Web/App_Data` trước khi nâng cấp. Bản SQL nền chỉ dùng cho database trống. Với database có dữ liệu, chạy các migration `000`, `001`, `002`, `003` theo thứ tự tên; xem [database/README.md](../database/README.md).

Bản tích hợp có 29 bảng nghiệp vụ và Identity. Nếu đã chạy SQL cũ của PR #77, migration giữ bảng quota cũ dưới tên `lecturer_campaign_quotas_legacy` và giữ các cột hồ sơ cũ trong `AspNetUsers`. Bảng/cột lưu trữ không còn là nguồn ghi của ứng dụng. Hồ sơ đã chuẩn hóa được ưu tiên; dữ liệu cũ không bị ghi đè vào hồ sơ hiện có.

Migration chỉ xác định khoa giảng viên khi các đợt quota cũ cùng thuộc một khoa. Trường hợp chưa có quota hoặc có nhiều khoa cần cán bộ liên kết hồ sơ chính xác, không tự đoán khoa. Phân công thực tập cũ chưa có mentor đã xác minh không tự trở thành phân công hợp lệ. Cần đối chiếu các dòng chưa chuyển, ví dụ:

```sql
-- Chỉ chạy truy vấn đầu nếu tồn tại bảng quota lưu trữ.
SELECT q.* FROM lecturer_campaign_quotas_legacy q
LEFT JOIN lecturer_profiles p ON p.user_id=q.lecturer_user_id
WHERE p.id IS NULL;

SELECT a.* FROM lecturer_campaign_assignments a
LEFT JOIN student_profiles s ON s.user_id=a.student_user_id
LEFT JOIN lecturer_profiles l ON l.user_id=a.lecturer_user_id
LEFT JOIN student_assignments current_assignment
  ON current_assignment.CampaignId=a.campaign_id
 AND current_assignment.StudentProfileId=s.id
 AND current_assignment.LecturerProfileId=l.id
WHERE a.status IN ('ASSIGNED','APPROVED_EXCEPTION')
  AND current_assignment.Id IS NULL;
```

CV cũ được tham chiếu qua `legacy_cvs/<tên PDF>` và đọc từ `App_Data/CVs`; tệp mới ở `App_Data/Uploads`. Giữ cả hai thư mục khi sao lưu. Kích thước metadata của tệp cũ là 0 vì migration không đọc tệp vật lý. Cấu hình kết nối thật tiếp tục nằm trong file Development bị Git bỏ qua; `appsettings.json` dùng giá trị mẫu.

## Kiểm thử

Chạy từ thư mục gốc:

```powershell
dotnet restore InternshipManagement.slnx
python tools/verify_backlog.py --clean-seed
python tools/verify_team_upgrade.py
```

Thiết lập `ConnectionStrings__DefaultConnection` hoặc file Development cục bộ trước khi chạy. Bộ kiểm thử sử dụng database tên ngẫu nhiên bắt đầu bằng `backlog_check_`, mở ứng dụng tạm ở cổng 5065 rồi dọn database và tiến trình. CSDL nguồn và tệp tải lên gốc không bị thay đổi; email kiểm thử được ghi vào outbox tạm, không gửi SMTP thật.

Phạm vi kiểm thử gồm 21 kịch bản Sprint 2, 8 nhóm backlog, 15 nhóm tiêu chí bổ sung và 6 nhóm tích hợp mới: màn hình/quyền truy cập, hồ sơ dùng chung, CV, nhập Excel, danh mục ngày tháng và chỉ tiêu/duyệt ngoại lệ. Kiểm thử nâng cấp riêng xác nhận hồ sơ, quota, phân công đã duyệt, CV và vai trò cũ còn nguyên sau nâng cấp và chạy lại migration.

Kiểm thử tự động kiểm tra các luồng được liệt kê; chưa xác nhận gửi thư qua SMTP thật hoặc nghiệm thu giao diện với giảng viên. GitHub Actions chạy lại kiểm tra sau khi nhánh được đẩy lên; trạng thái CI phải được xem riêng trước khi merge.
