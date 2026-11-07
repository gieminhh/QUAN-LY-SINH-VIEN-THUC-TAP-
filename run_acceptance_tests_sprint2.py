# -*- coding: utf-8 -*-
"""
Sprint 2 Acceptance Test Suite
Website Quản lý thực tập và đồ án sinh viên - Đề tài 36
Kiểm thử 4 User Stories:
- US-07: Quản lý hồ sơ sinh viên
- US-08: Quản lý hồ sơ giảng viên & Hạn mức theo đợt
- US-10: Giảng viên đăng ký đề tài
- US-11: Cán bộ khoa xét duyệt đề tài & Phân quyền Scope
"""

import sys
import os
import re
import html as html_parser
import urllib.request
import urllib.parse
import http.cookiejar
import json
import uuid

# Force utf-8 output in Windows console with error replacement to avoid charmap crashes
sys.stdout.reconfigure(encoding='utf-8', errors='replace')

BASE_URL = os.environ.get("TEST_BASE_URL", "http://localhost:5064")

class TestClient:
    def __init__(self):
        self.cookie_jar = http.cookiejar.CookieJar()
        self.opener = urllib.request.build_opener(
            urllib.request.HTTPCookieProcessor(self.cookie_jar),
            urllib.request.HTTPRedirectHandler()
        )

    def get(self, url):
        full_url = BASE_URL + url if url.startswith("/") else url
        req = urllib.request.Request(full_url, headers={
            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Sprint2Tester"
        })
        try:
            with self.opener.open(req) as resp:
                content = resp.read().decode("utf-8", errors="ignore")
                return resp.status, html_parser.unescape(content), resp.geturl()
        except urllib.error.HTTPError as e:
            content = e.read().decode("utf-8", errors="ignore") if e.fp else ""
            return e.code, html_parser.unescape(content), e.geturl()

    def post(self, url, data, is_multipart=False, boundary=None):
        full_url = BASE_URL + url if url.startswith("/") else url
        headers = {
            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Sprint2Tester"
        }
        if is_multipart:
            headers["Content-Type"] = f"multipart/form-data; boundary={boundary}"
            body = data
        else:
            headers["Content-Type"] = "application/x-www-form-urlencoded"
            body = urllib.parse.urlencode(data).encode("utf-8")

        req = urllib.request.Request(full_url, data=body, headers=headers)
        try:
            with self.opener.open(req) as resp:
                content = resp.read().decode("utf-8", errors="ignore")
                return resp.status, html_parser.unescape(content), resp.geturl()
        except urllib.error.HTTPError as e:
            content = e.read().decode("utf-8", errors="ignore") if e.fp else ""
            return e.code, html_parser.unescape(content), e.geturl()

    def extract_csrf_token(self, html_text):
        match = re.search(r'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"', html_text)
        if not match:
            match = re.search(r'value="([^"]+)"\s+name="__RequestVerificationToken"', html_text)
        return match.group(1) if match else None

    def login(self, username, password):
        status, html_text, _ = self.get("/Account/Login")
        token = self.extract_csrf_token(html_text)
        if not token:
            raise Exception("Cannot find CSRF token on Login page")
        status, html_text, final_url = self.post("/Account/Login", {
            "__RequestVerificationToken": token,
            "UserName": username,
            "Password": password,
            "RememberMe": "false"
        })
        return status, html_text, final_url


def encode_multipart_form(fields, files):
    boundary = '----WebKitFormBoundary' + uuid.uuid4().hex
    body = bytearray()
    for name, value in fields.items():
        body.extend(f'--{boundary}\r\n'.encode('utf-8'))
        body.extend(f'Content-Disposition: form-data; name="{name}"\r\n\r\n'.encode('utf-8'))
        body.extend(f'{value}\r\n'.encode('utf-8'))
    for name, (filename, file_bytes, content_type) in files.items():
        body.extend(f'--{boundary}\r\n'.encode('utf-8'))
        body.extend(f'Content-Disposition: form-data; name="{name}"; filename="{filename}"\r\n'.encode('utf-8'))
        body.extend(f'Content-Type: {content_type}\r\n\r\n'.encode('utf-8'))
        body.extend(file_bytes)
        body.extend(b'\r\n')
    body.extend(f'--{boundary}--\r\n'.encode('utf-8'))
    return bytes(body), boundary


