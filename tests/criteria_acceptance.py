"""Extended issue-criteria tests. Disposable database only; all writes are test fixtures."""
from pathlib import Path
import os,sys,json,re,subprocess,uuid,base64,struct,hashlib,secrets,io,zipfile,html
from datetime import datetime,timedelta,timezone
from concurrent.futures import ThreadPoolExecutor
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from run_acceptance_tests_sprint2 import TestClient,encode_multipart_form
sys.stdout.reconfigure(encoding="utf-8",line_buffering=True)
assert os.environ['TEST_DATABASE'].startswith('backlog_check_')
mysql=[os.environ['MYSQL_EXE'],'--protocol=TCP','--host='+os.environ.get('MYSQL_HOST','localhost'),'--port='+os.environ.get('MYSQL_PORT','3306'),'--user='+os.environ.get('MYSQL_USER','root'),'--default-character-set=utf8mb4','--batch','--skip-column-names',os.environ['TEST_DATABASE']]
def sql(q):
 p=subprocess.run(mysql,input=q.encode(),capture_output=True,timeout=30);assert p.returncode==0,p.stderr.decode(errors='replace');return p.stdout.decode().strip()
def esc(s):return s.replace("'","''")
def token(body):
 t=TestClient().extract_csrf_token(body);assert t,'missing CSRF';return html.unescape(t)
def login(name,password):
 c=TestClient();status,body,url=c.login(name,password);assert status==200 and '/Account/Login' not in url,(name,url);return c
def post(c,path,data):
 _,body,_=c.get('/');return c.post(path,{**data,'__RequestVerificationToken':token(body)})
def user(role='Student'):
 name='criteria_'+uuid.uuid4().hex[:9];identity=uuid.uuid4().hex;salt=secrets.token_bytes(16);hashed=base64.b64encode(b'\x01'+struct.pack('>III',2,100000,16)+salt+hashlib.pbkdf2_hmac('sha512',b'Criteria@123',salt,100000,32)).decode()
 sql("INSERT INTO AspNetUsers(Id,UserName,NormalizedUserName,Email,NormalizedEmail,EmailConfirmed,PasswordHash,SecurityStamp,ConcurrencyStamp,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnabled,AccessFailedCount,FullName,IsActive,CreatedAt) VALUES ('"+identity+"','"+name+"','"+name.upper()+"','"+name+"@example.com','"+name.upper()+"@EXAMPLE.COM',1,'"+hashed+"','"+uuid.uuid4().hex+"','"+uuid.uuid4().hex+"',0,0,1,0,'Criteria test',1,UTC_TIMESTAMP(6))")
 sql("INSERT INTO AspNetUserRoles(UserId,RoleId) SELECT '"+identity+"',Id FROM AspNetRoles WHERE Name='"+role+"'")
 return identity,name,login(name,'Criteria@123')
admin=login('admin','Admin@123');faculty=login('cb_cntt','CanBo@123');student=login('student','Student@123')
adminid=sql("SELECT Id FROM AspNetUsers WHERE UserName='admin'");sid=sql("SELECT id FROM student_profiles WHERE student_code='2011060001'");major=sql('SELECT major_id FROM student_profiles WHERE id='+sid);facultyid=sql('SELECT faculty_id FROM majors WHERE id='+major);lid=sql("SELECT id FROM lecturer_profiles WHERE user_id=(SELECT Id FROM AspNetUsers WHERE UserName='giangvien')");luser=sql('SELECT user_id FROM lecturer_profiles WHERE id='+lid)
now=lambda:datetime.now(timezone.utc).replace(tzinfo=None)+timedelta(hours=7)
def campaign(stage='REGISTRATION'):
 code='CR_'+uuid.uuid4().hex[:9];start=(now()-timedelta(days=20)).strftime('%Y-%m-%d');end=(now()+timedelta(days=20)).strftime('%Y-%m-%d')
 sql("INSERT INTO campaigns(code,name,term_id,major_id,kind,training_system,start_date,end_date,status,created_by_user_id,created_at,updated_at) VALUES ('"+code+"','Criteria test',1,"+major+",'PROJECT','CHINH_QUY','"+start+"','"+end+"','OPEN','"+adminid+"',UTC_TIMESTAMP(6),UTC_TIMESTAMP(6))")
 cp=sql("SELECT id FROM campaigns WHERE code='"+code+"'")
 sql('INSERT INTO campaign_windows(CampaignId,Stage,OpensAt,ClosesAt) VALUES ('+cp+",'"+stage+"','"+(now()-timedelta(days=1)).strftime('%Y-%m-%d %H:%M:%S')+"','"+(now()+timedelta(days=1)).strftime('%Y-%m-%d %H:%M:%S')+"')")
 sql('INSERT INTO campaign_eligibility_policies(CampaignId) VALUES ('+cp+')');return cp

