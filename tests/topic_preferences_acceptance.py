"""Exercise issue #18 on a dedicated MySQL demo database and loopback web instance."""

import concurrent.futures
import http.cookiejar
import os
import re
import shutil
import subprocess
import urllib.error
import urllib.parse
import urllib.request
import uuid


class Client:
    def __init__(self, base):
        self.base = base
        self.http = urllib.request.build_opener(
            urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar())
        )

    def request(self, path, fields=None):
        data = None if fields is None else urllib.parse.urlencode(fields, doseq=True).encode()
        try:
            with self.http.open(self.base + path, data, timeout=30) as response:
                return response.status, response.read().decode(), response.geturl()
        except urllib.error.HTTPError as error:
            return error.code, error.read().decode(), error.geturl()

    def login(self, username, password):
        status, page, _ = self.request("/Account/Login")
        assert status == 200
        status, _, location = self.request("/Account/Login", {
            "__RequestVerificationToken": input_value(page, "__RequestVerificationToken"),
            "UserName": username, "Password": password,
        })
        assert status == 200 and "/Account/Login" not in location, location


def input_value(page, name):
    for tag in re.findall(r"<input\b[^>]*>", page):
        if f'name="{name}"' in tag:
            found = re.search(r'\bvalue="([^"]*)"', tag)
            return found[1] if found else ""
    raise AssertionError(f"Missing input {name}")