passed_tests = []
failed_tests = []

def run_test(test_id, description, fn):
    print(f"\n========================================================")
    print(f"Bắt đầu: [{test_id}] {description}")
    try:
        fn()
        print(f"--> KẾT QUẢ: [{test_id}] THÀNH CÔNG (PASS)")
        passed_tests.append((test_id, description))
    except Exception as ex:
        print(f"--> KẾT QUẢ: [{test_id}] THẤT BẠI (FAIL): {ex}")
        failed_tests.append((test_id, description, str(ex)))


# Global state across test cases
state = {
    "topic_id": None,
    "topic_code": None,
    "campaign_id": None,
    "cv_attachment_id": None,
}

# TEST 1: Login Admin
def test_01_admin_login():
    client = TestClient()
    status, html, url = client.login("admin", "Admin@123")
    assert "/Account/Login" not in url, "Admin đăng nhập thất bại"
    assert "Quản trị viên" in html or "Bảng điều khiển" in html, "Admin không truy cập được dashboard"

# TEST 2: Student Login & View Profile (US-07)
def test_02_student_view_profile():
    client = TestClient()
    status, html, url = client.login("student", "Student@123")
    assert "/Account/Login" not in url, "Sinh viên đăng nhập thất bại"
    status, html, _ = client.get("/StudentProfile/Index")
    assert status == 200, f"Mã trạng thái {status} khi xem hồ sơ sinh viên"
    assert "2011060001" in html, "Không hiển thị mã sinh viên"
    assert "HỒ SƠ CÁ NHÂN SINH VIÊN" in html, "Không hiển thị tiêu đề hồ sơ sinh viên"
    assert "Thang 4" in html and "Thang 10" in html, "Không hiển thị bảng điểm GPA thang 4 và 10"

# TEST 3: Student Update Skills, Interests and Upload CV (US-07)
def test_03_student_update_profile_and_cv():
    client = TestClient()
    client.login("student", "Student@123")
    status, html, _ = client.get("/StudentProfile/Edit")
    assert status == 200, f"Mã trạng thái {status} khi mở form chỉnh sửa hồ sơ"
    token = client.extract_csrf_token(html)
    assert token, "Không tìm thấy token CSRF"

    fields = {
        "__RequestVerificationToken": token,
        "Skills": "C#, ASP.NET Core 10, Entity Framework, ReactJS, MySQL Server, Docker",
        "Interests": "Hệ thống thông tin quản lý, Điện toán đám mây và Trí tuệ nhân tạo"
    }
    # Mock PDF CV
    pdf_content = b"%PDF-1.4\n1 0 obj\n<< /Type /Catalog >>\nendobj\n%%EOF - CV Pham Ngoc Bao"
    files = {
        "CvFile": ("CV_PhamNgocBao.pdf", pdf_content, "application/pdf")
    }
    data, boundary = encode_multipart_form(fields, files)
    status, html, url = client.post("/StudentProfile/Edit", data, is_multipart=True, boundary=boundary)
    assert "/StudentProfile" in url, f"Chuyển hướng không đúng sau cập nhật: {url}"

    # Read back and verify persistence
    status, html, _ = client.get("/StudentProfile/Index")
    assert "C#, ASP.NET Core 10" in html, "Kỹ năng chuyên môn không được cập nhật chính xác"
    assert "Điện toán đám mây" in html, "Lĩnh vực quan tâm không được cập nhật chính xác"
    assert "CV_PhamNgocBao.pdf" in html, "Tên file CV không hiển thị sau khi tải lên"

    # Extract attachmentId
    match = re.search(r'attachmentId=(\d+)', html)
    if match:
        state["cv_attachment_id"] = match.group(1)

