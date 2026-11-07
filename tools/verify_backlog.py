"""Build and test against a temporary clone of the local MySQL database.
Does not mutate the source database or source uploads. No Git operations.
"""
from pathlib import Path
import json,os,subprocess,uuid,time,urllib.request,shutil,sys,hashlib,secrets,base64,struct
from datetime import datetime,timedelta,timezone
sys.stdout.reconfigure(encoding="utf-8")
root=Path(__file__).resolve().parents[1]
config=root/"InternshipManagement.Web/appsettings.Development.json"
cs=os.environ.get("ConnectionStrings__DefaultConnection")
if not cs:cs=json.loads(config.read_text(encoding="utf-8-sig"))["ConnectionStrings"]["DefaultConnection"]
parts=dict(x.split("=",1) for x in cs.split(";") if "=" in x)
env=os.environ.copy();env["MYSQL_PWD"]=parts["Pwd"]
mysql=os.environ.get("MYSQL_EXE") or shutil.which("mysql") or r"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"
dump_exe=str(Path(mysql).with_name("mysqldump.exe" if os.name=="nt" else "mysqldump"))
args=["--protocol=TCP","--host="+parts["Server"],"--port="+parts.get("Port","3306"),"--user="+parts["Uid"],"--default-character-set=utf8mb4"]
def sql(query):
 result=subprocess.run([mysql,*args],input=query.encode(),capture_output=True,env=env)
 if result.returncode:raise RuntimeError(result.stderr.decode(errors="replace"))
name="backlog_check_"+uuid.uuid4().hex[:10]
work=Path(os.environ.get("TEMP","/tmp"))/name;work.mkdir();proc=None;created=False
try:
 build=work/"build"
 subprocess.run(["dotnet","build",str(root/"InternshipManagement.Web"),"--no-restore","-o",str(build),"-p:UseAppHost=false"],check=True)
 dump=subprocess.run([dump_exe,*args,"--single-transaction","--no-tablespaces","--set-gtid-purged=OFF",parts["Database"]],capture_output=True,env=env,check=True).stdout
 if "--clean-seed" in sys.argv:dump=(root/"database/ql_thuctap_doan_demo.sql").read_bytes()
 sql("CREATE DATABASE `"+name+"`;");created=True
 sql("USE `"+name+"`;\n"+dump.decode())
 for migration in sorted((root/"database/migrations").glob("*.sql")):sql("USE `"+name+"`;\n"+migration.read_text(encoding="utf-8-sig"))
 for uname,role in [("backlog_other_lecturer","role_lecturer"),("backlog_company","role_company")]:
  salt=secrets.token_bytes(16);key=hashlib.pbkdf2_hmac("sha512",b"Regression@123",salt,100000,32);hashed=base64.b64encode(b"\x01"+struct.pack(">III",2,100000,16)+salt+key).decode();identity=uuid.uuid4().hex
  sql("USE `"+name+"`; INSERT INTO AspNetUsers(Id,UserName,NormalizedUserName,Email,NormalizedEmail,EmailConfirmed,PasswordHash,SecurityStamp,ConcurrencyStamp,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnabled,AccessFailedCount,FullName,IsActive,CreatedAt) VALUES ('"+identity+"','"+uname+"','"+uname.upper()+"','"+uname+"@example.com','"+uname.upper()+"@EXAMPLE.COM',1,'"+hashed+"','"+uuid.uuid4().hex+"','"+uuid.uuid4().hex+"',0,0,1,0,'Regression account',1,UTC_TIMESTAMP(6)); INSERT INTO AspNetUserRoles(UserId,RoleId) VALUES ('"+identity+"','"+role+"');")
 now=datetime.now(timezone.utc).replace(tzinfo=None)+timedelta(hours=7);start=(now-timedelta(days=1)).strftime("%Y-%m-%d %H:%M:%S");end=(now+timedelta(days=1)).strftime("%Y-%m-%d %H:%M:%S")
 sql("USE `"+name+"`; INSERT INTO campaign_windows(CampaignId,Stage,OpensAt,ClosesAt) SELECT id,'REGISTRATION','"+start+"','"+end+"' FROM campaigns WHERE status='OPEN' ON DUPLICATE KEY UPDATE OpensAt=VALUES(OpensAt),ClosesAt=VALUES(ClosesAt);")
 content=work/"content";content.mkdir()
 shutil.copytree(root/"InternshipManagement.Web/wwwroot",content/"wwwroot")
 if (root/"InternshipManagement.Web/App_Data").exists():shutil.copytree(root/"InternshipManagement.Web/App_Data",content/"App_Data",dirs_exist_ok=True)
 appenv={k:v for k,v in os.environ.items() if not k.lower().startswith(("mail__","mail:"))};appenv["Mail__Host"]="";appenv["ConnectionStrings__DefaultConnection"]=cs.replace("Database="+parts["Database"],"Database="+name);appenv["ASPNETCORE_ENVIRONMENT"]="Development";appenv["Mail:BaseUrl"]="http://localhost:5065"
 log=open(work/"web.log","wb")
 proc=subprocess.Popen(["dotnet",str(build/"InternshipManagement.Web.dll"),"--urls","http://localhost:5065"],cwd=content,env=appenv,stdout=log,stderr=log,creationflags=subprocess.CREATE_NO_WINDOW if os.name=="nt" else 0)
 for i in range(120):
  try:urllib.request.urlopen("http://localhost:5065/Account/Login",timeout=1);break
  except OSError:
   if proc.poll()!=None:raise RuntimeError("Test web exited. See "+str(work/"web.log"))
   time.sleep(.25)
 sql("USE `"+name+"`; INSERT INTO campaign_windows(CampaignId,Stage,OpensAt,ClosesAt) SELECT id,'REGISTRATION','"+start+"','"+end+"' FROM campaigns WHERE status='OPEN' ON DUPLICATE KEY UPDATE OpensAt=VALUES(OpensAt),ClosesAt=VALUES(ClosesAt);")
 testenv=env.copy();testenv.update(TEST_BASE_URL="http://localhost:5065",TEST_DATABASE=name,TEST_CONTENT_ROOT=str(content),MYSQL_EXE=mysql,MYSQL_HOST=parts["Server"],MYSQL_PORT=parts.get("Port","3306"),MYSQL_USER=parts["Uid"])
 if "--clean-seed" in sys.argv:testenv["CB_KINHTE_PASSWORD"]="KinhTe@123"
 for script in (["tests/criteria_acceptance.py"] if "--criteria-only" in sys.argv else ["tests/backlog_acceptance.py"] if "--backlog-only" in sys.argv else ["run_acceptance_tests_sprint2.py","tests/backlog_acceptance.py","tests/criteria_acceptance.py"]):
  result=subprocess.run([sys.executable,str(root/script)],capture_output=True,env=testenv)
  print(result.stdout.decode("utf-8",errors="replace"));print(result.stderr.decode("utf-8",errors="replace"))
  if result.returncode:raise RuntimeError(script+" failed; test logs at "+str(work))
 print("All checks passed. Original database and uploads were not changed.")
finally:
 if proc:
  proc.terminate();proc.wait(timeout=20);log.close()
 if created:sql("DROP DATABASE `"+name+"`;")
 print("Temporary test database removed.")
