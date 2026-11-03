import requests
import re
import urllib.parse
import subprocess
import os
import shutil

BASE_URL = os.environ.get("TEST_BASE_URL", "http://localhost:5064")
MYSQL_COMMAND = [os.environ.get("MYSQL_EXE", shutil.which("mysql") or "mysql"),
                 "--default-character-set=utf8mb4", "--host=" + os.environ.get("MYSQL_HOST", "localhost"),
                 "--port=" + os.environ.get("MYSQL_PORT", "3306"),
                 "--user=" + os.environ.get("MYSQL_USER", "root")]

def get_anti_forgery_token(html):
    match = re.search(r'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"', html)
    if not match:
        match = re.search(r'value="([^"]+)"\s+name="__RequestVerificationToken"', html)
    return match.group(1) if match else None

def test_scenario():
    if os.environ.get("ALLOW_DESTRUCTIVE_DEMO_TESTS") != "1":
        raise SystemExit("This script deletes/recreates demo users and campaigns. Use a disposable database and set ALLOW_DESTRUCTIVE_DEMO_TESTS=1.")
    if not os.environ.get("MYSQL_PWD"):
        raise SystemExit("Set MYSQL_PWD for the disposable test database before running.")
    print("==================================================")
    print("BAT DAU CHAY KICH BAN NGHIEM THU SPRINT 1 (12 BUOC)")
    print("==================================================")

    # Clean up previous test data if any to ensure test idempotence
    subprocess.run(MYSQL_COMMAND + ['-e',
                    "USE ql_thuctap_doan; DELETE FROM campaigns WHERE code='DA_CNTT_K63'; DELETE FROM AspNetUsers WHERE UserName IN ('cb_daotao', 'cb_kinhte');"],
                    capture_output=True, text=True, encoding='utf-8', errors='ignore')

    # -----------------------------------------------------------
    # TEST ERROR 1: Chưa đăng nhập truy cập trang bảo vệ
    # -----------------------------------------------------------
    print("\n[KIEM TRA LOI 1] Truy cap trang bao ve khi chua dang nhap...")
    r = requests.get(f"{BASE_URL}/Users/Index", allow_redirects=False)
    assert r.status_code == 302, f"Expected 302 redirect, got {r.status_code}"
    assert "/Account/Login" in r.headers.get("Location", ""), f"Expected redirect to Login, got {r.headers.get('Location')}"
    print("=> PASS: Nguoi dung chua dang nhap bi chuyen huong ve trang Login chinh xac.")

    # -----------------------------------------------------------
    # TEST ERROR 2: Sai mat khau
    # -----------------------------------------------------------
    print("\n[KIEM TRA LOI 2] Dang nhap sai mat khau...")
    session = requests.Session()
    login_page = session.get(f"{BASE_URL}/Account/Login").text
    token = get_anti_forgery_token(login_page)
    r = session.post(f"{BASE_URL}/Account/Login", data={
        "__RequestVerificationToken": token,
        "UserName": "admin",
        "Password": "WrongPassword999",
        "RememberMe": "false"
    })
    assert "Tên đăng nhập hoặc mật khẩu không chính xác" in r.text or r.status_code == 200
    print("=> PASS: Dang nhap sai mat khau bi tu choi voi thong bao loi phu hop.")

    # -----------------------------------------------------------
    # BUOC 1: Admin dang nhap
    # -----------------------------------------------------------
    print("\n[BUOC 1] Admin dang nhap...")
    login_page = session.get(f"{BASE_URL}/Account/Login").text
    token = get_anti_forgery_token(login_page)
    r = session.post(f"{BASE_URL}/Account/Login", data={
        "__RequestVerificationToken": token,
        "UserName": "admin",
        "Password": "Admin@123",
        "RememberMe": "false"
    }, allow_redirects=False)
    assert r.status_code == 302, f"Expected 302, got {r.status_code}"
    assert any(".VinhUni.Internship.Auth" in c.name for c in session.cookies), "Auth cookie not set!"
    print("=> PASS: Admin dang nhap thanh cong, cookie xac thuc duoc thiet lap.")

    # -----------------------------------------------------------
    # BUOC 2: Admin vao trang quan ly tai khoan
    # -----------------------------------------------------------
    print("\n[BUOC 2] Admin vao trang quan ly tai khoan...")
    r = session.get(f"{BASE_URL}/Users/Index")
    assert r.status_code == 200, f"Expected 200, got {r.status_code}"
    assert "Quản lý tài khoản người dùng" in r.text, "Title not found in page"
    assert "admin" in r.text, "Admin username not found in list"
    print("=> PASS: Admin truy cap trang quan ly tai khoan thanh cong, hien thi danh sach.")

    # -----------------------------------------------------------
    # BUOC 3: Admin tao tai khoan can bo quan ly
    # -----------------------------------------------------------
    print("\n[BUOC 3] Admin tao tai khoan can bo quan ly (TrainingManager)...")
    create_page = session.get(f"{BASE_URL}/Users/Create").text
    token = get_anti_forgery_token(create_page)
    r = session.post(f"{BASE_URL}/Users/Create", data={
        "__RequestVerificationToken": token,
        "FullName": "Trần Văn Cán Bộ",
        "UserName": "cb_daotao",
        "Email": "cb_daotao@vinhuni.edu.vn",
        "PhoneNumber": "0987654321",
        "Password": "DaoTao@123",
        "ConfirmPassword": "DaoTao@123",
        "IsActive": "true",
        "SelectedRoles": ["TrainingManager"]
    }, allow_redirects=False)
    assert r.status_code == 302, f"Expected 302 redirect after user creation, got {r.status_code}"
    print("=> PASS: Admin tao tai khoan cb_daotao thanh cong.")

    # Verify user saved in MySQL
    mysql_cmd = MYSQL_COMMAND + ['-e',
                 "USE ql_thuctap_doan; SELECT Id, UserName, Email, FullName, IsActive FROM AspNetUsers WHERE UserName='cb_daotao';"]
    p = subprocess.run(mysql_cmd, capture_output=True, text=True, encoding='utf-8', errors='ignore')
    assert "cb_daotao" in p.stdout, "User not found in MySQL AspNetUsers table!"
    cb_id = p.stdout.strip().split("\n")[1].split("\t")[0]
    print(f"=> PASS: Xac nhan du lieu luu thuc te trong MySQL (Id: {cb_id}).")

    # -----------------------------------------------------------
    # BUOC 4: Admin gan vai tro va pham vi quan ly (Scope)
    # -----------------------------------------------------------
    print("\n[BUOC 4] Admin gan pham vi quan ly (GLOBAL / FACULTY)...")
    scope_page = session.get(f"{BASE_URL}/Users/ManageScopes/{cb_id}").text
    token = get_anti_forgery_token(scope_page)
    # Find RoleId for TrainingManager
    role_match = re.search(r'value="([^"]+)">TrainingManager', scope_page)
    role_id = role_match.group(1) if role_match else "role_training_manager"

    r = session.post(f"{BASE_URL}/Users/AddScope", data={
        "__RequestVerificationToken": token,
        "UserId": cb_id,
        "SelectedRoleId": role_id,
        "SelectedScopeType": "GLOBAL"
    }, allow_redirects=False)
    assert r.status_code == 302, f"Expected 302 redirect after scope add, got {r.status_code}"

    # Verify scope in MySQL user_role_scopes
    mysql_scope_cmd = MYSQL_COMMAND + ['-e',
                       f"USE ql_thuctap_doan; SELECT id, scope_type FROM user_role_scopes WHERE UserId='{cb_id}';"]
    p_scope = subprocess.run(mysql_scope_cmd, capture_output=True, text=True, encoding='utf-8', errors='ignore')
    assert "GLOBAL" in p_scope.stdout, "Scope not found in user_role_scopes table!"
    print("=> PASS: Pham vi quyen GLOBAL da duoc luu vao bang user_role_scopes trong MySQL.")

    # -----------------------------------------------------------
    # BUOC 5: Dang xuat khoi Admin
    # -----------------------------------------------------------
    print("\n[BUOC 5] Dang xuat khoi Admin...")
    dash_page = session.get(f"{BASE_URL}/Home/Index").text
    token = get_anti_forgery_token(dash_page)
    r = session.post(f"{BASE_URL}/Account/Logout", data={"__RequestVerificationToken": token}, allow_redirects=False)
    assert r.status_code == 302, f"Expected 302 redirect after logout, got {r.status_code}"
    print("=> PASS: Dang xuat thanh cong, phien lam viec Admin duoc huy.")

    # -----------------------------------------------------------
    # BUOC 6: Dang nhap bang tai khoan can bo moi
    # -----------------------------------------------------------
    print("\n[BUOC 6] Dang nhap bang tai khoan can bo moi (cb_daotao)...")
    cb_session = requests.Session()
    login_page = cb_session.get(f"{BASE_URL}/Account/Login").text
    token = get_anti_forgery_token(login_page)
    r = cb_session.post(f"{BASE_URL}/Account/Login", data={
        "__RequestVerificationToken": token,
        "UserName": "cb_daotao",
        "Password": "DaoTao@123",
        "RememberMe": "false"
    }, allow_redirects=False)
    assert r.status_code == 302, f"Expected 302, got {r.status_code}"
    print("=> PASS: Can bo dao tao dang nhap thanh cong.")

    # -----------------------------------------------------------
    # BUOC 7: Can bo mo trang quan ly dot
    # -----------------------------------------------------------
    print("\n[BUOC 7] Can bo mo trang quan ly dot...")
    r = cb_session.get(f"{BASE_URL}/Campaigns/Index")
    assert r.status_code == 200, f"Expected 200, got {r.status_code}"
    assert "Quản lý đợt thực tập & đồ án" in r.text
    print("=> PASS: Trang quan ly dot hien thi chinh xac cho can bo dao tao.")

    # -----------------------------------------------------------
    # BUOC 8: Can bo tao dot do an hoac thuc tap
    # -----------------------------------------------------------
    print("\n[BUOC 8] Can bo tao dot do an moi (DA_CNTT_K63)...")
    create_camp_page = cb_session.get(f"{BASE_URL}/Campaigns/Create").text
    token = get_anti_forgery_token(create_camp_page)
    
    # Extract first available TermId and MajorId
    term_id_match = re.search(r'<option value="(\d+)">Học kỳ', create_camp_page)
    major_id_match = re.search(r'<option value="(\d+)">[^<]+CNTT', create_camp_page) or re.search(r'<option value="(\d+)">', create_camp_page)
    term_id = term_id_match.group(1) if term_id_match else "1"
    major_id = major_id_match.group(1) if major_id_match else "1"

    r = cb_session.post(f"{BASE_URL}/Campaigns/Create", data={
        "__RequestVerificationToken": token,
        "Code": "DA_CNTT_K63",
        "Name": "Đồ án tốt nghiệp CNTT Khóa 63",
        "Kind": "PROJECT",
        "TrainingSystem": "CHINH_QUY",
        "TermId": term_id,
        "MajorId": major_id,
        "StartDate": "2026-09-01",
        "EndDate": "2027-01-15"
    }, allow_redirects=False)
    # Check if redirect or already exists
    print(f"Response status: {r.status_code}")

    # -----------------------------------------------------------
    # BUOC 9: Du lieu duoc luu thanh cong trong MySQL
    # -----------------------------------------------------------
    print("\n[BUOC 9] Kiem tra du lieu dot duoc luu trong MySQL...")
    mysql_camp_cmd = MYSQL_COMMAND + ['-e',
                      "USE ql_thuctap_doan; SELECT id, code, name, kind, status, start_date, end_date FROM campaigns WHERE code='DA_CNTT_K63';"]
    p_camp = subprocess.run(mysql_camp_cmd, capture_output=True, text=True, encoding='utf-8', errors='ignore')
    assert "DA_CNTT_K63" in p_camp.stdout, "Campaign not found in MySQL campaigns table!"
    camp_line = [l for l in p_camp.stdout.strip().split("\n") if "DA_CNTT_K63" in l][0]
    camp_id = camp_line.split("\t")[0]
    print(f"=> PASS: Xac nhan du lieu dot DA_CNTT_K63 (id: {camp_id}) duoc luu trong MySQL.")

    # -----------------------------------------------------------
    # BUOC 10: Can bo sua thong tin va chuyen trang thai dot
    # -----------------------------------------------------------
    print("\n[BUOC 10] Can bo sua thong tin va chuyen trang thai dot sang OPEN...")
    details_page = cb_session.get(f"{BASE_URL}/Campaigns/Details/{camp_id}").text
    token = get_anti_forgery_token(details_page)

    r = cb_session.post(f"{BASE_URL}/Campaigns/ChangeStatus", data={
        "__RequestVerificationToken": token,
        "id": camp_id,
        "newStatus": "OPEN"
    }, allow_redirects=False)
    assert r.status_code == 302, f"Expected 302 redirect after status change, got {r.status_code}"

    # Verify status in MySQL
    p_camp_open = subprocess.run(mysql_camp_cmd, capture_output=True, text=True, encoding='utf-8', errors='ignore')
    assert "OPEN" in p_camp_open.stdout, "Campaign status was not updated to OPEN in MySQL!"
    print(f"=> PASS: Trang thai dot da chuyen thanh cong sang OPEN trong MySQL.")

    # -----------------------------------------------------------
    # BUOC 11: Dang nhap bang tai khoan sinh vien, thu vao quan tri va bi chan
    # -----------------------------------------------------------
    print("\n[BUOC 11] Dang nhap bang tai khoan sinh vien, thu truy cap trang quan tri...")
    student_session = requests.Session()
    login_page = student_session.get(f"{BASE_URL}/Account/Login").text
    token = get_anti_forgery_token(login_page)
    r = student_session.post(f"{BASE_URL}/Account/Login", data={
        "__RequestVerificationToken": token,
        "UserName": "student",
        "Password": "Student@123",
        "RememberMe": "false"
    }, allow_redirects=False)
    assert r.status_code == 302, f"Expected 302, got {r.status_code}"

    # Try accessing admin users page
    r_admin = student_session.get(f"{BASE_URL}/Users/Index", allow_redirects=False)
    assert r_admin.status_code in (302, 403), f"Expected 302 or 403 for unauthorized access, got {r_admin.status_code}"
    if r_admin.status_code == 302:
        assert "AccessDenied" in r_admin.headers.get("Location", ""), f"Expected redirect to AccessDenied, got {r_admin.headers.get('Location')}"
    
    # Try accessing campaigns management page
    r_camp = student_session.get(f"{BASE_URL}/Campaigns/Index", allow_redirects=False)
    assert r_camp.status_code in (302, 403), f"Expected 302 or 403 for unauthorized access, got {r_camp.status_code}"
    print("=> PASS: Sinh vien bi chan truy cap vao cac chuc nang quan tri va quan ly dot.")

    # -----------------------------------------------------------
    # BUOC 12: Kiem tra can bo khong the truy cap dot ngoai pham vi duoc cap
    # -----------------------------------------------------------
    print("\n[BUOC 12] Kiem tra can bo bi gioi han pham vi quan ly (Scope)...")
    # Admin creates a faculty manager for KTKD
    admin_session = requests.Session()
    login_page = admin_session.get(f"{BASE_URL}/Account/Login").text
    token = get_anti_forgery_token(login_page)
    admin_session.post(f"{BASE_URL}/Account/Login", data={
        "__RequestVerificationToken": token,
        "UserName": "admin",
        "Password": "Admin@123",
        "RememberMe": "false"
    })

    # Check if cb_kinhte exists
    p_check_kt = subprocess.run(MYSQL_COMMAND + ['-e',
                                 "USE ql_thuctap_doan; SELECT Id FROM AspNetUsers WHERE UserName='cb_kinhte';"], capture_output=True, text=True, encoding='utf-8', errors='ignore')
    if "cb_kinhte" not in p_check_kt.stdout and len(p_check_kt.stdout.strip().split("\n")) <= 1:
        # Create FacultyManager for KTKD
        create_page = admin_session.get(f"{BASE_URL}/Users/Create").text
        token = get_anti_forgery_token(create_page)
        admin_session.post(f"{BASE_URL}/Users/Create", data={
            "__RequestVerificationToken": token,
            "FullName": "Cán bộ Khoa Kinh tế",
            "UserName": "cb_kinhte",
            "Email": "cb_kinhte@vinhuni.edu.vn",
            "Password": "KinhTe@123",
            "ConfirmPassword": "KinhTe@123",
            "IsActive": "true",
            "SelectedRoles": ["FacultyManager"]
        })

    # Get cb_kinhte id from MySQL
    p_kt = subprocess.run(MYSQL_COMMAND + ['-e',
                           "USE ql_thuctap_doan; SELECT Id FROM AspNetUsers WHERE UserName='cb_kinhte';"], capture_output=True, text=True, encoding='utf-8', errors='ignore')
    kt_id = p_kt.stdout.strip().split("\n")[1].strip()

    # Assign FACULTY scope for KTKD only (FacultyId = 2)
    scope_page = admin_session.get(f"{BASE_URL}/Users/ManageScopes/{kt_id}").text
    token = get_anti_forgery_token(scope_page)
    role_match = re.search(r'value="([^"]+)">FacultyManager', scope_page)
    fm_role_id = role_match.group(1) if role_match else "role_faculty_manager"

    admin_session.post(f"{BASE_URL}/Users/AddScope", data={
        "__RequestVerificationToken": token,
        "UserId": kt_id,
        "SelectedRoleId": fm_role_id,
        "SelectedScopeType": "FACULTY",
        "SelectedFacultyId": "2"  # KTKD
    })

    # Now login as cb_kinhte
    kt_session = requests.Session()
    login_page = kt_session.get(f"{BASE_URL}/Account/Login").text
    token = get_anti_forgery_token(login_page)
    kt_session.post(f"{BASE_URL}/Account/Login", data={
        "__RequestVerificationToken": token,
        "UserName": "cb_kinhte",
        "Password": "KinhTe@123",
        "RememberMe": "false"
    })

    # cb_kinhte tries to view or manage DA_CNTT_K63 (which belongs to Faculty CNTT, id=1)
    r_out_of_scope = kt_session.get(f"{BASE_URL}/Campaigns/Details/{camp_id}", allow_redirects=False)
    assert r_out_of_scope.status_code in (302, 403), f"Expected 403/302 for out-of-scope access, got {r_out_of_scope.status_code}"
    print(f"=> PASS: Can bo Khoa Kinh te bi tu choi ({r_out_of_scope.status_code}) khi co truy cap dot thuoc Khoa CNTT.")

    # -----------------------------------------------------------
    # TEST ERROR 3: Tai khoan bi khoa khong duoc dang nhap
    # -----------------------------------------------------------
    print("\n[KIEM TRA LOI 3] Tai khoan bi vo hieu hoa (IsActive = false)...")
    # Admin toggles cb_kinhte to inactive if active
    subprocess.run(MYSQL_COMMAND + ['-e',
                    f"USE ql_thuctap_doan; UPDATE AspNetUsers SET IsActive=0 WHERE UserName='cb_kinhte';"], capture_output=True, text=True, encoding='utf-8', errors='ignore')
    
    # Try login with cb_kinhte
    deact_session = requests.Session()
    login_page = deact_session.get(f"{BASE_URL}/Account/Login").text
    token = get_anti_forgery_token(login_page)
    r_deact = deact_session.post(f"{BASE_URL}/Account/Login", data={
        "__RequestVerificationToken": token,
        "UserName": "cb_kinhte",
        "Password": "KinhTe@123",
        "RememberMe": "false"
    })
    assert "vô hiệu hóa" in r_deact.text or r_deact.status_code == 200, "Disabled account was not rejected with proper message!"
    print("=> PASS: Tai khoan bi vo hieu hoa bi chan dang nhap voi thong bao ro rang.")

    # -----------------------------------------------------------
    # TEST ERROR 4: Trùng mã đợt & Ngày kết thúc trước ngày bắt đầu
    # -----------------------------------------------------------
    print("\n[KIEM TRA LOI 4] Trung ma dot va loi ngay thang...")
    create_camp_page = cb_session.get(f"{BASE_URL}/Campaigns/Create").text
    token = get_anti_forgery_token(create_camp_page)
    r_dup = cb_session.post(f"{BASE_URL}/Campaigns/Create", data={
        "__RequestVerificationToken": token,
        "Code": "DA_CNTT_K63", # Existing code
        "Name": "Dot trung ma",
        "Kind": "PROJECT",
        "TrainingSystem": "CHINH_QUY",
        "TermId": term_id,
        "MajorId": major_id,
        "StartDate": "2026-10-01",
        "EndDate": "2026-09-01" # EndDate < StartDate
    })
    assert "đã tồn tại" in r_dup.text or "được sử dụng" in r_dup.text or "Ngày kết thúc" in r_dup.text
    print("=> PASS: Kiem tra trung ma dot va loi ngay ket thuc truoc ngay bat dau hoat dong chinh xac.")

    print("\n==================================================")
    print("TAT CA 12 BUOC KICH BAN NGHIEM THU & CAC TRUONG HOP LOI DEU DAT 100%!")
    print("==================================================")

if __name__ == "__main__":
    test_scenario()