def auth_lifecycle():
 uid,name,c=user();sql("UPDATE AspNetUsers SET IsActive=0 WHERE Id='"+uid+"'")
 assert '/Account/Login' in c.get('/')[2],'existing session survives account deactivation'
 _,body,url=TestClient().login(name,'Criteria@123');assert '/Account/Login' in url and 'vô hiệu hóa' in body
 sql("UPDATE AspNetUsers SET IsActive=1 WHERE Id='"+uid+"'");c=login(name,'Criteria@123')
 for _ in range(5):TestClient().login(name,'Wrong@123')
 assert int(sql("SELECT LockoutEnd IS NOT NULL AND LockoutEnd>UTC_TIMESTAMP() FROM AspNetUsers WHERE Id='"+uid+"'"))==1
 assert '/Account/Login' in c.get('/')[2],'existing session survives lockout'
 _,body,url=TestClient().login(name,'Criteria@123');assert '/Account/Login' in url

def roles_and_csrf():
 uid,name,c=user('FacultyManager');assert c.get('/Catalog')[0]==403
 sql("INSERT INTO AspNetUserRoles(UserId,RoleId) SELECT '"+uid+"',Id FROM AspNetRoles WHERE Name='Admin'");assert c.get('/Catalog')[0]==200
 sql("DELETE FROM AspNetUserRoles WHERE UserId='"+uid+"' AND RoleId=(SELECT Id FROM AspNetRoles WHERE Name='Admin')");assert c.get('/Catalog')[0]==403,'old role cookie still grants admin'
 assert admin.post('/Catalog/Save',{'Kind':'year','Code':'NO_CSRF','StartDate':'2026-01-01','EndDate':'2026-12-31'})[0]==400
 sql("INSERT INTO AspNetUserRoles(UserId,RoleId) SELECT '"+uid+"',Id FROM AspNetRoles WHERE Name='Student'")
 sql("INSERT INTO user_role_scopes(UserId,RoleId,scope_type,granted_at,granted_by_user_id) SELECT '"+uid+"',Id,'GLOBAL',UTC_TIMESTAMP(6),'"+adminid+"' FROM AspNetRoles WHERE Name='Student'")
 _,body,_=c.get('/Campaigns');assert 'Danh sách đợt (0 đợt)' in body,'student scope escalates management access'

def password_validation():
 uid,name,c=user();data={'CurrentPassword':'Criteria@123','NewPassword':'New@123','ConfirmPassword':'Mismatch@123'}
 post(c,'/Account/ChangePassword',data);login(name,'Criteria@123')
 post(c,'/Account/ChangePassword',dict(CurrentPassword='Criteria@123',NewPassword='x',ConfirmPassword='x'));login(name,'Criteria@123')
 other=login(name,'Criteria@123');post(c,'/Account/ChangePassword',dict(CurrentPassword='Criteria@123',NewPassword='New@123',ConfirmPassword='New@123'))
 assert '/Account/Login' in other.get('/')[2],'old password session not invalidated'
 login(name,'New@123')

def recovery_invalid_and_unknown():
 c=TestClient();_,form,_=c.get('/Account/ForgotPassword');_,known,_=c.post('/Account/ForgotPassword',{'Email':'admin@vinhuni.edu.vn','__RequestVerificationToken':token(form)})
 _,form,_=c.get('/Account/ForgotPassword');_,unknown,_=c.post('/Account/ForgotPassword',{'Email':uuid.uuid4().hex+'@example.com','__RequestVerificationToken':token(form)})
 assert 'Nếu email khớp' in known and 'Nếu email khớp' in unknown
 _,form,_=c.get('/Account/ResetPassword?email=admin@vinhuni.edu.vn&token=invalid')
 _,body,url=c.post('/Account/ResetPassword',{'Email':'admin@vinhuni.edu.vn','Token':'invalid','NewPassword':'Bad@123','ConfirmPassword':'Bad@123','__RequestVerificationToken':token(form)})
 assert 'Liên kết không hợp lệ' in body;login('admin','Admin@123')

