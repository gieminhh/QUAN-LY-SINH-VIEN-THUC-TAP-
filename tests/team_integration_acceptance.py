"""Regression checks for the merged team features; disposable database only."""
from pathlib import Path
import os,sys,re,html,uuid,subprocess,io,zipfile
from datetime import datetime,timedelta,timezone
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from run_acceptance_tests_sprint2 import TestClient,encode_multipart_form
sys.stdout.reconfigure(encoding='utf-8',line_buffering=True)
assert os.environ['TEST_DATABASE'].startswith('backlog_check_')
mysql=[os.environ['MYSQL_EXE'],'--protocol=TCP','--host='+os.environ.get('MYSQL_HOST','localhost'),'--port='+os.environ.get('MYSQL_PORT','3306'),'--user='+os.environ.get('MYSQL_USER','root'),'--default-character-set=utf8mb4','--batch','--skip-column-names',os.environ['TEST_DATABASE']]
def sql(q):
 p=subprocess.run(mysql,input=q.encode(),capture_output=True,timeout=30);assert p.returncode==0,p.stderr.decode(errors='replace');return p.stdout.decode().strip()
def token(body):
 t=TestClient().extract_csrf_token(body);assert t,'missing CSRF';return html.unescape(t)
def login(name,pw):
 c=TestClient();status,body,url=c.login(name,pw);assert status==200 and '/Account/Login' not in url,(name,status,url);return c
def post(c,path,data):
 _,body,_=c.get('/');return c.post(path,{**data,'__RequestVerificationToken':token(body)})
def upload(c,path,field,name,data,fields=None):
 _,body,_=c.get('/');payload,boundary=encode_multipart_form({'__RequestVerificationToken':token(body),**(fields or {})},{field:(name,data,'application/octet-stream')});return c.post(path,payload,is_multipart=True,boundary=boundary)
def workbook(rows):
 stream=io.BytesIO()
 with zipfile.ZipFile(stream,'w') as z:
  z.writestr('xl/workbook.xml','<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Users" sheetId="1" r:id="rId1"/></sheets></workbook>')
  z.writestr('xl/_rels/workbook.xml.rels','<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Target="worksheets/sheet1.xml"/></Relationships>')
  content='<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>'
  for i,row in enumerate(rows,1):content+='<row r="'+str(i)+'">'+''.join('<c r="'+chr(65+j)+str(i)+'" t="inlineStr"><is><t>'+html.escape(value)+'</t></is></c>' for j,value in enumerate(row))+'</row>'
  z.writestr('xl/worksheets/sheet1.xml',content+'</sheetData></worksheet>')
 return stream.getvalue()
def ok(message):print('PASS: '+message)
admin=login('admin','Admin@123');student=login('student','Student@123');lecturer=login('giangvien','Lecturer@123');faculty=login('cb_kinhte',os.environ.get('CB_KINHTE_PASSWORD','CanBo@123'))
sid=sql("SELECT id FROM student_profiles WHERE student_code='2011060001'");suser=sql('SELECT user_id FROM student_profiles WHERE id='+sid);major=sql('SELECT major_id FROM student_profiles WHERE id='+sid)
lid=sql("SELECT id FROM lecturer_profiles WHERE user_id=(SELECT Id FROM AspNetUsers WHERE UserName='giangvien')");luser=sql('SELECT user_id FROM lecturer_profiles WHERE id='+lid);adminid=sql("SELECT Id FROM AspNetUsers WHERE UserName='admin'")

for route in ['/Faculties','/Majors','/AcademicYears','/Terms','/Users/ImportExcel','/Profiles/Manage']:
 assert admin.get(route)[0]==200,route
 assert student.get(route)[0]==403,route
for route in ['/Profiles','/StudentProfile/Edit']:assert student.get(route)[0]==200,route
assert lecturer.get('/Profiles')[0]==200
ok('Team catalog, import, profile and quota screens render with role restrictions')

# Both profile routes write the same normalized records; academic data is immutable.
before=sql('SELECT CONCAT(major_id,":",COALESCE(class_id,0)) FROM student_profiles WHERE id='+sid)
post(student,'/Profiles',dict(Skills='Team shared skills',Interests='Team interests',MajorId='999999',ClassName='Spoofed class'))
assert sql('SELECT skills FROM student_profiles WHERE id='+sid)=='Team shared skills'
assert sql('SELECT CONCAT(major_id,":",COALESCE(class_id,0)) FROM student_profiles WHERE id='+sid)==before
assert 'Team shared skills' in student.get('/StudentProfile/Edit')[1]
post(student,'/StudentProfile/Edit',dict(Skills='Canonical shared skills',Interests='Canonical interests'))
assert 'Canonical shared skills' in student.get('/Profiles')[1]
post(lecturer,'/Profiles',dict(LecturerSpecialty='Shared lecturer expertise'))
assert sql('SELECT expertise FROM lecturer_profiles WHERE id='+lid)=='Shared lecturer expertise'
assert 'Shared lecturer expertise' in lecturer.get('/LecturerProfile')[1]
ok('Profile routes share student skills and lecturer expertise; academic tampering ignored')

