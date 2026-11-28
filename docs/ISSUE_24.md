# #24 — Xem trước kết quả ghép và thống kê nguyện vọng

Trong **Quản lý đợt**, chọn nút **Xem trước kết quả ghép** ở dòng của đợt.
Trang `/MatchingPreview/Index/{campaignId}` hiển thị sinh viên, các nguyện vọng đăng ký,
đề tài dự kiến, nguyện vọng được đáp ứng và trạng thái ghép.

## Cách tính và phạm vi

- Danh sách xét ghép giữ quy tắc hiện tại của `RunMatchingAsync`: tài khoản có vai trò
  Student thuộc ngành của đợt. Chưa đổi sang danh sách nhóm/đăng ký tham gia.
- Tỷ lệ NV1/2/3 = số sinh viên được ghép đúng NV tương ứng / tổng sinh viên trong
  danh sách xét ghép. Sinh viên chưa ghép vẫn thuộc mẫu số; danh sách rỗng trả về 0%.
  Ghép ngoài nguyện vọng không tính vào NV1/2/3.
- Chỉ nguyện vọng tới đề tài đã duyệt được dùng để ghép; nguyện vọng không hợp lệ vẫn
  hiện trong bảng để cán bộ phát hiện sai lệch.
- Dùng chung bộ lập kế hoạch cho xem trước và `RunMatchingAsync`, dựa trên
  `GreedyMatcher`, thứ tự đăng ký/ngẫu nhiên và chính sách không có nguyện vọng.
  Kết quả đã ghép được giữ nguyên và trừ khỏi chỉ tiêu còn lại.

## Xác nhận

- GET chỉ đọc, không tạo/sửa kết quả.
- Admin hoặc TrainingManager/FacultyManager có phạm vi quản lý phù hợp mới xem và
  xác nhận được; POST kiểm tra lại quyền và anti-forgery token.
- Token xem trước có chữ ký/mã hóa, gắn với cán bộ, đợt và toàn bộ đầu vào; hết hạn
  sau 30 phút. Với ghép ngẫu nhiên chưa cấu hình seed, seed sinh ở lúc xem trước được
  giữ đến lúc xác nhận.
- Nếu nguyện vọng, sinh viên, chỉ tiêu, trạng thái/cấu hình đợt hoặc kết quả đã lưu
  thay đổi thì phải tải lại bản xem trước. Không tin danh sách ghép do trình duyệt gửi.
- Lưu toàn bộ kết quả trong một transaction Serializable, khóa đợt và đề tài để
  phối hợp với thao tác ghép thủ công. Lỗi lưu hoàn tác toàn bộ; gửi lại token cũ
  không ghi đè kết quả mới.
- Chặn xác nhận ở đợt lưu trữ, khi không có thay đổi, hoặc phát hiện kết quả trùng,
  vượt chỉ tiêu, đề tài không hợp lệ hay sinh viên đã ghép ngoài danh sách ngành.

Xác nhận chỉ lưu vào `topic_student_assignments`; **không công bố kết quả** (#26).
Không thêm bảng hoặc migration.

## Kiểm thử

```sh
dotnet test InternshipManagement.slnx
```

Kiểm tra MySQL và HTTP dùng máy chủ MySQL tạm riêng ở **127.0.0.1:33324**,
schema **issue24_test** chưa tồn tại. Tuyệt đối không trỏ vào CSDL ứng dụng.
Harness tự tạo schema và fixture, không chạy startup/seed của web chính.

```sh
MATCHING_TEST_MYSQL='Server=127.0.0.1;Port=33324;Database=issue24_test;Uid=root;SslMode=Disabled' \
MATCHING_ALLOW_FIXTURES=1 \
dotnet run --project tests/MatchingPreviewChecks --no-launch-profile
```

Harness kiểm tra render Razor, vai trò/phạm vi, anti-forgery, token giả, quyền bị
thu hồi, dữ liệu thay đổi, rollback khi DB lỗi, xác nhận đồng thời, gửi lại token,
giữ kết quả đã lưu và tính nhất quán của ghép ngẫu nhiên. Web test dùng cổng tạm do
hệ điều hành cấp và tự tắt. Xác thực giả chỉ nằm trong project test.
