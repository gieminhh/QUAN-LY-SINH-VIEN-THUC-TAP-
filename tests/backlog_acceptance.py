"""Runs only against a disposable backlog_check_* MySQL database and isolated web app."""
import os,sys,re,json,subprocess,uuid,zipfile,io,html
from pathlib import Path
from datetime import datetime,timedelta,timezone
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from run_acceptance_tests_sprint2 import TestClient,encode_multipart_form
sys.stdout.reconfigure(encoding="utf-8")
assert os.environ["TEST_DATABASE"].startswith("backlog_check_")
mysql=[os.environ["MYSQL_EXE"],"--protocol=TCP","--host="+os.environ.get("MYSQL_HOST","localhost"),"--port="+os.environ.get("MYSQL_PORT","3306"),"--user="+os.environ.get("MYSQL_USER","root"),"--default-character-set=utf8mb4","--batch","--skip-column-names",os.environ["TEST_DATABASE"]]
def sql(query):
 p=subprocess.run(mysql,input=query.encode(),capture_output=True);assert p.returncode==0,p.stderr.decode(errors="replace");return p.stdout.decode().strip()
def token(body):
 x=TestClient().extract_csrf_token(body);assert x;return html.unescape(x)
def post(client,path,data):
 _,body,_=client.get(path if path in ["/Account/ChangePassword","/Account/ForgotPassword"] else "/");data["__RequestVerificationToken"]=token(body);return client.post(path,data)
def login(name,pwd):
 c=TestClient();status,body,url=c.login(name,pwd);assert status==200 and "/Account/Login" not in url,(name,url);return c
passed=[]
def ok(name):passed.append(name);print("PASS",name)
admin=login("admin","Admin@123");faculty=login("cb_cntt","CanBo@123");outsider=login("cb_kinhte",os.environ.get("CB_KINHTE_PASSWORD","CanBo@123"));student=login("student","Student@123")
uid=sql("SELECT Id FROM AspNetUsers WHERE UserName='admin'");sid=sql("SELECT id FROM student_profiles WHERE student_code='2011060001'");lid=sql("SELECT id FROM lecturer_profiles WHERE user_id=(SELECT Id FROM AspNetUsers WHERE UserName='giangvien')");major=sql("SELECT major_id FROM student_profiles WHERE id="+sid)
# Correct scope checks include profile and CV endpoints.
assert outsider.get('/StudentProfile?id='+sid)[0]==403
assert faculty.get('/StudentProfile?id='+sid)[0]==200
assert outsider.get('/LecturerProfile?id='+lid)[0]==403
attachment=sql('SELECT id FROM file_attachments WHERE student_profile_id='+sid+' LIMIT 1')
if attachment:assert outsider.get('/StudentProfile/DownloadCv?attachmentId='+attachment)[0]==403
ok('profile and CV faculty scope')
# Catalog create/update, duplicate and invalid semester.
code='YEAR_'+uuid.uuid4().hex[:5]
response=post(admin,'/Catalog/Save',dict(Kind='year',Code=code,StartDate='2026-01-01',EndDate='2026-12-31'))
assert code in response[1]
assert sql("SELECT COUNT(*) FROM academic_years WHERE code='"+code+"'")=='1'
post(admin,'/Catalog/Save',dict(Kind='year',Code=code,StartDate='2026-01-01',EndDate='2026-12-31'))
assert sql("SELECT COUNT(*) FROM academic_years WHERE code='"+code+"'")=='1'
assert student.get('/Catalog')[0]==403
ok('catalog administration, duplicate and role validation')
# Build a real minimal XLSX, including preview before writes.
name='import_'+uuid.uuid4().hex[:8];email=name+'@example.com'
def workbook(rows):
 stream=io.BytesIO()
 with zipfile.ZipFile(stream,'w') as z:
  z.writestr('xl/workbook.xml','<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Users" sheetId="1" r:id="rId1"/></sheets></workbook>')
  z.writestr('xl/_rels/workbook.xml.rels','<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Target="worksheets/sheet1.xml"/></Relationships>')
  xml='<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>'
  for i,row in enumerate(rows,1):
   xml+='<row r="'+str(i)+'">'+''.join('<c r="'+chr(65+j)+str(i)+'" t="inlineStr"><is><t>'+html.escape(value)+'</t></is></c>' for j,value in enumerate(row))+'</row>'
  z.writestr('xl/worksheets/sheet1.xml',xml+'</sheetData></worksheet>')
 return stream.getvalue()