count=sql('SELECT COUNT(*) FROM file_attachments WHERE student_profile_id='+sid+" AND file_category='STUDENT_CV'")
upload(student,'/Profiles','CvFile','fake.pdf',b'not a PDF',dict(Skills='Must not persist'))
assert sql('SELECT skills FROM student_profiles WHERE id='+sid)=='Canonical shared skills'
assert sql('SELECT COUNT(*) FROM file_attachments WHERE student_profile_id='+sid+" AND file_category='STUDENT_CV'")==count
upload(student,'/Profiles','CvFile','team.pdf',b'%PDF-1.4\n%%EOF',dict(Skills='CV saved'))
aid=sql('SELECT id FROM file_attachments WHERE student_profile_id='+sid+" AND file_category='STUDENT_CV' ORDER BY id DESC LIMIT 1")
assert aid and student.get('/Profiles/DownloadCv/'+suser)[0]==200
assert student.get('/StudentProfile/DownloadCv?attachmentId='+aid)[0]==200
assert faculty.get('/Profiles/DownloadCv/'+suser)[0]==403
post(student,'/Profiles/RemoveCv',{})
assert sql('SELECT COUNT(*) FROM file_attachments WHERE student_profile_id='+sid+" AND file_category='STUDENT_CV'")=='0'
assert student.get('/Profiles/DownloadCv/'+suser)[0]==404
ok('Legacy PDF signature validation, unified downloads, scope isolation and CV removal')

# The teammate's workbook format uses PhoneNumber and generates a password.
name='team_'+uuid.uuid4().hex[:10]
rows=[['UserName','Email','FullName','PhoneNumber','Role'],[name,name+'@example.com','Team imported','0900123456','Student']]
status,preview,_=upload(admin,'/Users/PreviewExcel','ExcelFile','team.xlsx',workbook(rows));assert status==200
ticket=html.unescape(re.search(r'name="ticket" value="([^"]+)"',preview).group(1))
assert sql("SELECT COUNT(*) FROM AspNetUsers WHERE UserName='"+name+"'")=='0'
assert post(admin,'/Users/ConfirmImport',dict(ticket=ticket+'tampered'))[0]==400
status,result,_=post(admin,'/Users/ConfirmImport',dict(ticket=ticket));assert status==200 and 'Đã tạo tài khoản.' in result
password=html.unescape(re.search(r'<code>([^<]+)</code>',result).group(1));login(name,password)
assert sql("SELECT PhoneNumber FROM AspNetUsers WHERE UserName='"+name+"'")=='0900123456'
post(admin,'/Users/ConfirmImport',dict(ticket=ticket));assert sql("SELECT COUNT(*) FROM AspNetUsers WHERE UserName='"+name+"'")=='1'
assert post(admin,'/Users/ConfirmImport',{'Rows[0].UserName':'bypass','Rows[0].Role':'Admin'})[0]==400
bad=[rows[0],[name,'invalid','Bad','','UnknownRole']]
assert 'name="ticket"' not in upload(admin,'/Users/PreviewExcel','ExcelFile','bad.xlsx',workbook(bad))[1]
ok('Team Excel preview/confirm, generated-password login, phone, tamper and replay checks')

# Date validation is enforced through the team's separate catalog edit screen too.
code='Y'+uuid.uuid4().hex[:8]
post(admin,'/AcademicYears/Create',dict(Code=code,StartDate='2030-01-01',EndDate='2030-12-31'))
yid=sql("SELECT id FROM academic_years WHERE code='"+code+"'");assert yid
post(admin,'/Terms/Create',dict(AcademicYearId=yid,TermNo=1,StartDate='2030-02-01',EndDate='2030-06-30'))
tid=sql('SELECT id FROM terms WHERE academic_year_id='+yid);assert tid
post(admin,'/AcademicYears/Edit',dict(Id=yid,Code=code,StartDate='2030-03-01',EndDate='2030-12-31'))
assert sql('SELECT start_date FROM academic_years WHERE id='+yid)=='2030-01-01'
post(admin,'/Terms/Edit',dict(Id=tid,AcademicYearId=yid,TermNo=1,StartDate='2029-01-01',EndDate='2030-06-30'))
assert sql('SELECT start_date FROM terms WHERE id='+tid)=='2030-02-01'
assert admin.get('/AcademicYears?page=0')[0]==200
ok('Team year and term forms protect nested dates and pagination')