def academic_tamper_and_file_limits():
 before=sql('SELECT student_code FROM student_profiles WHERE id='+sid);gpa=sql("SELECT CONCAT(gpa_4,':',gpa_10) FROM student_gpa_records WHERE student_profile_id="+sid+' ORDER BY id')
 _,form,_=student.get('/StudentProfile/Edit');data={'__RequestVerificationToken':token(form),'Skills':'Updated only in temp DB','Interests':'Criteria','StudentCode':'FORGED','LatestGpa4':'4','LatestGpa10':'10'}
 student.post('/StudentProfile/Edit',data)
 assert sql('SELECT student_code FROM student_profiles WHERE id='+sid)==before
 assert sql("SELECT CONCAT(gpa_4,':',gpa_10) FROM student_gpa_records WHERE student_profile_id="+sid+' ORDER BY id')==gpa
 cp=campaign();lecturer=login('giangvien','Lecturer@123')
 for filename,payload in [('wrong.exe',b'file'),('large.pdf',b'x'*(10*1024*1024+1)),('empty.pdf',b'')]:
  code='FILE_'+uuid.uuid4().hex[:8];_,form,_=lecturer.get('/Topics/Create')
  fields={'__RequestVerificationToken':token(form),'CampaignId':cp,'Code':code,'Title':'Invalid file test','MaxStudents':'1'}
  body,boundary=encode_multipart_form(fields,{'DocumentFile':(filename,payload,'application/octet-stream')})
  status,body,url=lecturer.post('/Topics/Create',body,is_multipart=True,boundary=boundary)
  assert status<500 and '/Topics/Details/' not in url
  assert sql("SELECT COUNT(*) FROM topics WHERE code='"+code+"'")=='0'

def cv_type_size_and_atomicity():
 original=sql('SELECT skills FROM student_profiles WHERE id='+sid)
 attachment_before=sql('SELECT id FROM file_attachments WHERE student_profile_id='+sid+' ORDER BY id')
 for filename,payload in [('wrong.exe',b'file'),('archive.zip',b'zip'),('large.pdf',b'x'*(10*1024*1024+1)),('empty.pdf',b'')]:
  _,form,_=student.get('/StudentProfile/Edit')
  body,boundary=encode_multipart_form({'__RequestVerificationToken':token(form),'Skills':'Must not persist invalid CV','Interests':'Invalid CV regression'},{'CvFile':(filename,payload,'application/octet-stream')})
  status,body,url=student.post('/StudentProfile/Edit',body,is_multipart=True,boundary=boundary)
  assert status==200 and 'CV phải là PDF' in body,(filename,status)
  assert sql('SELECT skills FROM student_profiles WHERE id='+sid)==original,'invalid upload partly saves profile'
  assert sql('SELECT id FROM file_attachments WHERE student_profile_id='+sid+' ORDER BY id')==attachment_before,'invalid upload removes existing CV'


def invalid_excel():
 for filename,payload in [('bad.csv',b'not-xlsx'),('broken.xlsx',b'not a zip')]:
  _,form,_=admin.get('/UserImport');body,boundary=encode_multipart_form({'__RequestVerificationToken':token(form)},{'file':(filename,payload,'application/octet-stream')})
  status,body,_=admin.post('/UserImport/Preview',body,is_multipart=True,boundary=boundary)
  assert status==200 and 'name="ticket"' not in body
 assert post(admin,'/UserImport/Confirm',{'ticket':'modified-ticket'})[0]==400