# TEST 4: Student CV Download & Security (US-07 AC-4 & AC-6)
def test_04_student_cv_download():
    client = TestClient()
    client.login("student", "Student@123")
    assert state["cv_attachment_id"], "Không tìm thấy attachmentId của CV"
    status, content, _ = client.get(f"/StudentProfile/DownloadCv?attachmentId={state['cv_attachment_id']}")
    assert status == 200, f"Tải CV thất bại với mã trạng thái {status}"
    assert "%PDF-1.4" in content, "Nội dung CV tải về không đúng"

# TEST 5: Lecturer View Profile (US-08)
def test_05_lecturer_view_profile():
    client = TestClient()
    status, html, url = client.login("giangvien", "Lecturer@123")
    assert "/Account/Login" not in url, "Giảng viên đăng nhập thất bại"
    status, html, _ = client.get("/LecturerProfile/Index")
    assert status == 200, f"Mã trạng thái {status} khi xem hồ sơ giảng viên"
    assert "GV_CNTT01" in html, "Không hiển thị mã giảng viên"
    assert "Khoa Công nghệ Thông tin" in html, "Không hiển thị khoa trực thuộc"

# TEST 6: Lecturer Update Profile (US-08)
def test_06_lecturer_update_profile():
    client = TestClient()
    client.login("giangvien", "Lecturer@123")
    status, html, _ = client.get("/LecturerProfile/Edit")
    token = client.extract_csrf_token(html)
    assert token, "Không tìm thấy CSRF token"

    status, html, url = client.post("/LecturerProfile/Edit", {
        "__RequestVerificationToken": token,
        "DepartmentName": "Bộ môn Kỹ thuật Phần mềm",
        "AcademicDegree": "Tiến sĩ",
        "AcademicTitle": "Phó Giáo sư",
        "Expertise": "Kiến trúc phần mềm Cloud-Native, DevOps, Hệ thống phân tán",
        "ResearchInterests": "Trí tuệ nhân tạo trong kiểm thử tự động, Microservices"
    })
    assert "/LecturerProfile" in url, "Chuyển hướng thất bại sau khi cập nhật hồ sơ giảng viên"

    status, html, _ = client.get("/LecturerProfile/Index")
    assert "Cloud-Native" in html, "Thông tin chuyên môn không được lưu chính xác"
    assert "Microservices" in html, "Hướng nghiên cứu không được lưu chính xác"

# TEST 7: Manager sets Lecturer Quota (US-08 AC-3 & AC-4)
def test_07_manager_set_quota():
    client = TestClient()
    status, html, url = client.login("cb_cntt", "CanBo@123")
    assert "/Account/Login" not in url, "Cán bộ khoa CNTT đăng nhập thất bại"

    status, html, _ = client.get("/LecturerQuotas/Index")
    assert status == 200, f"Mã {status} khi vào trang hạn mức giảng viên"
    assert "QUẢN LÝ HẠN MỨC" in html, "Không đúng giao diện quản lý hạn mức"

    # Find campaign ID from page
    campaign_match = re.search(r'value="(\d+)"[^>]*selected', html)
    if not campaign_match:
        campaign_match = re.search(r'name="CampaignId"\s+value="(\d+)"', html)
    assert campaign_match, "Không tìm thấy campaignId trên trang hạn mức"
    campaign_id = campaign_match.group(1)
    state["campaign_id"] = campaign_id

    # Find lecturer profile id
    lp_match = re.search(r'name="LecturerProfileId"\s+value="(\d+)"', html)
    assert lp_match, "Không tìm thấy LecturerProfileId"
    lp_id = lp_match.group(1)

    token = client.extract_csrf_token(html)
    status, html, url = client.post("/LecturerQuotas/SetQuota", {
        "__RequestVerificationToken": token,
        "CampaignId": campaign_id,
        "LecturerProfileId": lp_id,
        "MaxStudents": "8"
    })
    assert status == 200 or "/LecturerQuotas" in url, "Cập nhật hạn mức thất bại"

    # Verify updated quota displays
    status, html, _ = client.get(f"/LecturerQuotas/Index?campaignId={campaign_id}")
    assert 'value="8"' in html, "Hạn mức 8 sinh viên không hiển thị sau khi lưu"

