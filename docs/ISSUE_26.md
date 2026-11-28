# #26 — Công bố, xác nhận ghép và vòng bổ sung

## Luồng sử dụng

1. Cán bộ mở **Công bố kết quả ghép** ở thanh bên, chọn đợt trong phạm vi được duyệt.
   Kết quả vòng chính lấy từ `topic_student_assignments`, do thuật toán/ghép thủ công
   hiện có cung cấp. Đợt phải ở trạng thái **CLOSED** trước khi công bố.
2. Kiểm tra danh sách rồi chọn **Duyệt và công bố**. Hệ thống ghi người duyệt, thời
   gian và một bản kết quả cố định. Thay đổi dữ liệu nội bộ sau đó không tự đổi bản
   đã công bố.
3. Sinh viên mở **Kết quả ghép & bổ sung**. Mỗi sinh viên chỉ thấy kết quả của mình
   trong bản công bố mới nhất; chọn **Xác nhận cặp ghép của tôi**. Gửi lại xác nhận
   không đổi thời gian xác nhận ban đầu.
4. Cán bộ mở vòng bổ sung với hạn đăng ký trong tương lai, thuộc thời gian đợt.
   Form nhập và giao diện hiển thị giờ Việt Nam; lưu trữ thời gian UTC.
5. Sinh viên cùng ngành chưa ghép chọn tối đa 3 đề tài khác nhau đã duyệt còn chỗ.
   Có thể sửa/hủy nguyện vọng trước hạn; không thay đổi nguyện vọng của vòng chính.
6. Sau hạn, cán bộ xem kết quả dự kiến và duyệt công bố vòng mới. Xét theo thứ tự
   đăng ký hoặc ngẫu nhiên theo cấu hình đợt, dùng `GreedyMatcher` và chỉ tiêu còn
   trống. Sinh viên không gửi nguyện vọng bổ sung không được tự ghép.

Các cặp đã công bố, kể cả đang chờ sinh viên xác nhận, luôn được giữ nguyên và
chiếm chỉ tiêu ở vòng sau. Giữ lịch sử từng lần công bố và mang theo xác nhận của
những cặp không đổi. Nếu nội bộ đã gỡ/đổi cặp công bố hoặc giảm chỉ tiêu dưới số
đã ghép, cán bộ phải xử lý sai lệch trước khi mở/công bố vòng mới. Chức năng thu hồi
hoặc chuyển cặp đã công bố nằm ngoài issue này.

Danh sách xét ghép giữ quy tắc của thuật toán hiện tại: tài khoản Student đang hoạt
động thuộc ngành của đợt; chưa thay bằng đơn vị nhóm đồ án. PR này độc lập với
#24/#25, không sửa thuật toán vòng chính hoặc ghép thủ công. Có thể dùng sau khi
các chức năng đó lưu kết quả nội bộ.

## Quyền và tính nhất quán

- Admin hoặc TrainingManager/FacultyManager có scope duyệt phù hợp mới xem danh
  sách cán bộ, mở vòng hoặc duyệt công bố. Kiểm tra cả vai trò đang hoạt động và
  scope ở server, không dựa vào việc ẩn nút.
- Token duyệt gắn với người xem, đợt và toàn bộ đầu vào; hiệu lực 30 phút. Dữ liệu,
  chỉ tiêu, nguyện vọng hoặc xác nhận trước đó thay đổi thì phải xem lại.
- Các POST có anti-forgery. Xác nhận luôn lấy StudentId từ người đăng nhập, đối
  chiếu cặp và vòng công bố mới nhất, không nhận danh tính sinh viên từ form.
- Transaction Serializable và khóa đợt phối hợp mở vòng, đăng ký, công bố và xác
  nhận; công bố khóa thêm các đề tài để đồng bộ với ghép thủ công. Gặp lỗi DB thì
  hoàn tác toàn bộ kết quả mới. Form nguyện vọng có version chống ghi đè phiên khác.
- Sinh viên đọc `matching_published_results` của vòng đã công bố; không đọc cặp
  nháp từ `topic_student_assignments` để hiển thị kết quả.

## CSDL và triển khai

Migration `20261128125053_AddMatchingPublication` chỉ thêm:

- `matching_rounds`: vòng, hạn đăng ký và thông tin duyệt/công bố.
- `matching_published_results`: bản kết quả theo vòng/sinh viên và xác nhận.
- `matching_round_preferences`: nguyện vọng riêng từng vòng bổ sung.

SQL nâng cấp tương ứng ở `database/migrations/issue26_matching_publication.sql`.
Chạy một lần trên CSDL triển khai đã có schema trước issue #26, sau khi chọn đúng
schema và có `__EFMigrationsHistory` như khi dùng EF migrations. Không tự chạy khi
khởi động web. Không áp dụng vào CSDL CNPM local trong quá trình thực hiện issue.

Snapshot/migration giữ nguyên phần `InternshipPosition` đang lệch sẵn giữa model
và snapshot trên main (#31); PR này không tạo/xóa/sửa bảng `internship_positions`.

## Kiểm thử

```sh
dotnet test InternshipManagement.slnx
```

Kiểm tra nâng cấp migration, MySQL và HTTP dùng **máy chủ MySQL tạm riêng** ở
`127.0.0.1:33326`, schema `issue26_test` chưa tồn tại. Harness từ chối endpoint/schema
khác và schema đã tồn tại, không chạy startup/seed của web chính. Web test dùng
cổng tạm và tự tắt; xác thực giả chỉ được đăng ký trong project test.

```sh
PUBLICATION_TEST_MYSQL='Server=127.0.0.1;Port=33326;Database=issue26_test;Uid=root;SslMode=Disabled' \
PUBLICATION_ALLOW_FIXTURES=1 \
dotnet run --project tests/MatchingPublicationChecks --no-launch-profile
```

Harness thử phân quyền/scope, CSRF/token giả, quyền bị thu hồi, bản duyệt cũ,
ẩn kết quả nháp, quyền sở hữu cặp ghép, xác nhận lặp, nguyện vọng bổ sung, hạn đăng
ký, chỉ tiêu, mở/công bố đồng thời và rollback khi công bố vòng chính/vòng bổ sung lỗi.
