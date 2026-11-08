using System;
using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Drawing;

public static class SilverPinesArabic {
 static readonly string Package=AppDomain.CurrentDomain.BaseDirectory;
 static readonly JavaScriptSerializer Json=new JavaScriptSerializer();
 static Dictionary<string,object> Manifest;
 static string LogPath;
 static string H(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
 static string HF(string p){using(var f=File.OpenRead(p))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
 static string S(Dictionary<string,object> d,string k){return (string)d[k];}
 static IEnumerable<Dictionary<string,object>> Records(string key){foreach(object x in (IEnumerable)Manifest[key])yield return (Dictionary<string,object>)x;}
 static string Inside(string root,string relative){string a=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;string p=Path.GetFullPath(Path.Combine(a,relative.Replace('/',Path.DirectorySeparatorChar)));if(!p.StartsWith(a,StringComparison.OrdinalIgnoreCase))throw new Exception("مسار ملف غير صالح في الحزمة.");return p.StartsWith(@"\\?\")?p:@"\\?\"+p;}
 static void Log(string s){string l=DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+" "+s; if(LogPath!=null)File.AppendAllText(LogPath,l+Environment.NewLine,new UTF8Encoding(false));}
 static byte[] ReadExactly(BinaryReader r,int n){byte[] b=r.ReadBytes(n);if(b.Length!=n)throw new Exception("ملف الباتش ناقص.");return b;}
 static byte[] Patch(byte[] old,byte[] patch){
  if(patch.Length<80)throw new Exception("ملف الباتش ناقص.");
  using(var stream=new MemoryStream(patch))using(var r=new BinaryReader(stream)){
   string magic=Encoding.ASCII.GetString(ReadExactly(r,8));bool raw=magic=="SPARUI02"||magic=="SPARUI04";bool additive=magic=="SPARUI03"||magic=="SPARUI04";if(magic!="SPARUI01"&&magic!="SPARUI02"&&magic!="SPARUI03"&&magic!="SPARUI04")throw new Exception("صيغة الباتش غير صالحة.");
   string expected=BitConverter.ToString(ReadExactly(r,32)).Replace("-","").ToLowerInvariant();
   string target=BitConverter.ToString(ReadExactly(r,32)).Replace("-","").ToLowerInvariant();
   ulong length=r.ReadUInt64();if(length>536870912 || H(old)!=expected)throw new Exception("بصمة الملف الأصلي غير مدعومة.");
   using(Stream gz=raw?(Stream)stream:new GZipStream(stream,CompressionMode.Decompress))using(var commands=new BinaryReader(gz))using(var output=new MemoryStream()){
    if(additive){
     uint count=commands.ReadUInt32();if(count>1048576)throw new Exception("عدد تعليمات الباتش غير صالح.");
     byte[] data=new byte[(int)length];int position=0;long oldPosition=0;
     for(uint c=0;c<count;c++){
      uint n=commands.ReadUInt32(),extra=commands.ReadUInt32();long shift=commands.ReadInt64();
      if((ulong)position+n+extra>length)throw new Exception("حدود بيانات الباتش غير صالحة.");
      byte[] differences=ReadExactly(commands,(int)n);
      for(int i=0;i<(int)n;i++){long index=checked(oldPosition+i);int previous=index>=0&&index<old.LongLength?old[(int)index]:0;data[position+i]=(byte)((previous+differences[i])&255);}
      position+=(int)n;byte[] literal=ReadExactly(commands,(int)extra);Buffer.BlockCopy(literal,0,data,position,(int)extra);position+=(int)extra;oldPosition=checked(oldPosition+n+shift);
     }
     if((ulong)position!=length||H(data)!=target)throw new Exception("فشل التحقق من الملف الناتج.");return data;
    }
    while(true){byte op=commands.ReadByte();if(op==255)break;
     if(op==0){ulong offset=commands.ReadUInt64();uint n=commands.ReadUInt32();if(offset>(ulong)old.Length || (ulong)n>(ulong)old.Length-offset || (ulong)output.Length+n>length)throw new Exception("تعليمة نسخ غير صالحة.");output.Write(old,(int)offset,(int)n);}
     else if(op==1){uint n=commands.ReadUInt32();if(n>536870912 || (ulong)output.Length+n>length)throw new Exception("تعليمة بيانات غير صالحة.");byte[] b=ReadExactly(commands,(int)n);output.Write(b,0,b.Length);}
     else throw new Exception("تعليمة باتش غير معروفة.");
    }
    byte[] result=output.ToArray();if((ulong)result.Length!=length || H(result)!=target)throw new Exception("فشل التحقق من الملف الناتج.");return result;
   }
  }
 }
 static bool Previous(Dictionary<string,object> d,string hash){if(!d.ContainsKey("previous_sha256"))return false;foreach(object x in (IEnumerable)d["previous_sha256"])if((string)x==hash)return true;return false;}
 static void Apply(string root,bool restore){
  root=Path.GetFullPath(root);if(!File.Exists(Path.Combine(root,"Carpenter.exe")))throw new Exception("اختر مجلد Silver Pines الذي يحتوي Carpenter.exe.");
  foreach(var process in Process.GetProcessesByName("Carpenter")){process.Dispose();throw new Exception("أغلق اللعبة أولا ثم أعد المحاولة.");}
  foreach(var g in Records("guards"))if(HF(Inside(root,S(g,"path")))!=S(g,"sha256"))throw new Exception("إصدار اللعبة غير مدعوم. الحزمة مخصصة لبناء Steam رقم "+S(Manifest,"steam_buildid")+".");
  string backups=Inside(root,"_SilverPinesArabic_UI");Directory.CreateDirectory(backups);LogPath=Path.Combine(backups,"install.log");Log(restore?"بدء استرجاع الملفات الأصلية.":"بدء تثبيت تحديث التعريب.");
  var plan=new List<Tuple<string,byte[],byte[]>>();var stages=new List<string>();var changed=new List<Tuple<string,byte[]>>();
  foreach(var file in Records("files")){
   string path=Inside(root,S(file,"path"));byte[] current=File.ReadAllBytes(path);string hash=H(current);string orig=S(file,"original_sha256"),patched=S(file,"patched_sha256");
   if(hash!=orig && hash!=patched && !Previous(file,hash))throw new Exception("ملف مختلف أو معدل غير معروف؛ لم تثبت الحزمة: "+Path.GetFileName(path));
   string backup=Inside(backups,orig+".bin");
   if(File.Exists(backup) && HF(backup)!=orig)throw new Exception("النسخة الاحتياطية غير سليمة؛ توقف التثبيت.");
   if((hash==patched || Previous(file,hash)) && !File.Exists(backup))throw new Exception("الملف معرّب لكن نسخته الأصلية الاحتياطية غير موجودة.");
   if(hash==orig && !File.Exists(backup))File.WriteAllBytes(backup,current);
   if(restore){if(hash==orig)continue;byte[] data=File.ReadAllBytes(backup);if(H(data)!=orig)throw new Exception("بصمة النسخة الاحتياطية غير سليمة.");plan.Add(Tuple.Create(path,current,data));}
   else {if(hash==patched)continue;string patch=Inside(Package,S(file,"patch"));byte[] delta=File.ReadAllBytes(patch);if(H(delta)!=S(file,"patch_sha256"))throw new Exception("ملف باتش غير سليم.");byte[] data=Patch(hash==orig?current:File.ReadAllBytes(backup),delta);if(H(data)!=patched)throw new Exception("بصمة الملف الناتج غير صحيحة.");plan.Add(Tuple.Create(path,current,data));}
  }
  try{
   foreach(var item in plan){string temp=item.Item1+".sparui-"+Guid.NewGuid().ToString("N")+".tmp";File.WriteAllBytes(temp,item.Item3);if(HF(temp)!=H(item.Item3))throw new Exception("فشل التحقق من الملف المؤقت.");stages.Add(temp);}
   for(int i=0;i<plan.Count;i++){File.Replace(stages[i],plan[i].Item1,null);changed.Add(Tuple.Create(plan[i].Item1,plan[i].Item2));Log("تم تحديث: "+Path.GetFileName(plan[i].Item1));}
   File.WriteAllText(Path.Combine(backups,"state.json"),Json.Serialize(new {version=S(Manifest,"version"),installed=!restore,steam_buildid=S(Manifest,"steam_buildid"),saved_games_touched=false}),new UTF8Encoding(false));
   Log(restore?"تم استرجاع الملفات الأصلية. لم تُلمس ملفات الحفظ.":"تم تثبيت نصوص الحزمة. اختر العربية في خانة الإنجليزية. اكتملت ترجمة الجداول المكتشفة؛ مراجعة العرض والصور ما زالت جارية.");
  }catch{
   foreach(var item in changed){string temp=item.Item1+".sparui-rollback-"+Guid.NewGuid().ToString("N")+".tmp";File.WriteAllBytes(temp,item.Item2);File.Replace(temp,item.Item1,null);}
   Log("تراجع المثبت عن التغييرات بعد خطأ.");throw;
  }finally{foreach(string temp in stages)if(File.Exists(temp))File.Delete(temp);}
 }

 static string FindGame(){
  var libraries=new List<string>();
  string nearby=Package;
  if(!String.IsNullOrEmpty(nearby) && File.Exists(Path.Combine(nearby,"Carpenter.exe")))return nearby;
  foreach(string key in new[]{@"HKEY_CURRENT_USER\Software\Valve\Steam",@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam",@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam"}){
   foreach(string name in new[]{"SteamPath","InstallPath"})try{object value=Microsoft.Win32.Registry.GetValue(key,name,null);if(value is string)libraries.Add((string)value);}catch{}
  }
  libraries.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Steam"));
  libraries.Add(@"D:\SteamLibrary");libraries.Add(@"E:\SteamLibrary");
  foreach(string steam in new List<string>(libraries))try{
   string vdf=Path.Combine(steam,@"steamapps\libraryfolders.vdf");
   if(File.Exists(vdf))foreach(System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(vdf),"\"path\"\\s*\"([^\"]+)\""))libraries.Add(match.Groups[1].Value.Replace(@"\\",@"\"));
  }catch{}
  foreach(string library in libraries)try{
   string folder=Path.Combine(library,@"steamapps\common\Silver Pines");if(File.Exists(Path.Combine(folder,"Carpenter.exe")))return folder;
  }catch{}
  return "";
 }


 static string InspectGame(string root){
  foreach(var guard in Records("guards"))if(HF(Inside(root,S(guard,"path")))!=S(guard,"sha256"))return "unknown";
  bool allOriginal=true,allPatched=true;
  foreach(var f in Records("files")){
   string hash=HF(Inside(root,S(f,"path")));string original=S(f,"original_sha256"),patched=S(f,"patched_sha256");
   if(hash!=original && hash!=patched && !Previous(f,hash))return "unknown";
   if(hash!=original){allOriginal=false;string backup=Inside(root,"_SilverPinesArabic_UI/"+original+".bin");if(!File.Exists(backup)||HF(backup)!=original)return "unknown";}
   if(hash!=patched)allPatched=false;
  }
  return allPatched?"installed":allOriginal?"original":"previous";
 }

 [STAThread] public static int Main(string[] args){
  AppContext.SetSwitch("Switch.System.IO.UseLegacyPathHandling",false);AppContext.SetSwitch("Switch.System.IO.BlockLongPaths",false);
  try{Manifest=Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(Package,"manifest.json"),Encoding.UTF8));}
  catch(Exception ex){MessageBox.Show(ex.Message,"حزمة غير مكتملة");return 1;}
  if(args.Length==2 && (args[0]=="--install" || args[0]=="--restore")){
   try{Apply(args[1],args[0]=="--restore");File.WriteAllText(Path.Combine(Package,"last-result.json"),Json.Serialize(new {success=true,action=args[0]}));return 0;}
   catch(Exception ex){if(LogPath!=null)Log("خطأ: "+ex.Message);File.WriteAllText(Path.Combine(Package,"last-result.json"),Json.Serialize(new {success=false,error=ex.Message}));return 1;}
  }
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  string initial=args.Length==2 && args[0]=="--ui"?Path.GetFullPath(args[1]):FindGame();
  Application.Run(new InstallerForm(initial,Apply,InspectGame));return 0;
 }
}