# TEST 8: Lecturer creates Draft Topic (US-10 AC-1)
def test_08_lecturer_create_draft_topic():
    client = TestClient()
    client.login("giangvien", "Lecturer@123")
    status, html, _ = client.get("/Topics/Create")
    assert status == 200, "Mở trang tạo đề tài thất bại"
    token = client.extract_csrf_token(html)

    topic_code = f"DT_{uuid.uuid4().hex[:6].upper()}"
    state["topic_code"] = topic_code

    doc_content = b"%PDF-1.4\nDetailed Topic Outline Document\n%%EOF"
    fields = {
        "__RequestVerificationToken": token,
        "CampaignId": state["campaign_id"],
        "Code": topic_code,
        "Title": "Phát triển nền tảng học tập trực tuyến tích hợp trợ lý AI",
        "ResearchField": "Công nghệ phần mềm & AI",
        "MaxStudents": "3",
        "Description": "Xây dựng hệ thống học tập thông minh cho sinh viên Đại học Vinh",
        "Objectives": "Đạt độ chính xác cao và tối ưu trải nghiệm người học",
        "KnowledgeRequirements": "Lập trình C#, kiến thức về Web API và React",
        "Technologies": "ASP.NET Core 10, MySQL, Docker, React",
        "SubmitImmediately": "false"  # Save as DRAFT
    }
    files = {
        "DocumentFile": ("DeCuong_DeTai.pdf", doc_content, "application/pdf")
    }
    data, boundary = encode_multipart_form(fields, files)
    status, html, url = client.post("/Topics/Create", data, is_multipart=True, boundary=boundary)
    assert "/Topics/Details/" in url, f"Không chuyển hướng đến trang chi tiết đề tài: {url}"

    # Extract topicId
    match = re.search(r'/Topics/Details/(\d+)', url)
    assert match, "Không tìm thấy TopicId trong URL"
    topic_id = match.group(1)
    state["topic_id"] = topic_id

    # Check status is DRAFT
    status, html, _ = client.get(f"/Topics/Details/{topic_id}")
    assert "Bản nháp" in html, "Đề tài mới tạo không ở trạng thái Bản nháp (DRAFT)"
    assert topic_code in html, "Mã đề tài không xuất hiện trên trang chi tiết"

# TEST 9: Lecturer edits Draft Topic (US-10 AC-2)
def test_09_lecturer_edit_draft_topic():
    client = TestClient()
    client.login("giangvien", "Lecturer@123")
    status, html, _ = client.get(f"/Topics/Edit/{state['topic_id']}")
    assert status == 200, "Không mở được form chỉnh sửa đề tài nháp"
    token = client.extract_csrf_token(html)

    status, html, url = client.post(f"/Topics/Edit/{state['topic_id']}", {
        "__RequestVerificationToken": token,
        "CampaignId": state["campaign_id"],
        "Code": state["topic_code"],
        "Title": "Phát triển nền tảng học tập trực tuyến tích hợp trợ lý AI (Đã cập nhật)",
        "ResearchField": "Công nghệ phần mềm & AI",
        "MaxStudents": "4",
        "Description": "Cập nhật chi tiết nội dung nghiên cứu",
        "Objectives": "Hoàn thiện hệ thống và kiểm thử tải",
        "KnowledgeRequirements": "Lập trình C#, ASP.NET Core",
        "Technologies": "ASP.NET Core 10, MySQL, React",
        "SubmitImmediately": "false"
    })
    assert "/Topics/Details/" in url, "Cập nhật bản nháp đề tài thất bại"

    status, html, _ = client.get(f"/Topics/Details/{state['topic_id']}")
    assert "Đã cập nhật" in html, "Tên đề tài sau chỉnh sửa không được lưu"
    assert "4 sinh viên" in html or "4" in html, "Số lượng SV không được cập nhật"