_,form,_=admin.get('/UserImport');body,boundary=encode_multipart_form({'__RequestVerificationToken':token(form)},{'file':('users.xlsx',workbook([['UserName','Email','FullName','Role','InitialPassword'],[name,email,'Sinh viên kiểm thử','Student','Import@123']]),'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet')})
status,preview,_=admin.post('/UserImport/Preview',body,is_multipart=True,boundary=boundary)
assert status==200 and 'Hợp lệ; chưa tạo tài khoản.' in preview
assert sql("SELECT COUNT(*) FROM AspNetUsers WHERE UserName='"+name+"'")=='0'
ticket=html.unescape(re.search('name="ticket" value="([^"]+)"',preview).group(1))
status,body,_=post(admin,'/UserImport/Confirm',dict(ticket=ticket));assert 'Đã tạo tài khoản.' in body
assert sql("SELECT COUNT(*) FROM AspNetUsers WHERE UserName='"+name+"'")=='1'
post(admin,'/UserImport/Confirm',dict(ticket=ticket));assert sql("SELECT COUNT(*) FROM AspNetUsers WHERE UserName='"+name+"'")=='1'
assert student.get('/UserImport')[0]==403
ok('Excel preview, confirmation, replay protection and role restriction')
# Self-change and one-use reset token, routed to local development outbox.
account=login(name,'Import@123');status,body,_=post(account,'/Account/ChangePassword',dict(CurrentPassword='Wrong@123',NewPassword='Changed@123',ConfirmPassword='Changed@123'));assert 'Mật khẩu hiện tại' in body
post(account,'/Account/ChangePassword',dict(CurrentPassword='Import@123',NewPassword='Changed@123',ConfirmPassword='Changed@123'));login(name,'Changed@123')
sql("UPDATE AspNetUsers SET EmailConfirmed=1 WHERE UserName='"+name+"'")
_,forgot,_=TestClient().get('/Account/ForgotPassword');anon=TestClient();_,forgot,_=anon.get('/Account/ForgotPassword');anon.post('/Account/ForgotPassword',{'Email':email,'__RequestVerificationToken':token(forgot)})
files=sorted((Path(os.environ['TEST_CONTENT_ROOT'])/'App_Data/MailOutbox').glob('*.txt'),key=lambda p:p.stat().st_mtime)
assert files
from urllib.parse import urlparse,parse_qs
text=files[-1].read_text(encoding="utf-8");url=re.search(r'https?://[^\s]+',text).group(0);params=parse_qs(urlparse(url).query);reset=params['token'][0]
_,form,_=anon.get('/Account/ResetPassword?email='+email+'&token='+reset)
status,body,url=anon.post('/Account/ResetPassword',{'__RequestVerificationToken':token(form),'Email':email,'Token':reset,'NewPassword':'Reset@123','ConfirmPassword':'Reset@123'})
assert '/Account/Login' in url;login(name,'Reset@123')
_,form,_=anon.get('/Account/ResetPassword?email='+email+'&token='+reset)
_,body,_=anon.post('/Account/ResetPassword',{'__RequestVerificationToken':token(form),'Email':email,'Token':reset,'NewPassword':'Again@123','ConfirmPassword':'Again@123'});assert 'Liên kết không hợp lệ' in body
ok('password change, recovery delivery and one-use reset token')
# New test campaign linked to the student's own major.
now=datetime.now(timezone.utc).replace(tzinfo=None)+timedelta(hours=7);start=(now-timedelta(days=30)).strftime('%Y-%m-%d');end=(now+timedelta(days=30)).strftime('%Y-%m-%d')
cpcode='RULES_'+uuid.uuid4().hex[:6]
sql("INSERT INTO campaigns(code,name,term_id,major_id,kind,training_system,start_date,end_date,status,created_by_user_id,created_at,updated_at) VALUES ('"+cpcode+"','Đợt kiểm tra backlog',1,"+major+",'PROJECT','CHINH_QUY','"+start+"','"+end+"','OPEN','"+uid+"',UTC_TIMESTAMP(6),UTC_TIMESTAMP(6))")
cp=sql("SELECT id FROM campaigns WHERE code='"+cpcode+"'")
# Rules validate ordering and past/future windows; source data remains in test DB.
status,body,_=post(admin,'/CampaignRules/SaveWindow',dict(campaignId=cp,stage='REGISTRATION',opensAt=(now-timedelta(days=1)).isoformat(),closesAt=(now+timedelta(days=1)).isoformat()));assert 'Đã lưu lịch' in body
post(admin,'/CampaignRules/SaveWindow',dict(campaignId=cp,stage='MATCHING',opensAt=now.isoformat(),closesAt=(now+timedelta(days=2)).isoformat()));assert sql('SELECT COUNT(*) FROM campaign_windows WHERE CampaignId='+cp+" AND Stage='MATCHING'")=='0'
assert post(outsider,'/CampaignRules/SavePolicy',dict(campaignId=cp,minimumGpa4='2'))[0]==403
post(admin,'/CampaignRules/SavePolicy',dict(campaignId=cp,minimumGpa4='4',minimumCredits='100'))
_,body,_=admin.get('/CampaignRules?campaignId='+cp);assert 'Thiếu hoặc chưa đạt GPA thang 4' in body
post(admin,'/CampaignRules/SavePolicy',dict(campaignId=cp))
ok('campaign ordering, scoped policy and explicit GPA criteria')
# Deadline enforcement in topic creation plus an approved exception.
lecturer=login('giangvien','Lecturer@123');sql('UPDATE campaign_windows SET OpensAt=DATE_SUB(NOW(),INTERVAL 3 DAY),ClosesAt=DATE_SUB(NOW(),INTERVAL 2 DAY) WHERE CampaignId='+cp)
topiccode='LATE_'+uuid.uuid4().hex[:6]
_,form,_=lecturer.get('/Topics/Create');data={'__RequestVerificationToken':token(form),'CampaignId':cp,'Code':topiccode,'Title':'Đề tài kiểm tra thời hạn','MaxStudents':'1'}
_,body,url=lecturer.post('/Topics/Create',data);assert '/Topics/Details/' not in url and 'ngoài thời gian' in body
luser=sql('SELECT user_id FROM lecturer_profiles WHERE id='+lid)
post(admin,'/CampaignRules/GrantException',dict(campaignId=cp,userId=luser,stage='REGISTRATION',expiresAt=(now+timedelta(days=1)).isoformat(),reason='Đồng ý bổ sung đề tài kiểm thử'))
_,form,_=lecturer.get('/Topics/Create');data['__RequestVerificationToken']=token(form);_,body,url=lecturer.post('/Topics/Create',data);assert '/Topics/Details/' in url
ok('deadline enforced server-side with recorded approved exception')
# Set matching stage and ensure zero quota blocks, override is explicit, duplicate is blocked.
sql('DELETE FROM campaign_windows WHERE CampaignId='+cp)
post(admin,'/CampaignRules/SaveWindow',dict(campaignId=cp,stage='MATCHING',opensAt=(now-timedelta(days=1)).isoformat(),closesAt=(now+timedelta(days=1)).isoformat()))
_,body,_=post(admin,'/Assignments/Assign',dict(campaignId=cp,studentId=sid,lecturerId=lid));assert 'hết hạn mức' in body
assert sql('SELECT COUNT(*) FROM student_assignments WHERE CampaignId='+cp)=='0'
post(admin,'/Assignments/Assign',dict(campaignId=cp,studentId=sid,lecturerId=lid,quotaOverrideReason='Cán bộ phê duyệt thêm một sinh viên'))
assert sql('SELECT COUNT(*) FROM student_assignments WHERE CampaignId='+cp)=='1'
post(admin,'/Assignments/Assign',dict(campaignId=cp,studentId=sid,lecturerId=lid,quotaOverrideReason='Kiểm tra trùng'))
assert sql('SELECT COUNT(*) FROM student_assignments WHERE CampaignId='+cp)=='1'
_,body,_=post(admin,'/LecturerQuotas/SetQuota',dict(CampaignId=cp,LecturerProfileId=lid,MaxStudents=0));assert 'thấp hơn' in body
ok('zero quota, explicit approved overflow, duplicate assignment and quota reduction')
# A company owner submits registration; off-scope reviewers cannot approve.
company=login('backlog_company','Regression@123');code='DN_'+uuid.uuid4().hex[:8]
_,body,url=post(company,'/Companies/Register',dict(Name='Doanh nghiệp kiểm thử',RegistrationCode=code,MajorId=major,Email='contact@example.com',ContactName='Người hướng dẫn',Address='Địa chỉ kiểm thử'))
cid=sql("SELECT Id FROM companies WHERE RegistrationCode='"+code+"'");assert cid
assert post(outsider,'/Companies/Review',dict(id=cid,decision='APPROVED',reason='Không đúng phạm vi'))[0]==403
assert post(admin,'/Companies/Review',dict(id=cid,decision='APPROVED',reason=''))[0]==400
post(admin,'/Companies/Review',dict(id=cid,decision='APPROVED',reason='Hồ sơ hợp lệ'))
assert sql('SELECT Status FROM companies WHERE Id='+cid)=='APPROVED'
assert sql('SELECT COUNT(*) FROM company_mentors WHERE CompanyId='+cid)=='1'
_,body,_=company.get('/Companies/Students');assert 'Bạn chưa được phân công sinh viên' in body
assert company.get('/StudentProfile?id='+sid)[0]==403
mentor=sql('SELECT Id FROM company_mentors WHERE CompanyId='+cid+' LIMIT 1');sql('UPDATE student_assignments SET CompanyMentorId='+mentor+' WHERE CampaignId='+cp)
assert company.get('/StudentProfile?id='+sid)[0]==200
_,body,_=company.get('/Companies/Students');assert '2011060001' in body
ok('company registration, scoped review, required reason and assigned students only')
print(str(len(passed))+'/'+str(len(passed))+' backlog integration groups passed')