now=datetime.now(timezone.utc).replace(tzinfo=None)+timedelta(hours=7)
code='TEAM_'+uuid.uuid4().hex[:8]
sql("INSERT INTO campaigns(code,name,term_id,major_id,kind,training_system,start_date,end_date,status,created_by_user_id,created_at,updated_at) VALUES ('"+code+"','Team quota test',1,"+major+",'PROJECT','CHINH_QUY','"+(now-timedelta(days=20)).strftime('%Y-%m-%d')+"','"+(now+timedelta(days=20)).strftime('%Y-%m-%d')+"','OPEN','"+adminid+"',UTC_TIMESTAMP(6),UTC_TIMESTAMP(6))")
cp=sql("SELECT id FROM campaigns WHERE code='"+code+"'")
sql("INSERT INTO campaign_windows(CampaignId,Stage,OpensAt,ClosesAt) VALUES ("+cp+",'MATCHING','"+(now-timedelta(days=1)).strftime('%Y-%m-%d %H:%M:%S')+"','"+(now+timedelta(days=1)).strftime('%Y-%m-%d %H:%M:%S')+"')")
sql('INSERT INTO campaign_eligibility_policies(CampaignId) VALUES ('+cp+')')
newuid=sql("SELECT Id FROM AspNetUsers WHERE UserName='"+name+"'")
sql("INSERT INTO student_profiles(user_id,student_code,major_id) VALUES ('"+newuid+"','"+name+"',"+major+")")
newsid=sql("SELECT id FROM student_profiles WHERE user_id='"+newuid+"'")
post(admin,'/Profiles/SetQuota',dict(CampaignId=cp,LecturerUserId=luser,Capacity=1))
assert sql('SELECT max_students FROM lecturer_campaign_quotas WHERE campaign_id='+cp+' AND lecturer_profile_id='+lid)=='1'
post(admin,'/Assignments/Assign',dict(campaignId=cp,studentId=sid,lecturerId=lid))
assert sql('SELECT COUNT(*) FROM student_assignments WHERE CampaignId='+cp)=='1'
post(admin,'/Profiles/AddAssignment',dict(CampaignId=cp,LecturerUserId=luser,StudentUserId=newuid))
assert sql('SELECT COUNT(*) FROM student_assignments WHERE CampaignId='+cp)=='1'
post(admin,'/Profiles/AddAssignment',dict(CampaignId=cp,LecturerUserId=luser,StudentUserId=newuid,ExceptionReason='Team exception review'))
rid=sql("SELECT id FROM lecturer_campaign_assignments WHERE campaign_id="+cp+" AND student_user_id='"+newuid+"' AND status='PENDING_EXCEPTION'");assert rid
assert sql('SELECT COUNT(*) FROM student_assignments WHERE CampaignId='+cp)=='1'
assert post(faculty,'/Profiles/ReviewException',dict(id=rid,approve='true'))[0]==403
post(admin,'/Assignments/Assign',dict(campaignId=cp,studentId=newsid,lecturerId=lid,quotaOverrideReason='Bypass pending'))
assert sql('SELECT COUNT(*) FROM student_assignments WHERE CampaignId='+cp)=='1'
sql("UPDATE campaigns SET status='CLOSED' WHERE id="+cp)
post(admin,'/Profiles/ReviewException',dict(id=rid,approve='true'))
assert sql("SELECT status FROM lecturer_campaign_assignments WHERE id="+rid)=='PENDING_EXCEPTION'
sql("UPDATE campaigns SET status='OPEN' WHERE id="+cp)
post(admin,'/Profiles/ReviewException',dict(id=rid,approve='true'))
assert sql('SELECT COUNT(*) FROM student_assignments WHERE CampaignId='+cp)=='2'
assert sql("SELECT status FROM lecturer_campaign_assignments WHERE id="+rid)=='APPROVED_EXCEPTION'
assert sql('SELECT AssignedBy FROM student_assignments WHERE CampaignId='+cp+' AND StudentProfileId='+newsid)==adminid
post(admin,'/Profiles/ReviewException',dict(id=rid,approve='true'))
assert sql('SELECT COUNT(*) FROM student_assignments WHERE CampaignId='+cp)=='2'
post(admin,'/Profiles/SetQuota',dict(CampaignId=cp,LecturerUserId=luser,Capacity=0))
assert sql('SELECT max_students FROM lecturer_campaign_quotas WHERE campaign_id='+cp+' AND lecturer_profile_id='+lid)=='1'
assert 'Team exception review' in admin.get('/Profiles/Manage?campaignId='+cp)[1]
ok('Both assignment screens share counts; pending exception, approval, scope, deadline, replay and quota floor')
print('6 team integration groups passed.')