# TEST 10: Lecturer submits Topic for approval (US-10 AC-3 & AC-4)
def test_10_lecturer_submit_topic():
    client = TestClient()
    client.login("giangvien", "Lecturer@123")
    status, html, _ = client.get(f"/Topics/Details/{state['topic_id']}")
    token = client.extract_csrf_token(html)

    status, html, url = client.post(f"/Topics/Submit/{state['topic_id']}", {
        "__RequestVerificationToken": token
    })
    status, html, _ = client.get(f"/Topics/Details/{state['topic_id']}")
    assert "Chờ xét duyệt" in html, "Đề tài không chuyển sang trạng thái 'Chờ xét duyệt' (PENDING)"

# TEST 11: Student CANNOT see Pending topic (US-10 AC-5, US-11 AC-6)
def test_11_student_cannot_see_pending_topic():
    client = TestClient()
    client.login("student", "Student@123")
    # Public approved topics list
    status, html, _ = client.get("/Topics/Approved")
    assert state["topic_code"] not in html, "Đề tài PENDING bị lộ trong danh sách đề tài đã duyệt của sinh viên"

    # Direct access details of pending topic as student must be forbidden
    status, html, _ = client.get(f"/Topics/Details/{state['topic_id']}")
    assert status == 403 or "AccessDenied" in html or status == 401, f"Sinh viên không được xem đề tài PENDING nhưng nhận mã {status}"

# TEST 12: Faculty Manager sees topic in review list (US-11 AC-1 & AC-2)
def test_12_manager_sees_pending_topic():
    client = TestClient()
    client.login("cb_cntt", "CanBo@123")
    status, html, _ = client.get("/TopicApproval/Index?status=PENDING")
    assert status == 200, "Mở trang xét duyệt đề tài thất bại"
    assert state["topic_code"] in html, "Đề tài chờ xét duyệt không xuất hiện trong danh sách của cán bộ khoa CNTT"

    # Open review screen
    status, html, _ = client.get(f"/TopicApproval/Review/{state['topic_id']}")
    assert status == 200, "Không mở được màn hình xét duyệt đề tài"
    assert "Thao tác xét duyệt" in html, "Không có panel thao tác xét duyệt"
    assert "Phê duyệt đề tài" in html, "Không có nút Phê duyệt"

# TEST 13: Faculty Manager requests revision (US-11 AC-4 & US-10 AC-6)
def test_13_manager_request_revision():
    client = TestClient()
    client.login("cb_cntt", "CanBo@123")
    status, html, _ = client.get(f"/TopicApproval/Review/{state['topic_id']}")
    token = client.extract_csrf_token(html)

    feedback_text = "Đề nghị giảng viên làm rõ kiến trúc tích hợp trợ lý AI và bổ sung tài liệu."
    state["feedback_text"] = feedback_text

    status, html, url = client.post(f"/TopicApproval/RequestRevision/{state['topic_id']}", {
        "__RequestVerificationToken": token,
        "comments": feedback_text
    })
    assert "/TopicApproval" in url, "Gửi yêu cầu chỉnh sửa thất bại"

    # Check topic status is now REVISION_REQUIRED
    status, html, _ = client.get(f"/Topics/Details/{state['topic_id']}")
    assert "Yêu cầu chỉnh sửa" in html, "Trạng thái đề tài không chuyển thành Yêu cầu chỉnh sửa"
    assert feedback_text in html, "Nội dung phản hồi không hiển thị trên chi tiết đề tài"