def main():
    base = os.environ.get("ISSUE18_BASE_URL", "http://127.0.0.1:5068").rstrip("/")
    if urllib.parse.urlsplit(base).hostname != "127.0.0.1" or os.environ.get("ISSUE18_ALLOW_FIXTURES") != "1":
        raise SystemExit("Use a dedicated local demo server. Set ISSUE18_ALLOW_FIXTURES=1 to write test fixtures.")
    if not os.environ.get("MYSQL_PWD"):
        raise SystemExit("Set MYSQL_PWD for the isolated MySQL instance.")
    mysql = os.environ.get("MYSQL_EXE") or shutil.which("mysql")
    if not mysql:
        raise SystemExit("Set MYSQL_EXE to the local MySQL client.")
    command = [mysql, "--no-defaults", "--protocol=TCP", "--host=127.0.0.1", "--port=33318",
               "--user=root", "--database=issue18_demo", "--default-character-set=utf8mb4", "--batch", "--skip-column-names"]

    def sql(query):
        result = subprocess.run(command, input=query, capture_output=True, text=True, encoding="utf-8")
        if result.returncode:
            raise RuntimeError(result.stderr.strip())
        return result.stdout.strip()

    student = Client(base)
    student.login("student", "Student@123")
    admin = Client(base)
    admin.login("admin", "Admin@123")
    suffix = uuid.uuid4().hex[:8]
    campaign_code = "NV18_" + suffix
    foreign_code = "NV18_OTHER_" + suffix
    sql(f"""
        START TRANSACTION;
        SET @major = (SELECT s.major_id FROM student_profiles s JOIN AspNetUsers u ON u.Id=s.user_id WHERE u.UserName='student');
        SET @other = (SELECT id FROM majors WHERE id<>@major ORDER BY id LIMIT 1);
        SET @term = (SELECT id FROM terms ORDER BY id LIMIT 1);
        SET @admin = (SELECT Id FROM AspNetUsers WHERE UserName='admin');
        SET @lecturer = (SELECT id FROM lecturer_profiles ORDER BY id LIMIT 1);
        SET @now = UTC_TIMESTAMP(6) + INTERVAL 7 HOUR;
        INSERT INTO campaigns(code,name,term_id,major_id,kind,training_system,start_date,end_date,status,created_by_user_id)
        VALUES ('{campaign_code}','Đồ án CNTT - Đăng ký nguyện vọng',@term,@major,'PROJECT','CHINH_QUY',DATE(@now),DATE(@now)+INTERVAL 7 DAY,'OPEN',@admin),
               ('{foreign_code}','Đợt kiểm thử ngành khác',@term,@other,'PROJECT','CHINH_QUY',DATE(@now),DATE(@now)+INTERVAL 7 DAY,'OPEN',@admin);
        SET @campaign = (SELECT id FROM campaigns WHERE code='{campaign_code}');
        SET @outside = (SELECT id FROM campaigns WHERE code='{foreign_code}');
        INSERT INTO campaign_windows(CampaignId,Stage,OpensAt,ClosesAt)
        VALUES (@campaign,'REGISTRATION',@now-INTERVAL 1 HOUR,@now+INTERVAL 1 DAY),
               (@outside,'REGISTRATION',@now-INTERVAL 1 HOUR,@now+INTERVAL 1 DAY);
        INSERT INTO topics(campaign_id,lecturer_profile_id,code,title,research_field,status)
        VALUES (@campaign,@lecturer,'NV-A','Cổng hỗ trợ sinh viên','Phát triển web','APPROVED'),
               (@campaign,@lecturer,'NV-B','Theo dõi tiến độ đồ án','Hệ thống thông tin','APPROVED'),
               (@campaign,@lecturer,'NV-C','Tổng hợp phản hồi thực tập','Phân tích dữ liệu','APPROVED'),
               (@campaign,@lecturer,'NV-DRAFT','Đề tài nháp kiểm thử','Phần mềm','DRAFT'),
               (@outside,@lecturer,'NV-OTHER','Đề tài ngoài ngành','Phần mềm','APPROVED');
        COMMIT;
    """)
    campaign, outside = (
        int(sql(f"SELECT id FROM campaigns WHERE code='{code}'")) for code in (campaign_code, foreign_code)
    )
    a, b, c, draft = [
        int(sql(f"SELECT id FROM topics WHERE campaign_id={campaign} AND code='{code}'"))
        for code in ("NV-A", "NV-B", "NV-C", "NV-DRAFT")
    ]
    foreign = int(sql(f"SELECT id FROM topics WHERE campaign_id={outside}"))
    page_url = "/TopicPreferences?campaignId=" + str(campaign)

    def form(client):
        status, page, _ = client.request(page_url)
        assert status == 200 and "Lưu nguyện vọng" in page
        return {
            "__RequestVerificationToken": input_value(page, "__RequestVerificationToken"),
            "Input.CampaignId": str(campaign), "Input.Version": input_value(page, "Input.Version"),
            "Input.ResearchDirection": "Phát triển web",
        }, page

    def save(client, fields, choices):
        return client.request("/TopicPreferences/Save", {**fields, "Input.TopicIds": choices})

    def persisted():
        return sql(f"SELECT PreferenceRank,TopicId FROM student_topic_preferences WHERE CampaignId={campaign} ORDER BY PreferenceRank")

    fields, page = form(student)
    assert "NV-A" in page and "NV-B" in page and "NV-DRAFT" not in page and "NV-OTHER" not in page
    assert student.request(f"/TopicPreferences?campaignId={outside}")[0] == 404
    assert "/Account/AccessDenied" in admin.request(page_url)[2]
    assert student.request("/TopicPreferences/Save", {"Input.CampaignId": str(campaign)})[0] == 400
    _, invalid, _ = save(student, fields, [a, a])
    assert "Mỗi đề tài chỉ được chọn một lần" in invalid and not persisted()
    for bad_id in (draft, foreign, 0):
        _, invalid, _ = save(student, fields, [a, bad_id])
        assert "Chỉ chọn đề tài đã được phê duyệt" in invalid and not persisted()
    print("PASS student access, campaign scope, CSRF, duplicate/draft/foreign topic rejection")

    status, _, location = save(student, fields, [b, "", a, c])
    assert status == 200 and "TopicPreferences?campaignId=" in location
    assert persisted() == f"1\t{b}\n2\t{a}\n3\t{c}"
    assert sql(f"SELECT ResearchDirection, SavedAt IS NOT NULL FROM student_topic_preference_lists WHERE CampaignId={campaign}") == "Phát triển web\t1"
    current, page = form(student)
    assert "Lưu lần cuối" in page and current["Input.Version"] != fields["Input.Version"]
    _, saved_page, _ = save(student, current, [c, b])
    assert "Đã lưu nguyện vọng" in saved_page and persisted() == f"1\t{c}\n2\t{b}"
    _, stale, _ = save(student, current, [a])
    assert "Vui lòng tải lại trang" in stale and persisted() == f"1\t{c}\n2\t{b}"
    print("PASS direction, timestamp, compact ranks, reorder/removal, stale form")

    other_tab = Client(base)
    other_tab.login("student", "Student@123")
    one, _ = form(student)
    two, _ = form(other_tab)
    assert one["Input.Version"] == two["Input.Version"]
    with concurrent.futures.ThreadPoolExecutor(2) as pool:
        first = pool.submit(save, student, one, [a, b])
        second = pool.submit(save, other_tab, two, [b, c])
        results = [first.result()[1], second.result()[1]]
    assert sum("Đã lưu nguyện vọng" in body for body in results) == 1
    assert sum("Vui lòng tải lại trang" in body for body in results) == 1
    assert persisted() in (f"1\t{a}\n2\t{b}", f"1\t{b}\n2\t{c}")
    print("PASS concurrent saves preserve one complete ordered list")

    current, _ = form(student)
    _, empty, _ = save(student, current, [""])
    assert "Đã lưu nguyện vọng" in empty and not persisted()
    current, _ = form(student)
    save(student, current, [a, b, c])
    fields, _ = form(student)
    sql(f"UPDATE campaign_windows SET ClosesAt=UTC_TIMESTAMP(6)+INTERVAL 7 HOUR-INTERVAL 1 SECOND WHERE CampaignId={campaign}")
    _, locked, _ = save(student, fields, [c, a])
    assert "Nguyện vọng đã được khóa" in locked and "Lưu nguyện vọng</button>" not in locked
    assert persisted() == f"1\t{a}\n2\t{b}\n3\t{c}"
    print("PASS empty list, deadline lock in UI and direct POST, preserved saved ordering")

    sql(f"UPDATE campaign_windows SET ClosesAt=UTC_TIMESTAMP(6)+INTERVAL 7 HOUR+INTERVAL 1 DAY WHERE CampaignId={campaign}")
    print("Demo", base + page_url)


if __name__ == "__main__":
    main()
