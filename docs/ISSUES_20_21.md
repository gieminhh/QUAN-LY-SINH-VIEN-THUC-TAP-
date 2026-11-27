# Issues #20 / #21 – Đăng ký nhóm và ưu tiên sinh viên

Triển khai độc lập từ main `60f6f25`, trong nhánh `codex/issues-20-21-groups-ranking`. Các bảng mới được thêm bằng SQL thủ công; không tự nâng cấp hoặc ghi vào CSDL khi build. Các luồng xác thực, hồ sơ, đề tài và ghép cá nhân hiện có không bị thay thế.

## Cài đặt

Sao lưu CSDL triển khai, dùng EF migration `20261122021530_AddParticipationAndLecturerPriorities` hoặc áp dụng `database/migrations/006_issues20_21_participation.sql` một lần vào schema main đã có migration `20261121161226_AddNoPreferencePolicy`. Tệp SQL được xuất từ đúng migration, có cập nhật `__EFMigrationsHistory`; không chạy cả hai cách. Tệp chỉ tạo sáu bảng mới, không có dữ liệu tài khoản. Không dùng CSDL dự án đang làm để thử nghiệm.

Cán bộ có quyền quản lý đợt vào **Chính sách nhóm / ưu tiên** (`/ParticipationPolicies`), chọn đợt và lưu:

- Có cho nhóm đồ án hay chỉ cá nhân; tối đa 2–20 thành viên, bao gồm trưởng nhóm. Thực tập luôn đăng ký từng SV; server từ chối bật nhóm cho thực tập.
- Thời gian mở/đóng đăng ký, dùng giờ Việt Nam và nằm trong khoảng ngày đợt.
- Có bật danh sách ưu tiên GV hay không và thời gian mở/khóa riêng. Mặc định không bật; đợt chưa cấu hình bị khóa.

Không được tắt nhóm hoặc giảm quy mô xuống dưới số thành viên đang đăng ký. Quản lý được điều chỉnh lịch; sau hạn, SV/GV chỉ được xem dữ liệu đã lưu. Không tự suy diễn chính sách chính thức của trường.

## #20 – Luồng sinh viên

Vào **Đăng ký cá nhân / nhóm** (`/ProjectGroups`), chọn đợt thuộc ngành của mình. Chọn đăng ký cá nhân hoặc tạo nhóm nếu chính sách cho phép. Trưởng nhóm được xác nhận tự động, chỉ trưởng nhóm được gửi lời mời cho SV cùng ngành, còn hoạt động và có vai trò sinh viên.

Lời mời chưa tạo thành viên. Người nhận đăng nhập và chọn xác nhận hoặc từ chối; trạng thái hiển thị cho trưởng nhóm. Khi xác nhận, hệ thống kiểm tra lại đợt, hạn, quy mô và đăng ký hiện có. Khóa chính `(CampaignId, StudentId)` cùng giao dịch khóa hàng đợt bảo đảm một SV không thể tham gia hai nhóm/đăng ký đồng thời trong cùng đợt. Một SV có thể đăng ký ở các đợt khác nhau.

Trước hạn, thành viên được rời nhóm; trưởng nhóm được giải tán nhóm, hủy đăng ký và các lời mời của nhóm. Sau khi rời, SV có thể đăng ký cá nhân hoặc nhận lời mời khác. Có thể gửi lại lời mời bị từ chối. Nhóm đồ án này khác nhóm tại doanh nghiệp.

## #21 – Luồng giảng viên

Vào **Ưu tiên sinh viên** (`/LecturerPriorities`), chọn đề tài do mình tạo hoặc được phân công hướng dẫn trong `topic_lecturers`. Chỉ đề tài đã duyệt và công khai được xếp hạng. Ứng viên phải còn hoạt động, đúng ngành của đợt, có vai trò SV và đã chọn đề tài đó trong `StudentTopicPreferences` của main.

GV chọn SV theo thứ tự từ trên xuống, có nút thêm/bỏ/lên/xuống. Server kiểm tra quyền và tập ứng viên, chặn trùng, đánh lại thứ tự liên tục từ 1, ghi tiêu chí/lý do bắt buộc, thời điểm lưu UTC và mã phiên bản. Hai tab lưu đồng thời chỉ một tab thành công. GPA không được dùng để tự xếp; GV có thể để danh sách rỗng có giải thích. Tiêu chí lưu là thông tin phục vụ giải trình, không phải quy định công bằng đã được trường phê duyệt.

Danh sách ưu tiên là dữ liệu đầu vào cho quy trình ghép hai phía khi quy trình đó được triển khai/cấu hình; thay đổi này **không thay thuật toán ghép cá nhân hiện tại**. Tương tự, đăng ký nhóm lưu thành viên đã xác nhận; tự động ghép nguyên nhóm vào đề tài nằm ngoài phạm vi đăng ký của #20.

## Kiểm thử độc lập

Dùng MySQL riêng ở `127.0.0.1:33320`, database `issue2021_demo`, không dùng MySQL của dự án. Các chương trình kiểm thử từ chối CSDL/cổng khác.

1. Đặt `PARTICIPATION_ALLOW_FIXTURES=1` và `PARTICIPATION_TEST_MYSQL` trỏ đúng MySQL riêng. Tạo database trống.
2. `dotnet run --project tests/ParticipationChecks -- --prepare` tạo schema/fixtures.
3. Kiểm chứng SQL triển khai: xóa riêng sáu bảng mới trong database demo theo thứ tự phụ thuộc, tạo `__EFMigrationsHistory` tương ứng main, áp dụng tệp `006` rồi khôi phục các dòng chính sách mẫu (không xóa dữ liệu nền).
4. `dotnet run --project tests/ParticipationChecks -- --check` kiểm tra nghiệp vụ, phân quyền, hạn, phạm vi ứng viên, lưu đồng thời và tính toàn vẹn dữ liệu.
5. Chạy web với `ConnectionStrings__DefaultConnection` trỏ database demo, ở `http://127.0.0.1:5070`; chạy `python3 tests/participation_http_checks.py` với `MYSQL_PWD`/`MYSQL_EXE` tương ứng để kiểm tra trang Razor, đăng nhập, phân quyền và CSRF qua HTTP.

## Kết quả xác minh – 22/11/2026

- Build .NET 10 thành công; một cảnh báo nullable có sẵn ở `TopicsController.cs:537`.
- **33 kiểm tra nghiệp vụ đạt** trên MySQL dùng riêng, bao gồm chấp nhận nhóm và lưu xếp hạng đồng thời, quyền và phạm vi, sức chứa nhóm, cá nhân/nhóm, từ chối/rời/giải tán, khóa sau hạn và GPA không bắt buộc.
- **3 nhóm kiểm tra HTTP đạt**: Razor/đăng nhập, phân quyền/CSRF, ứng viên hợp lệ/lưu thứ tự và chính sách thực tập.
- Tệp SQL migration đã được áp dụng lại trên CSDL kiểm thử trước khi chạy nghiệp vụ; EF xác nhận không còn thay đổi model chưa có migration.
- Không chạy migration, web hay kiểm thử trên CSDL của bản CNPM gốc.
