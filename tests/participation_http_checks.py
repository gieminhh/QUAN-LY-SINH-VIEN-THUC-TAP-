"""HTTP checks against the explicitly authorized isolated issue2021_demo instance."""
import html
import http.cookiejar
import os
import re
import shutil
import subprocess
import time
import urllib.error
import urllib.parse
import urllib.request

if os.environ.get('PARTICIPATION_ALLOW_FIXTURES') != '1' or not os.environ.get('MYSQL_PWD'):
    raise SystemExit('Set PARTICIPATION_ALLOW_FIXTURES=1 and MYSQL_PWD for the isolated MySQL instance.')
mysql = os.environ.get('MYSQL_EXE') or shutil.which('mysql')
if not mysql:
    raise SystemExit('Set MYSQL_EXE to the MySQL client.')
base = 'http://127.0.0.1:5070'

def sql(query):
    return subprocess.check_output([mysql, '--no-defaults', '-h127.0.0.1', '-P33320', '-uroot',
        '-N', '--default-character-set=utf8mb4', 'issue2021_demo', '-e', query], text=True).strip()

class Client:
    def __init__(self):
        self.http = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
    def request(self, path, fields=None):
        data = None if fields is None else urllib.parse.urlencode(fields, doseq=True).encode()
        try:
            with self.http.open(base + path, data, timeout=30) as response:
                return response.status, html.unescape(response.read().decode()), response.geturl()
        except urllib.error.HTTPError as error:
            return error.code, html.unescape(error.read().decode()), error.geturl()
    def login(self, username):
        _, page, _ = self.request('/Account/Login')
        password = 'Admin@123' if username == 'admin' else 'Test20@123'
        status, _, url = self.request('/Account/Login', {'__RequestVerificationToken': token(page), 'UserName': username, 'Password': password})
        assert status == 200 and '/Account/Login' not in url, url

def field(page, name):
    for tag in re.findall(r'<input\b[^>]*>', page):
        if f'name="{name}"' in tag:
            value = re.search(r'\bvalue="([^"]*)"', tag)
            return value[1] if value else ''
    raise AssertionError('Missing field ' + name)

def token(page):
    return field(page, '__RequestVerificationToken')

for attempt in range(60):
    try:
        if Client().request('/Account/Login')[0] == 200:
            break
    except urllib.error.URLError:
        pass
    time.sleep(0.5)
else:
    raise AssertionError('Isolated web server did not become ready.')
cp = sql("SELECT id FROM campaigns WHERE code='TEST20_PROJECT'")
ip = sql("SELECT id FROM campaigns WHERE code='TEST20_INTERNSHIP'")
topic = sql("SELECT id FROM topics WHERE title='TEST21_TOPIC'")
student, lecturer, other_lecturer, admin = Client(), Client(), Client(), Client()
for client, name in [(student, 'test20-s1'), (lecturer, 'test20-l1'), (other_lecturer, 'test20-l2'), (admin, 'admin')]:
    client.login(name)
status, page, _ = student.request('/ProjectGroups?campaignId=' + cp)
assert status == 200 and 'Đăng ký cá nhân' in page
assert student.request('/ProjectGroups/Create', {'campaignId': cp, 'name': 'No CSRF'})[0] == 400
assert '/Account/AccessDenied' in student.request('/ParticipationPolicies?campaignId=' + cp)[2]
assert '/Account/AccessDenied' in student.request('/LecturerPriorities?topicId=' + topic)[2]
assert '/Account/AccessDenied' in other_lecturer.request('/LecturerPriorities?topicId=' + topic)[2]
print('PASS HTTP group page, role authorization and CSRF')
status, page, _ = lecturer.request('/LecturerPriorities?topicId=' + topic)
assert status == 200 and 'Tiêu chí / lý do ưu tiên' in page and 'test20-s1' in page and 'test20-s2' in page
assert 'test20-other' not in page and 'test20-s3' not in page
version = field(page, 'Input.Version')
status, invalid, _ = lecturer.request('/LecturerPriorities/Save', {'__RequestVerificationToken': token(page), 'Input.TopicId': topic,
    'Input.Version': version, 'Input.Reason': 'HTTP invalid candidate', 'Input.StudentIds': ['test20-other']})
assert status == 200 and 'Chỉ xếp hạng sinh viên hợp lệ' in invalid
status, saved, _ = lecturer.request('/LecturerPriorities/Save', {'__RequestVerificationToken': token(page), 'Input.TopicId': topic,
    'Input.Version': version, 'Input.Reason': 'HTTP manual order', 'Input.StudentIds': ['test20-s2', 'test20-s1']})
assert status == 200 and 'Đã lưu thứ tự ưu tiên' in saved
print('PASS HTTP lecturer page, candidate scope and priority save')
status, page, _ = admin.request('/ParticipationPolicies?campaignId=' + cp)
assert status == 200 and 'Mở đăng ký' in page
assert admin.request('/ParticipationPolicies/Save', {'Input.CampaignId': cp})[0] == 400
policy_before = sql('SELECT GroupsAllowed FROM campaign_participation_policies WHERE CampaignId=' + ip)
status, invalid, _ = admin.request('/ParticipationPolicies/Save', {'__RequestVerificationToken': token(page), 'Input.CampaignId': ip,
    'Input.GroupsAllowed': 'true', 'Input.MaxGroupSize': '3',
    'Input.RegistrationOpensAt': field(page, 'Input.RegistrationOpensAt'),
    'Input.RegistrationClosesAt': field(page, 'Input.RegistrationClosesAt')})
assert status == 200 and 'Thực tập chỉ đăng ký cá nhân' in invalid
assert policy_before == sql('SELECT GroupsAllowed FROM campaign_participation_policies WHERE CampaignId=' + ip)
print('PASS HTTP policy page, CSRF and internship group prohibition')
print('All HTTP checks passed.')
