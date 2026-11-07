# Thiết kế dữ liệu và ràng buộc — Đề tài 36

Tài liệu thiết kế cho issue #2. Các sơ đồ đầu mô tả dữ liệu **đã triển khai** sau bản nâng cấp backlog Sprint 1–2. Sơ đồ cuối là **thiết kế dự kiến**, chưa phải các bảng có trong phần mềm.

## Tài khoản và phạm vi

```mermaid
erDiagram
 AspNetUsers ||--o{ AspNetUserRoles : has
 AspNetRoles ||--o{ AspNetUserRoles : grants
 AspNetUserRoles ||--o{ user_role_scopes : scoped
 faculties ||--o{ majors : contains
 faculties ||--o{ user_role_scopes : target
 majors ||--o{ user_role_scopes : target
 campaigns ||--o{ user_role_scopes : target
 AspNetUsers { string Id PK }
 AspNetRoles { string Id PK }
 AspNetUserRoles { string UserId PK,FK
 string RoleId PK,FK }
 user_role_scopes { ulong id PK
 string UserId FK
 string RoleId FK
 string scope_type
 ulong faculty_id FK
 ulong major_id FK
 ulong campaign_id FK }
 faculties { ulong id PK }
 majors { ulong id PK
 ulong faculty_id FK }
 campaigns { ulong id PK }
```

Một tài khoản có nhiều vai trò. Phạm vi gắn với cặp tài khoản–vai trò đang tồn tại; khi bỏ vai trò, phạm vi đó không còn hiệu lực. GLOBAL không có mục tiêu; FACULTY/MAJOR/CAMPAIGN chỉ có một mục tiêu tương ứng. Admin quản lý hệ thống; TrainingManager và FacultyManager thực hiện nghiệp vụ trong phạm vi được cấp. Lecturer bao gồm chức năng hướng dẫn hiện có; vai trò phản biện riêng thuộc giai đoạn đánh giá sau này.

## Học vụ, hồ sơ và đề tài

```mermaid
erDiagram
 academic_years ||--o{ terms : contains
 terms ||--o{ campaigns : schedules
 faculties ||--o{ majors : contains
 majors ||--o{ campaigns : organizes
 majors ||--o{ student_classes : contains
 student_classes o|--o{ student_profiles : groups
 majors ||--o{ student_profiles : studies
 AspNetUsers ||--o| student_profiles : owns
 AspNetUsers ||--o| lecturer_profiles : owns
 faculties ||--o{ lecturer_profiles : employs
 student_profiles ||--o{ student_gpa_records : records
 terms ||--o{ student_gpa_records : period
 campaigns ||--o{ lecturer_campaign_quotas : sets
 lecturer_profiles ||--o{ lecturer_campaign_quotas : receives
 campaigns ||--o{ topics : offers
 lecturer_profiles ||--o{ topics : proposes
 topics ||--o{ topic_approval_history : tracks
 topics o|--o{ file_attachments : documents
 student_profiles o|--o{ file_attachments : cv
 academic_years { ulong id PK }
 terms { ulong id PK
 ulong academic_year_id FK }
 campaigns { ulong id PK
 ulong term_id FK
 ulong major_id FK }
 student_profiles { ulong id PK
 string user_id FK
 ulong major_id FK
 ulong class_id FK }
 lecturer_profiles { ulong id PK
 string user_id FK
 ulong faculty_id FK }
 student_gpa_records { ulong id PK
 ulong student_profile_id FK
 ulong term_id FK
 decimal gpa_4
 decimal gpa_10
 string source_type }
 topics { ulong id PK
 ulong campaign_id FK
 ulong lecturer_profile_id FK
 string status }
 topic_approval_history { ulong id PK
 ulong topic_id FK
 string actor_user_id FK }
 file_attachments { ulong id PK
 ulong topic_id FK
 ulong student_profile_id FK }
```

GPA 4 và GPA 10 được lưu riêng theo kỳ, nguồn, người ghi và thời điểm. Không suy diễn thang này từ thang kia. GPA demo mang nguồn MANUAL; API trường chưa triển khai. Mã đề tài duy nhất trong một đợt. Nháp/Yêu cầu sửa mới được chỉnh sửa; người sở hữu mới gửi duyệt; cán bộ đúng phạm vi mới xét duyệt. File riêng tư nằm ngoài wwwroot, kiểm tra quyền ở endpoint tải.

## Lịch, điều kiện, doanh nghiệp và phân công

