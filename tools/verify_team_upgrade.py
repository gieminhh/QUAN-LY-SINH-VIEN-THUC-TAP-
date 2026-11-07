"""Verify a team's old schema can be upgraded twice without losing its records.
Uses only a disposable database; never imports into the configured source database.
"""
from pathlib import Path
import os,json,subprocess,shutil,uuid
root=Path(__file__).resolve().parents[1]
cs=os.environ.get('ConnectionStrings__DefaultConnection')
if not cs:cs=json.loads((root/'InternshipManagement.Web/appsettings.Development.json').read_text(encoding='utf-8-sig'))['ConnectionStrings']['DefaultConnection']
parts=dict(x.split('=',1) for x in cs.split(';') if '=' in x)
env=os.environ.copy();env['MYSQL_PWD']=parts['Pwd']
mysql=os.environ.get('MYSQL_EXE') or shutil.which('mysql') or r'C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe'
args=[mysql,'--protocol=TCP','--host='+parts['Server'],'--port='+parts.get('Port','3306'),'--user='+parts['Uid'],'--default-character-set=utf8mb4','--batch','--skip-column-names']
name='backlog_check_upgrade_'+uuid.uuid4().hex[:10]
assert name.startswith('backlog_check_upgrade_') and name!=parts['Database']
def run(q,selected=True):
 p=subprocess.run(args,input=(('USE `'+name+'`;\n' if selected else '')+q).encode(),capture_output=True,env=env,timeout=60)
 assert p.returncode==0,p.stderr.decode(errors='replace')
 return p.stdout.decode().strip()
created=False
try:
 run('CREATE DATABASE `'+name+'` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;',False);created=True
 run((root/'database/ql_thuctap_doan_demo.sql').read_text(encoding='utf-8-sig'))
 run((root/'database/legacy/upgrade_student_profiles_and_lecturer_quotas.sql').read_text(encoding='utf-8-sig'))
 run("""
 SET @campaign=(SELECT id FROM campaigns WHERE kind='PROJECT' LIMIT 1);
 SET @major=(SELECT major_id FROM campaigns WHERE id=@campaign);
 SET @student=(SELECT Id FROM AspNetUsers WHERE UserName='student');
 SET @lecturer=(SELECT Id FROM AspNetUsers WHERE UserName='giangvien_demo');
 UPDATE AspNetUsers SET major_id=@major,class_name='LegacyClass',skills='Legacy skills',interests='Legacy interests',cv_stored_file_name='legacytest.pdf' WHERE Id=@student;
 UPDATE AspNetUsers SET lecturer_specialty='Legacy specialty' WHERE Id=@lecturer;
 INSERT INTO lecturer_campaign_quotas(campaign_id,lecturer_user_id,capacity,updated_at) VALUES (@campaign,@lecturer,7,UTC_TIMESTAMP(6));
 INSERT INTO lecturer_campaign_assignments(campaign_id,lecturer_user_id,student_user_id,status,exception_reason,reviewed_by_user_id,reviewed_at,created_at) SELECT @campaign,@lecturer,@student,'APPROVED_EXCEPTION','Legacy approved reason',Id,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6) FROM AspNetUsers WHERE UserName='admin';
 """)
 baseline=run('SELECT COUNT(*) FROM AspNetUsers; SELECT COUNT(*) FROM AspNetUserRoles;')
 for repeat in range(2):
  for migration in sorted((root/'database/migrations').glob('*.sql')):run(migration.read_text(encoding='utf-8-sig'))
  assert run('SELECT COUNT(*) FROM AspNetUsers; SELECT COUNT(*) FROM AspNetUserRoles;')==baseline
  assert run("SELECT skills FROM student_profiles WHERE user_id=(SELECT Id FROM AspNetUsers WHERE UserName='student')")=='Legacy skills'
  assert run("SELECT expertise FROM lecturer_profiles WHERE user_id=(SELECT Id FROM AspNetUsers WHERE UserName='giangvien_demo')")=='Legacy specialty'
  assert run('SELECT max_students FROM lecturer_campaign_quotas')=='7'
  assert run('SELECT capacity FROM lecturer_campaign_quotas_legacy')=='7'
  assert run('SELECT COUNT(*) FROM student_assignments')=='1'
  assert run('SELECT QuotaOverrideReason FROM student_assignments')=='Legacy approved reason'
  assert run('SELECT storage_key FROM file_attachments')=='legacy_cvs/legacytest.pdf'
  assert run('SELECT COUNT(*) FROM file_attachments')=='1'
  assert run('SELECT COUNT(*) FROM student_classes')=='1'
 print('PASS: legacy profiles, faculty, quota, approved assignment, CV reference and account roles survive upgrade and repeat upgrade.')
finally:
 if created:run('DROP DATABASE `'+name+'`;',False)
 print('Temporary upgrade database removed; source database unchanged.')