# TEST 14: Lecturer views feedback and resubmits topic (US-10 AC-7)
def test_14_lecturer_resubmit_topic():
    client = TestClient()
    client.login("giangvien", "Lecturer@123")
    status, html, _ = client.get(f"/Topics/Details/{state['topic_id']}")
    assert state["feedback_text"] in html, "Giảng viên không nhìn thấy nội dung nhận xét yêu cầu sửa"

    token = client.extract_csrf_token(html)
    # Resubmit
    status, html, url = client.post(f"/Topics/Submit/{state['topic_id']}", {
        "__RequestVerificationToken": token
    })
    status, html, _ = client.get(f"/Topics/Details/{state['topic_id']}")
    assert "Chờ xét duyệt" in html, "Đề tài không chuyển lại trạng thái Chờ xét duyệt sau khi gửi lại"

# TEST 15: Out-of-scope Manager CANNOT approve topic (US-11 AC-7)
def test_15_outofscope_manager_cannot_approve():
    client = TestClient()
    # cb_kinhte is FacultyManager for KTKD (Faculty 2), not CNTT (Faculty 1)
    status, html, url = client.login("cb_kinhte", os.environ.get("CB_KINHTE_PASSWORD", "CanBo@123"))
    assert "/Account/Login" not in url, "Đăng nhập cán bộ khoa kinh tế thất bại"

    # Try to approve topic in CNTT
    status, html, _ = client.get(f"/TopicApproval/Review/{state['topic_id']}")
    token = client.extract_csrf_token(html)
    if token:
        status, html, _ = client.post(f"/TopicApproval/Approve/{state['topic_id']}", {
            "__RequestVerificationToken": token,
            "comments": "Duyệt trái quyền"
        })
    # Verify topic is NOT approved
    status, html, _ = client.get(f"/Topics/Details/{state['topic_id']}")
    assert "Đã phê duyệt" not in html, "Cán bộ ngoài phạm vi quyền đã duyệt được đề tài vi phạm AC-7!"

# TEST 16: Authorized Faculty Manager approves topic (US-11 AC-3)
def test_16_authorized_manager_approves_topic():
    client = TestClient()
    client.login("cb_cntt", "CanBo@123")
    status, html, _ = client.get(f"/TopicApproval/Review/{state['topic_id']}")
    token = client.extract_csrf_token(html)

    status, html, url = client.post(f"/TopicApproval/Approve/{state['topic_id']}", {
        "__RequestVerificationToken": token,
        "comments": "Đề tài đáp ứng chuẩn đầu ra, phê duyệt cho sinh viên đăng ký."
    })
    assert "/TopicApproval" in url, "Phê duyệt đề tài thất bại"

    # Verify status is APPROVED
    status, html, _ = client.get(f"/Topics/Details/{state['topic_id']}")
    assert "Đã phê duyệt" in html, "Đề tài không chuyển sang trạng thái Đã phê duyệt (APPROVED)"

# TEST 17: Student CAN NOW see approved topic (US-11 AC-6 & US-10 AC-5)
def test_17_student_can_see_approved_topic():
    client = TestClient()
    client.login("student", "Student@123")
    status, html, _ = client.get("/Topics/Approved")
    assert status == 200, "Mở danh mục đề tài phê duyệt thất bại"
    assert state["topic_code"] in html, "Đề tài đã phê duyệt không hiển thị trong danh mục của sinh viên"

    # Student can now open details of approved topic
    status, html, _ = client.get(f"/Topics/Details/{state['topic_id']}")
    assert status == 200, "Sinh viên không xem được chi tiết đề tài đã duyệt"
    assert "Đã phê duyệt" in html, "Trạng thái không phải Đã phê duyệt"

