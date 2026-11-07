# Bản local đối chiếu backlog Sprint 1–2

Phạm vi: issue #2–12 và hoàn thiện luồng đề tài hiện có #14–15. Không tự đóng issue hoặc commit/push. Đây là bản triển khai để nhóm xem và nghiệm thu; không thay cho xác nhận nghiệp vụ của giảng viên.

## Các mục đã bổ sung

- **#2:** [ERD và ràng buộc](ERD.md) mô tả hệ thống hiện tại và phần tổng thể dự kiến. Phân biệt thực tập cá nhân, nhóm đồ án, nguồn GPA và quy tắc quota.
- **#3:** .NET 10 MVC + MySQL, cấu hình Development được bỏ qua bởi Git; biến môi trường hỗ trợ môi trường triển khai.
- **#4:** `/Account/ChangePassword`, `/Account/ForgotPassword`, `/Account/ResetPassword`. Đổi mật khẩu cần mật khẩu hiện tại; token reset có thời hạn theo Identity, không dùng lại sau reset thành công. Mật khẩu và trạng thái khóa không bị seed ghi đè khi khởi động lại.
- **#5:** vai trò và phạm vi được kiểm tra từ dữ liệu hiện hành; tài khoản khóa hoặc cookie có security stamp cũ không tiếp tục dùng phiên. Xem hồ sơ/CV theo chủ sở hữu, phạm vi hoặc phân công; doanh nghiệp chỉ xem SV được giao qua người hướng dẫn đã xác minh. Quyền phản biện sẽ nằm trong nghiệp vụ đánh giá ở sprint sau, hiện không có luồng phản biện.
- **#6:** workflow `.github/workflows/build-and-test.yml`, mẫu Issue và PR; bộ test local dùng CSDL tạm. Workflow mới được chuẩn bị trong máy, chưa chạy trên GitHub vì chưa push.
- **#7:** `/UserImport`, hỗ trợ `.xlsx` 2MB / 500 dòng, file mẫu tải trực tiếp; kiểm tra lỗi theo dòng trước khi tạo. Xác nhận bằng phiếu được bảo vệ, gắn người nhập và hết hạn sau 15 phút. Kiểm tra lại trùng khi xác nhận; mỗi dòng có giao dịch riêng và kết quả. Không lưu mật khẩu ban đầu vào báo cáo kết quả.
- **#8:** hồ sơ sinh viên, hai thang GPA riêng cùng nguồn/người/thời điểm ghi, lịch sử kỳ; SV không sửa dữ liệu học vụ. CV tải lên có giới hạn loại và 10MB; dữ liệu GPA chính thức vẫn cần nhà trường xác nhận, hiện bản demo dùng MANUAL.
- **#9:** hồ sơ GV, quota theo đợt, `/Assignments` đếm số SV đã phân công. Chặn quota 0/hết quota, trùng SV trong đợt và giảm quota dưới tải hiện tại. Cán bộ có quyền có thể phê duyệt vượt quota với lý do được lưu.
- **#10:** `/Companies/Register` cho tài khoản Company gửi yêu cầu, `/Companies` cho cán bộ trong phạm vi duyệt/từ chối kèm lý do. Doanh nghiệp được xác minh có danh sách tài khoản người hướng dẫn; `/Companies/Students` chỉ trả về SV được phân công cho chính người hướng dẫn đang đăng nhập.
- **#11:** `/Catalog` dành cho Admin quản lý khoa/ngành/năm học/học kỳ, thêm/sửa và xóa bản ghi chưa được dùng. Ràng buộc mã trùng, quan hệ tham chiếu, số kỳ 1–3 và khoảng thời gian. `/Campaigns` giữ chức năng quản lý đợt.
- **#12:** `/CampaignRules` cho cán bộ đúng phạm vi cấu hình bốn giai đoạn, kiểm tra thứ tự/không chồng lấn/nằm trong đợt; cấu hình GPA/tín chỉ/tiên quyết, nhập minh chứng có nguồn và lọc danh sách đủ/chưa đủ điều kiện. Ngoại lệ thời hạn có người dùng, giai đoạn, hết hạn, lý do, người duyệt và thu hồi.
- **#14:** kiểm tra lịch đăng ký ở server khi tạo/sửa/gửi đề tài; chặn tệp sai loại hoặc dung lượng trước khi lưu.
- **#15:** luồng nháp → chờ duyệt → yêu cầu sửa/từ chối/duyệt có lịch sử, quyền và phản hồi. SV chỉ thấy đề tài đã duyệt; đăng ký/nguyện vọng đề tài (#18) chưa triển khai nên tiêu chí chống chọn đề tài chưa duyệt cần kiểm tra lại khi triển khai chức năng đó.