```mermaid
erDiagram
 campaigns ||--o{ campaign_windows : configures
 campaigns ||--o| campaign_eligibility_policies : requires
 campaigns ||--o{ campaign_exceptions : permits
 campaigns ||--o{ student_eligibility_evidence : verifies
 student_profiles ||--o{ student_eligibility_evidence : provides
 AspNetUsers ||--o{ companies : registers
 majors ||--o{ companies : verifies
 companies ||--o{ company_mentors : designates
 AspNetUsers ||--o{ company_mentors : mentors
 campaigns ||--o{ student_assignments : assigns
 student_profiles ||--o{ student_assignments : joins
 lecturer_profiles ||--o{ student_assignments : supervises
 company_mentors o|--o{ student_assignments : supervises
 campaign_windows { ulong Id PK
 ulong CampaignId FK
 string Stage
 datetime OpensAt
 datetime ClosesAt }
 campaign_eligibility_policies { ulong Id PK
 ulong CampaignId FK
 decimal MinimumGpa4
 decimal MinimumGpa10
 uint MinimumCredits }
 campaign_exceptions { ulong Id PK
 ulong CampaignId FK
 string UserId FK
 string ApprovedBy FK
 string Reason
 datetime ExpiresAt }
 student_eligibility_evidence { ulong Id PK
 ulong CampaignId FK
 ulong StudentProfileId FK
 string RecordedBy FK }
 companies { ulong Id PK
 string OwnerUserId FK
 ulong MajorId FK
 string Status }
 company_mentors { ulong Id PK
 ulong CompanyId FK
 string UserId FK }
 student_assignments { ulong Id PK
 ulong CampaignId FK
 ulong StudentProfileId FK
 ulong LecturerProfileId FK
 ulong CompanyMentorId FK
 string AssignedBy FK }
```

- Mỗi giai đoạn trong đợt có một cặp mở/đóng, nằm trong ngày đợt và không chồng lấn giai đoạn khác. Thứ tự: đăng ký → phân công → nộp → đánh giá. Thời gian nhập theo giờ Việt Nam.
- Chỉ áp dụng GPA, tín chỉ, tiên quyết khi được cấu hình; dữ liệu thiếu không được mặc định là đạt. Nguồn tín chỉ/tiên quyết và người xác nhận được lưu.
- Ngoại lệ thời hạn phải có tài khoản, giai đoạn, thời điểm hết hiệu lực, lý do và cán bộ duyệt; không mở lại đợt đã đóng/lưu trữ.
- Doanh nghiệp PENDING → APPROVED/REJECTED; quyết định có lý do, người xét và thời điểm. Ngành đăng ký quyết định phạm vi khoa xét duyệt. Đăng ký hợp tác nhiều ngành là nhiều yêu cầu riêng, hiện mã doanh nghiệp được duy nhất toàn hệ thống nên muốn đổi phạm vi cần cán bộ xử lý ở bản mở rộng.
- Một sinh viên chỉ có một phân công trong một đợt. Thực tập bắt buộc có người hướng dẫn doanh nghiệp đã xác minh; giảng viên và sinh viên phải thuộc khoa/ngành hợp lệ.
- Hạn mức đếm số **sinh viên đã phân công**, không đếm tổng chỉ tiêu đề xuất trên các đề tài. Phân công vượt quota phải được cán bộ xác nhận cùng lý do; không hạ quota dưới số đã phân công.

## Thiết kế tổng thể cho các sprint tiếp theo — chưa triển khai

```mermaid
erDiagram
 campaigns ||--o{ project_groups : allows
 project_groups ||--o{ group_members : contains
 student_profiles ||--o{ group_members : confirms
 student_profiles ||--o{ topic_preferences : ranks
 topics ||--o{ topic_preferences : chosen
 campaigns ||--o{ project_enrollments : admits
 topics ||--o{ project_enrollments : selected
 project_groups o|--o{ project_enrollments : performs
 student_assignments ||--o{ progress_reports : tracks
 progress_reports ||--o{ report_versions : versions
 rubric_templates ||--o{ rubric_criteria : defines
 rubric_templates ||--o{ assessments : evaluates
 student_assignments ||--o{ assessments : receives
 assessments ||--o{ assessment_scores : contains
 rubric_criteria ||--o{ assessment_scores : scores
 project_groups { ulong id PK
 ulong campaign_id FK }
 group_members { ulong id PK
 ulong group_id FK
 ulong student_profile_id FK
 string confirmation_status }
 topic_preferences { ulong id PK
 ulong student_profile_id FK
 ulong topic_id FK
 int priority }
 project_enrollments { ulong id PK
 ulong campaign_id FK
 ulong topic_id FK
 ulong project_group_id FK }
 progress_reports { ulong id PK
 ulong assignment_id FK }
 report_versions { ulong id PK
 ulong report_id FK
 int version }
 rubric_templates { ulong id PK }
 rubric_criteria { ulong id PK
 ulong template_id FK }
 assessments { ulong id PK
 ulong template_id FK
 ulong assignment_id FK }
 assessment_scores { ulong id PK
 ulong assessment_id FK
 ulong criterion_id FK }
```

Thực tập là phân công cá nhân. Nhóm đồ án chỉ được bật theo chính sách từng đợt; cần thành viên xác nhận, một SV không thuộc hai nhóm hiệu lực cùng đợt. Khi triển khai, đăng ký đề tài phải kiểm tra APPROVED ở server, thời hạn, điều kiện và quota trong giao dịch. Rubric có trọng số và phiên bản; điểm công bố/khóa tách khỏi bản nháp.

Các chính sách quy mô nhóm, điều kiện tín chỉ/tiên quyết, cách chốt GPA và quy trình phê duyệt ngoại lệ phải được giảng viên xác nhận. Giao diện hiện cho cấu hình các điều kiện được hỗ trợ, không khẳng định đây là quy định chính thức của trường.
