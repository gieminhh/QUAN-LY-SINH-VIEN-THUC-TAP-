# Issue #18 – Nguyện vọng đề tài

Sinh viên mở **Nguyện vọng đề tài** trong menu bên trái, chọn đợt thuộc ngành của mình, nhập hướng quan tâm và sắp đề tài theo thứ tự ưu tiên. Hệ thống chỉ nhận đề tài đã duyệt trong đợt, chặn trùng, đánh lại thứ tự liên tục sau khi bỏ dòng, ghi thời điểm lưu và giữ dữ liệu sau khi hết hạn.

Thời hạn lấy từ cửa sổ `REGISTRATION` theo giờ Việt Nam. Khi đợt đóng hoặc hết hạn, trang hiển thị danh sách đã lưu ở chế độ chỉ xem. Mỗi sinh viên có một danh sách theo đợt. Khi hai tab lưu cùng lúc, một lần lưu được chấp nhận và tab còn lại được yêu cầu tải lại.

## Chạy trên một CSDL riêng

1. Chọn một CSDL dành riêng cho bản này. Nếu dùng CSDL trống, thiết lập schema nền và các tệp `000` đến `003` theo [hướng dẫn CSDL](../database/README.md).
2. Áp dụng thủ công [005_issue18_topic_preferences.sql](../database/migrations/005_issue18_topic_preferences.sql) vào đúng CSDL đó. Tệp bổ sung hai bảng. Ứng dụng không tự chạy SQL này khi khởi động.
3. Đặt `ConnectionStrings__DefaultConnection` trỏ tới CSDL riêng, rồi chạy tại thư mục bản này.

   ```powershell
   dotnet run --project InternshipManagement.Web --no-launch-profile --urls http://127.0.0.1:5068
   ```

4. Đăng nhập bằng tài khoản sinh viên của CSDL riêng, mở `/TopicPreferences` và chọn đợt đã mở. Đợt cần cửa sổ đăng ký và đề tài đã được duyệt.

Bộ chấp thuận [topic_preferences_acceptance.py](../tests/topic_preferences_acceptance.py) dùng MySQL demo ở `127.0.0.1:33318`, tên CSDL `issue18_demo` và web ở cổng `5068`. Bộ test thêm dữ liệu mẫu và kiểm tra trực tiếp kết quả ghi, nên chỉ chạy trên bản demo riêng. Cần tài khoản mẫu `student` và `admin` từ SQL nền, biến môi trường `MYSQL_PWD`, `ISSUE18_ALLOW_FIXTURES=1` và đường dẫn `MYSQL_EXE` nếu MySQL client chưa có trong PATH.