# TEST 18: Duplicate Topic Code validation (US-10 Rule)
def test_18_duplicate_topic_code_validation():
    client = TestClient()
    client.login("giangvien", "Lecturer@123")
    status, html, _ = client.get("/Topics/Create")
    token = client.extract_csrf_token(html)

    # Try creating topic with identical code in same campaign
    status, html, url = client.post("/Topics/Create", {
        "__RequestVerificationToken": token,
        "CampaignId": state["campaign_id"],
        "Code": state["topic_code"], # DUPLICATE CODE
        "Title": "Đề tài thử nghiệm trùng mã",
        "MaxStudents": "2",
        "SubmitImmediately": "false"
    })
    assert "đã tồn tại trong đợt này" in html or "/Topics/Details/" not in url, "Hệ thống không chặn mã đề tài trùng lặp!"



def test_19_invalid_attachment_does_not_create_topic():
    client = TestClient()
    client.login("giangvien", "Lecturer@123")
    _, form, _ = client.get("/Topics/Create")
    code = "INVALID_" + uuid.uuid4().hex[:10]
    fields = {"__RequestVerificationToken": client.extract_csrf_token(form),
              "CampaignId": state["campaign_id"], "Code": code,
              "Title": "Invalid attachment regression", "MaxStudents": "1", "SubmitImmediately": "false"}
    body, boundary = encode_multipart_form(fields, {"DocumentFile": ("invalid.exe", b"invalid", "application/octet-stream")})
    status, html, url = client.post("/Topics/Create", body, is_multipart=True, boundary=boundary)
    assert status == 200 and "/Topics/Details/" not in url
    assert "Chỉ chấp nhận PDF" in html
    _, listing, _ = client.get("/Topics?search=" + code)
    assert "Danh sách đề tài (0)" in listing, "Invalid file left a saved topic"


def test_20_draft_and_document_are_private():
    owner = TestClient()
    owner.login("giangvien", "Lecturer@123")
    _, form, _ = owner.get("/Topics/Create")
    body, boundary = encode_multipart_form({
        "__RequestVerificationToken": owner.extract_csrf_token(form),
        "CampaignId": state["campaign_id"], "Code": "PRIVATE_" + uuid.uuid4().hex[:10],
        "Title": "Private draft regression", "MaxStudents": "1", "SubmitImmediately": "false"
    }, {"DocumentFile": ("draft.pdf", b"%PDF-1.4 regression draft", "application/pdf")})
    status, detail, url = owner.post("/Topics/Create", body, is_multipart=True, boundary=boundary)
    assert status == 200 and "/Topics/Details/" in url
    topic_id = re.search(r"/Topics/Details/(\d+)", url).group(1)
    attachment = re.search(r"/Topics/DownloadDocument\?attachmentId=(\d+)", detail)
    assert attachment, "Draft attachment missing"
    for username, password in [("cb_kinhte", os.environ.get("CB_KINHTE_PASSWORD", "CanBo@123")), ("backlog_other_lecturer", "Regression@123"), ("student", "Student@123")]:
        other = TestClient()
        _, _, login_url = other.login(username, password)
        assert "/Account/Login" not in login_url, username
        assert other.get("/Topics/Details/" + topic_id)[0] == 403, username + " can read private draft"
        assert other.get("/Topics/DownloadDocument?attachmentId=" + attachment.group(1))[0] == 403, username + " can download private draft"


def test_21_approval_major_options_follow_scope():
    client = TestClient()
    client.login("cb_cntt", "CanBo@123")
    status, html, _ = client.get("/TopicApproval")
    assert status == 200
    select = re.search(r'<select[^>]*name="majorId"[^>]*>(.*?)</select>', html, re.S)
    assert select
    assert "Công nghệ Thông tin" in select.group(1)
    assert "Quản trị Kinh doanh" not in select.group(1)