def catalog_dates_and_references():
 code='DATES_'+uuid.uuid4().hex[:6]
 post(admin,'/Catalog/Save',dict(Kind='year',Code=code,StartDate='2027-12-31',EndDate='2027-01-01'))
 assert sql("SELECT COUNT(*) FROM academic_years WHERE code='"+code+"'")=='0'
 code='YEAR_'+uuid.uuid4().hex[:6]
 post(admin,'/Catalog/Save',dict(Kind='year',Code=code,StartDate='2026-01-01',EndDate='2026-12-31'))
 year=sql("SELECT id FROM academic_years WHERE code='"+code+"'")
 for termno,start,end in [(4,'2026-01-01','2026-05-01'),(1,'2025-01-01','2026-05-01')]:
  post(admin,'/Catalog/Save',dict(Kind='term',ParentId=year,TermNo=termno,StartDate=start,EndDate=end))
 assert sql('SELECT COUNT(*) FROM terms WHERE academic_year_id='+year)=='0'
 post(admin,'/Catalog/Save',dict(Kind='term',ParentId=year,TermNo=1,StartDate='2026-01-01',EndDate='2026-05-01'))
 post(admin,'/Catalog/Delete',dict(kind='year',id=year));assert sql('SELECT COUNT(*) FROM academic_years WHERE id='+year)=='1'
 post(admin,'/Catalog/Save',dict(Kind='year',Id=year,Code=code,StartDate='2026-06-01',EndDate='2026-12-31'))
 assert sql("SELECT DATE_FORMAT(start_date,'%Y-%m-%d') FROM academic_years WHERE id="+year)=='2026-01-01','year update invalidates child term dates'

def malformed_policy_and_evidence():
 cp=campaign();post(admin,'/CampaignRules/SavePolicy',dict(campaignId=cp,minimumGpa4=3))
 post(admin,'/CampaignRules/SavePolicy',dict(campaignId=cp,minimumGpa4='not-a-number'))
 assert sql('SELECT MinimumGpa4 FROM campaign_eligibility_policies WHERE CampaignId='+cp)=='3.00','invalid input silently removes GPA threshold'
 status,_,_=post(admin,'/CampaignRules/SaveEvidence',dict(campaignId=cp,studentId=sid,credits='bad',sourceNote='Test invalid credits'))
 assert status<500 and sql('SELECT COUNT(*) FROM student_eligibility_evidence WHERE CampaignId='+cp)=='0','invalid credits saved as zero'

def eligibility_filters():
 cp=campaign();post(admin,'/CampaignRules/SavePolicy',dict(campaignId=cp,minimumCredits=100,prerequisiteNote='Môn tiên quyết'))
 _,body,_=admin.get('/CampaignRules?campaignId='+cp+'&eligibility=eligible');assert '2011060001' not in body
 post(admin,'/CampaignRules/SaveEvidence',dict(campaignId=cp,studentId=sid,credits=110,prerequisitesMet='false',sourceNote='Danh sách học vụ kiểm thử'))
 _,body,_=admin.get('/CampaignRules?campaignId='+cp);assert 'Chưa xác nhận điều kiện tiên quyết' in body
 post(admin,'/CampaignRules/SaveEvidence',dict(campaignId=cp,studentId=sid,credits=110,prerequisitesMet='true',sourceNote='Danh sách học vụ kiểm thử'))
 _,body,_=admin.get('/CampaignRules?campaignId='+cp+'&eligibility=eligible');assert '2011060001' in body
 _,body,_=admin.get('/CampaignRules?campaignId='+cp+'&eligibility=ineligible');assert '2011060001' not in body
 assert sql('SELECT RecordedBy FROM student_eligibility_evidence WHERE CampaignId='+cp)==adminid

def deadlines_and_revocation():
 cp=campaign();lecturer=login('giangvien','Lecturer@123');code='WIN_'+uuid.uuid4().hex[:8]
 _,form,_=lecturer.get('/Topics/Create');status,body,url=lecturer.post('/Topics/Create',{'__RequestVerificationToken':token(form),'CampaignId':cp,'Code':code,'Title':'Deadline test','MaxStudents':1})
 assert '/Topics/Details/' in url;tid=url.rstrip('/').split('/')[-1]
 sql('UPDATE campaign_windows SET OpensAt=DATE_SUB(NOW(),INTERVAL 3 DAY),ClosesAt=DATE_SUB(NOW(),INTERVAL 2 DAY) WHERE CampaignId='+cp)
 post(lecturer,'/Topics/Submit/'+tid,{})
 assert sql('SELECT status FROM topics WHERE id='+tid)=='DRAFT'
 _,form,_=lecturer.get('/Topics/Edit/'+tid);lecturer.post('/Topics/Edit/'+tid,{'__RequestVerificationToken':token(form),'CampaignId':cp,'Code':code,'Title':'Forbidden late edit','MaxStudents':1})
 assert sql('SELECT title FROM topics WHERE id='+tid)=='Deadline test'
 post(admin,'/CampaignRules/GrantException',dict(campaignId=cp,userId=luser,stage='REGISTRATION',expiresAt=(now()+timedelta(days=1)).isoformat(),reason='Chấp thuận kiểm thử'))
 ex=sql('SELECT Id FROM campaign_exceptions WHERE CampaignId='+cp)
 post(admin,'/CampaignRules/RevokeException',dict(id=ex));post(lecturer,'/Topics/Submit/'+tid,{})
 assert sql('SELECT status FROM topics WHERE id='+tid)=='DRAFT'
 post(admin,'/CampaignRules/GrantException',dict(campaignId=cp,userId=luser,stage='REGISTRATION',expiresAt=(now()+timedelta(days=1)).isoformat(),reason='Chấp thuận kiểm thử'))
 sql("UPDATE campaigns SET status='CLOSED' WHERE id="+cp);post(lecturer,'/Topics/Submit/'+tid,{})
 assert sql('SELECT status FROM topics WHERE id='+tid)=='DRAFT','exception opens a closed campaign'