## CSDL và nâng cấp

Bản SQL `ql_thuctap_doan_demo.sql` là nền Sprint 1 (13 bảng), không phải bản xuất đầy đủ mới. Trên một bản sao CSDL, nhập theo thứ tự:

1. `database/ql_thuctap_doan_demo.sql` nếu bắt đầu từ database trống.
2. Chạy `database/migrations/000_prepare_team_schema.sql` trước, để lưu bảng quota cũ nếu có.
3. `database/migrations/001_sprint2_profiles_topics.sql` bổ sung 8 bảng Sprint 2.
4. `database/migrations/002_backlog_sprint1_2.sql` bổ sung 7 bảng lịch/điều kiện/doanh nghiệp/phân công.

Hai script nâng cấp dùng CREATE TABLE IF NOT EXISTS, không xóa dữ liệu cũ. Chọn đúng database đích trước khi nhập. Máy local đã được sao lưu riêng trong `InternshipManagement.Web/App_Data/Backups/` và áp dụng các bảng bổ sung. Không đưa backup, upload hoặc hộp thư thử nghiệm lên Git.

Sau nâng cấp, **đợt chưa có lịch đăng ký sẽ bị chặn tạo/sửa/gửi đề tài**. Vào Lịch & điều kiện đợt để cấu hình trước khi demo. Không tự cấu hình GPA hoặc tín chỉ theo một quy định chưa được xác nhận.

## Email khôi phục

Development và chưa có SMTP: thư được ghi vào `InternshipManagement.Web/App_Data/MailOutbox/`. Đây là hộp thư thử nghiệm trên máy, không gửi ra Internet. Liên kết chứa token nhạy cảm, không đưa lên repo.

Gửi email thật cần cấu hình riêng các khóa `Mail:BaseUrl`, `Mail:Host`, `Mail:Port`, `Mail:User`, `Mail:Password`, `Mail:From` qua cấu hình bỏ qua Git hoặc biến môi trường tương ứng (`Mail__...`). SMTP bật TLS. BaseUrl phải là địa chỉ website do quản trị cấu hình, không lấy theo Host của yêu cầu. Production không tự chuyển sang hộp thư local nếu thiếu SMTP.

Tài khoản nhập từ Excel chưa xác nhận email; không tự coi email là đã được xác minh. Admin vẫn có thể đặt lại mật khẩu qua quản lý tài khoản. Luồng xác minh email tự phục vụ chưa thuộc phần bổ sung lần này.

## Kiểm tra local

```powershell
python tools/verify_backlog.py
```

Lệnh build vào thư mục tạm, sao chép CSDL và upload sang môi trường kiểm thử riêng cổng 5065, chạy bộ 21 kịch bản Sprint 2 và 8 nhóm kiểm thử backlog. Các tài khoản kiểm thử bổ sung chỉ được tạo trong bản sao. Sau khi chạy, database kiểm thử được xóa; log trong thư mục tạm được giữ để chẩn đoán. Không chạy trực tiếp các script nghiệm thu có cập nhật dữ liệu lên CSDL dùng để demo.

Để kiểm tra khả năng khôi phục từ SQL trong repo, không dựa vào dữ liệu local:

```powershell
python tools/verify_backlog.py --clean-seed
```

Trên Windows cần .NET SDK 10 và MySQL client. Runner đọc kết nối từ `ConnectionStrings__DefaultConnection` hoặc file Development đã bỏ qua Git. Có thể chỉ định `MYSQL_EXE` nếu MySQL client ở nơi khác. Không in mật khẩu trong log. Cổng 5065 phải trống.

## Các bước demo

1. Admin: đổi mật khẩu, danh mục học vụ, tải mẫu Excel → kiểm tra → xác nhận.
2. Cán bộ: chọn đợt → cấu hình bốn mốc và điều kiện → xem SV đủ/chưa đủ.
3. Company: gửi hồ sơ → cán bộ đúng khoa duyệt với lý do → thêm người hướng dẫn đã có tài khoản Company.
4. Cán bộ: đặt quota → phân công trong cửa sổ MATCHING → thử quota 0/hết quota → ghi lý do để phê duyệt ngoại lệ.
5. Người hướng dẫn DN: xem SV được giao; thử đường dẫn SV ngoài phân công bị từ chối.
6. GV: thử sửa/gửi đề tài ngoài cửa sổ REGISTRATION → bị chặn; cán bộ cấp ngoại lệ có hạn → thực hiện lại được.

Bản này không bao gồm các nghiệp vụ nguyện vọng/nhóm, thuật toán ghép, báo cáo hoặc chấm điểm ở các issue sprint sau. Mốc nộp/đánh giá đã cấu hình được nhưng chưa có form nộp/chấm để áp dụng; các endpoint mới sau này phải sử dụng cùng dịch vụ kiểm tra cửa sổ thời gian.

## Kết quả kiểm tra mở rộng — 05/11/2026

Chạy `python tools/verify_backlog.py --clean-seed` trên CSDL khôi phục từ SQL repo: **21/21 kịch bản Sprint 2, 8/8 nhóm backlog và 15/15 nhóm tiêu chí mở rộng đạt**. Mỗi nhóm có nhiều kiểm tra dữ liệu/HTTP/quan hệ; không quy đổi các con số này thành tỷ lệ bao phủ toàn hệ thống.

Kết quả máy đọc: [criteria-results-2026-11-05.json](verification/criteria-results-2026-11-05.json).

Đã kiểm tra thêm khóa tài khoản/phiên cũ, thay đổi nhiều vai trò, đổi mật khẩu và phiên cũ, token sai/đã dùng, giả mạo GPA, CV không hợp lệ, Excel lỗi, CSRF, thời hạn/ngoại lệ, điều kiện tiên quyết, trạng thái xét duyệt và hai yêu cầu phân công đồng thời tranh một chỗ quota. Bốn sơ đồ Mermaid đã parse và render thành SVG thành công. Workflow đã qua kiểm tra cú pháp YAML và cấu trúc trigger/job.

Lỗi được tìm và sửa trong đợt test: sửa năm học làm học kỳ nằm ngoài khoảng; nhập dữ liệu không phải số làm mất ngưỡng GPA hoặc lưu tín chỉ thành 0; tranh quota đồng thời trả lỗi 500; CV chấp nhận loại ngoài PDF/DOC/DOCX hoặc tệp rỗng. Các trường hợp này đã có regression test.

SMTP chỉ kiểm tra giao thư vào outbox local, chưa xác nhận hộp thư thật. Workflow chưa chạy trên GitHub vì chưa push. Chưa kiểm tra nghiệp vụ giảng viên phản biện hoặc sinh viên chọn đề tài vì chưa có các luồng đó. Không thay đổi issue/commit trên GitHub.

Bản tích hợp nhóm chạy thêm `database/migrations/003_team_integration.sql`, tổng cộng 29 bảng. Xem [TEAM_INTEGRATION.md](TEAM_INTEGRATION.md) trước khi nâng cấp database đã dùng bản của các nhánh nhóm.