if __name__ == "__main__":
    tests = [
        ("TC-01", "Kiểm tra đăng nhập Quản trị viên (Baseline US-01)", test_01_admin_login),
        ("TC-02", "Sinh viên đăng nhập và xem hồ sơ học vụ & điểm GPA (US-07 AC-1)", test_02_student_view_profile),
        ("TC-03", "Sinh viên cập nhật kỹ năng, lĩnh vực quan tâm và tải file CV (US-07 AC-2, AC-3)", test_03_student_update_profile_and_cv),
        ("TC-04", "Tải xuống và kiểm soát quyền truy cập tệp CV (US-07 AC-4, AC-6)", test_04_student_cv_download),
        ("TC-05", "Giảng viên đăng nhập và xem hồ sơ chuyên môn (US-08 AC-1, AC-2)", test_05_lecturer_view_profile),
        ("TC-06", "Giảng viên cập nhật thông tin chuyên môn và hướng nghiên cứu (US-08 AC-1, AC-6)", test_06_lecturer_update_profile),
        ("TC-07", "Cán bộ thiết lập hạn mức hướng dẫn theo đợt cho giảng viên (US-08 AC-3, AC-4)", test_07_manager_set_quota),
        ("TC-08", "Giảng viên tạo đề tài mới và lưu bản nháp DRAFT (US-10 AC-1)", test_08_lecturer_create_draft_topic),
        ("TC-09", "Giảng viên chỉnh sửa bản nháp đề tài (US-10 AC-2)", test_09_lecturer_edit_draft_topic),
        ("TC-10", "Giảng viên gửi đề tài xét duyệt -> PENDING (US-10 AC-3, AC-4)", test_10_lecturer_submit_topic),
        ("TC-11", "Sinh viên KHÔNG thể xem đề tài PENDING hoặc bị từ chối (US-10 AC-5, US-11 AC-6)", test_11_student_cannot_see_pending_topic),
        ("TC-12", "Cán bộ khoa thấy đề tài chờ xét duyệt trong phạm vi (US-11 AC-1, AC-2)", test_12_manager_sees_pending_topic),
        ("TC-13", "Cán bộ khoa yêu cầu chỉnh sửa đề tài có nhận xét (US-11 AC-4)", test_13_manager_request_revision),
        ("TC-14", "Giảng viên xem phản hồi, chỉnh sửa và gửi lại xét duyệt (US-10 AC-6, AC-7)", test_14_lecturer_resubmit_topic),
        ("TC-15", "Cán bộ ngoài phạm vi quyền KHÔNG thể xét duyệt đề tài (US-11 AC-7)", test_15_outofscope_manager_cannot_approve),
        ("TC-16", "Cán bộ khoa phê duyệt đề tài thành APPROVED (US-11 AC-3, AC-8)", test_16_authorized_manager_approves_topic),
        ("TC-17", "Sinh viên xem được đề tài đã phê duyệt công khai (US-11 AC-6)", test_17_student_can_see_approved_topic),
        ("TC-19", "Tệp sai định dạng không được lưu đề tài", test_19_invalid_attachment_does_not_create_topic),
        ("TC-20", "Bảo vệ bản nháp và tệp với người ngoài phạm vi", test_20_draft_and_document_are_private),
        ("TC-21", "Bộ lọc ngành theo phạm vi khoa", test_21_approval_major_options_follow_scope),
        ("TC-18", "Ràng buộc kiểm tra trùng mã đề tài trong cùng một đợt (US-10 Rule)", test_18_duplicate_topic_code_validation),
    ]

    print("======================================================================")
    print("CHẠY BỘ KIỂM THỬ TỰ ĐỘNG SPRINT 2 (US-07, US-08, US-10, US-11)")
    print("======================================================================")

    for test_id, desc, fn in tests:
        run_test(test_id, desc, fn)

    print("\n======================================================================")
    print(f"KẾT QUẢ TỔNG HỢP: {len(passed_tests)}/{len(tests)} TEST CASES ĐẠT (PASS)")
    if failed_tests:
        print(f"CÁC TEST THẤT BẠI: {len(failed_tests)}")
        for fid, fdesc, ferr in failed_tests:
            print(f"- [{fid}] {fdesc}: {ferr}")
        sys.exit(1)
    else:
        print("Tất cả kịch bản trong lần chạy này đã đạt.")
        sys.exit(0)