def approval_transitions():
 cp=campaign();lecturer=login('giangvien','Lecturer@123');code='REJECT_'+uuid.uuid4().hex[:6]
 _,form,_=lecturer.get('/Topics/Create');_,_,url=lecturer.post('/Topics/Create',{'__RequestVerificationToken':token(form),'CampaignId':cp,'Code':code,'Title':'Approval test','MaxStudents':1,'SubmitImmediately':'true'})
 assert '/Topics/Details/' in url;tid=url.rstrip('/').split('/')[-1]
 post(faculty,'/TopicApproval/Reject/'+tid,{'comments':''});assert sql('SELECT status FROM topics WHERE id='+tid)=='PENDING'
 post(faculty,'/TopicApproval/Reject/'+tid,{'comments':'Không phù hợp yêu cầu'});assert sql('SELECT status FROM topics WHERE id='+tid)=='REJECTED'
 assert sql('SELECT COUNT(*) FROM topic_approval_history WHERE topic_id='+tid+' AND actor_user_id IS NOT NULL AND action_at IS NOT NULL')=='2'
 post(faculty,'/TopicApproval/Approve/'+tid,{});assert sql('SELECT status FROM topics WHERE id='+tid)=='REJECTED'
 assert student.get('/Topics/Details/'+tid)[0]==403
 _,form,_=lecturer.get('/Topics/Create');_,_,url=lecturer.post('/Topics/Create',{'__RequestVerificationToken':token(form),'CampaignId':cp,'Code':code+'A','Title':'Approve test','MaxStudents':1,'SubmitImmediately':'true'})
 tid=url.rstrip('/').split('/')[-1]
 second_reviewer=login('admin','Admin@123')
 with ThreadPoolExecutor(2) as pool:
  responses=list(pool.map(lambda reviewer: post(reviewer,'/TopicApproval/Approve/'+tid,{'comments':'Đủ điều kiện'}),[faculty,second_reviewer]))
 assert all(response[0]<500 for response in responses),'concurrent approval returns a server error'
 assert sql('SELECT status FROM topics WHERE id='+tid)=='APPROVED'
 assert sql("SELECT COUNT(*) FROM topic_approval_history WHERE topic_id="+tid+" AND to_status='APPROVED'")=='1','concurrent approval records more than one decision'
 assert student.get('/Topics/Details/'+tid)[0]==200
 post(lecturer,'/Topics/Edit/'+tid,dict(CampaignId=cp,Code=code+'A',Title='Tampered approved title',MaxStudents=1))
 assert sql('SELECT title FROM topics WHERE id='+tid)=='Approve test'

def student_fixture():
 uid,name,c=user();code='SV_'+uuid.uuid4().hex[:8]
 sql("INSERT INTO student_profiles(user_id,student_code,major_id,created_at,updated_at) VALUES ('"+uid+"','"+code+"',"+major+",UTC_TIMESTAMP(6),UTC_TIMESTAMP(6))")
 return sql("SELECT id FROM student_profiles WHERE user_id='"+uid+"'")
def quota_race():
 cp=campaign('MATCHING');s1=student_fixture();s2=student_fixture()
 post(admin,'/LecturerQuotas/SetQuota',dict(CampaignId=cp,LecturerProfileId=lid,MaxStudents=1))
 c1=login('admin','Admin@123');c2=login('admin','Admin@123')
 with ThreadPoolExecutor(2) as pool:
  jobs=[pool.submit(post,c,'/Assignments/Assign',dict(campaignId=cp,studentId=s,lecturerId=lid)) for c,s in [(c1,s1),(c2,s2)]]
  responses=[f.result(timeout=45) for f in jobs]
 assert sql('SELECT COUNT(*) FROM student_assignments WHERE CampaignId='+cp)=='1','concurrent assignments exceed quota'
 assert all(r[0]<500 for r in responses),'concurrent request returns 500 instead of recoverable message'

def company_registration_integrity():
 uid,name,company=user('Company');cp=campaign();code='DN_'+uuid.uuid4().hex[:8]
 data=dict(Name='Test company',RegistrationCode=code,MajorId=major,Email='company@example.com',ContactName='Test mentor',Address='Test address',Status='APPROVED',OwnerUserId=adminid)
 post(company,'/Companies/Register',data);cid=sql("SELECT Id FROM companies WHERE RegistrationCode='"+code+"'")
 assert sql('SELECT OwnerUserId FROM companies WHERE Id='+cid)==uid
 assert sql('SELECT Status FROM companies WHERE Id='+cid)=='PENDING'
 post(company,'/Companies/Register',data);assert sql("SELECT COUNT(*) FROM companies WHERE RegistrationCode='"+code+"'")=='1'
 post(admin,'/Companies/Review',dict(id=cid,decision='REJECTED',reason='Chưa đủ hồ sơ'));assert sql('SELECT Status FROM companies WHERE Id='+cid)=='REJECTED'
 assert post(company,'/Companies/AddMentor',dict(companyId=cid,email='student@vinhuni.edu.vn',name='Invalid mentor'))[0]==403

def csrf_roles_and_erd():
 for route in ['/UserImport','/Catalog','/Assignments','/CampaignRules','/Companies']:
  assert student.get(route)[0]==403,(route,'student can access manager UI')
 for route in ['/StudentProfile/Edit','/LecturerProfile/Edit','/TopicApproval/Approve/1','/Assignments/Assign']:
  assert admin.post(route,{})[0] in (400,403),'mutation accepts missing CSRF '+route
 doc=(Path(__file__).resolve().parents[1]/'docs/ERD.md').read_text(encoding='utf-8').split('## Thiết kế tổng thể')[0]
 actual={x.lower() for x in sql('SHOW TABLES').splitlines()}
 refs=set(re.findall(r'^\s*(\w+)\s+[|o}{]+--[|o}{]+\s+(\w+)',doc,re.M))
 assert refs
 for left,right in refs:assert left.lower() in actual and right.lower() in actual,(left,right)
 assert len(actual)==29

cases=[('#8 CV type/size/empty-file rejection preserves current profile and attachment',cv_type_size_and_atomicity),('#4 Account deactivation and lockout revoke sessions',auth_lifecycle),('#5 Multiple roles, revocation and Student-scope isolation',roles_and_csrf),('#4 Password mismatch, policy and old-session invalidation',password_validation),('#4 Invalid reset and generic unknown-email response',recovery_invalid_and_unknown),('#8 Academic tampering and #14 invalid/oversized/empty files',academic_tamper_and_file_limits),('#7 Invalid workbook and tampered import ticket',invalid_excel),('#11 Catalog dates and referenced-record protection',catalog_dates_and_references),('#12 Malformed policy/evidence must not silently change data',malformed_policy_and_evidence),('#12 Credits, prerequisites and eligible/ineligible filters',eligibility_filters),('#12/#14 Late submit/edit, revoked exception and closed campaign',deadlines_and_revocation),('#15 Reject reason, history and immutable approved/rejected state',approval_transitions),('#9 Concurrent assignments enforce one remaining quota',quota_race),('#10 Registration status/owner spoof, duplicate and rejected company',company_registration_integrity),('#2/#5 Role/CSRF boundaries and implemented ERD tables',csrf_roles_and_erd)]
results=[]
for name,fn in cases:
 try:fn();results.append(dict(name=name,passed=True));print('PASS',name)
 except Exception as e:results.append(dict(name=name,passed=False,error=str(e)));print('FAIL',name,repr(e))
report=Path(os.environ['TEST_CONTENT_ROOT']).parent/'criteria-results.json';report.write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
print(str(sum(x['passed'] for x in results))+'/'+str(len(results))+' extended criteria groups passed')
sys.exit(0 if all(x['passed'] for x in results) else 1)
