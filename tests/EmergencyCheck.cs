using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using JouchoPet;
class EmergencyCheck {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static int checks;
 static void Check(bool value,string text){if(!value)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
 static object Get(object o,string name){return o.GetType().GetField(name,F).GetValue(o);}
 static void Set(object o,string name,object value){o.GetType().GetField(name,F).SetValue(o,value);}
 static void Call(Pet p,string name,params object[] args){typeof(Pet).GetMethod(name,F).Invoke(p,args);}
 static HungApplication Target(){using(var self=Process.GetCurrentProcess())return new HungApplication{Id=self.Id+10000,Session=self.SessionId,Created=123456,Window=new IntPtr(123),Name="TestApp",Title="Test only",Hung=true};}
 sealed class Lease:IHungProcess {
  internal HungApplication Current;internal int Ends,Disposals;
  public HungApplication Inspect(){return Current;}
  public ProgramLaunchResult End(){Ends++;return new ProgramLaunchResult(true,"");}
  public void Dispose(){Disposals++;}
 }
 sealed class Apps:IHungApplications {
  internal int Calls,Reads;internal bool Fail,Throw,Empty;internal TaskCompletionSource<ProgramLaunchResult> Hold;
  public List<HungApplication> Read(){Reads++;if(Empty)return new List<HungApplication>();var a=Target();var b=Target();b.Id++;return new List<HungApplication>{a,a,b};}
  public Task<ProgramLaunchResult> EndAsync(HungApplication target){Calls++;if(Hold!=null)return Hold.Task;if(Throw)throw new Exception("test failure");return Task.FromResult(new ProgramLaunchResult(!Fail,Fail?"recovered":""));}
 }
 static void Drain(Pet pet){var watch=Stopwatch.StartNew();while((bool)Get(pet,"emergencyRunning")&&watch.ElapsedMilliseconds<5000){Application.DoEvents();System.Threading.Thread.Sleep(1);}Check(!(bool)Get(pet,"emergencyRunning"),"batch completed without blocking UI");}
 static void Policy(){
  var target=Target();var lease=new Lease{Current=Target()};var service=new HungApplications(t=>lease);
  Check(service.End(target).Success&&lease.Ends==1&&lease.Disposals==1,"same pinned process identity ends once and releases handle");
  foreach(var mutate in new Action<HungApplication>[] {a=>a.Hung=false,a=>a.Created++,a=>a.Id++,a=>a.Window=new IntPtr(321),a=>a.Session++,a=>a.Critical=true,a=>a.Name="explorer",a=>a.Name="ApplicationFrameHost",a=>{using(var self=Process.GetCurrentProcess())a.Id=self.Id;}}){
   lease.Current=Target();mutate(lease.Current);int before=lease.Ends;
   Check(!service.End(target).Success&&lease.Ends==before,"changed/recovered/protected target never terminated");
  }
  var denied=new HungApplications(t=>{throw new Win32Exception(5);});Check(!denied.End(target).Success&&denied.End(target).Error.Contains("权限不足"),"access denied reports failure without escalation");
  Check(!service.End(null).Success,"no target cannot terminate anything");
 }
 static void Elevation(){
  string path=Path.GetFullPath("IsekaijouchoPet.exe");int calls=0;
  var admin=new ShellFileOpener(true,info=>{calls++;Check(info.UseShellExecute&&info.Verb=="runas"&&info.FileName==path&&info.Arguments=="","administrator launch retains exact path, no command string");return null;});
  Check(admin.Open(path).Success&&calls==1,"runas accepts Shell handoff without returned process");
  var plain=new ShellFileOpener();Check(!plain.Administrator&&plain.StartInfo(path).Verb=="open","default opener remains ordinary open");
  var cancelled=new ShellFileOpener(true,info=>{calls++;throw new Win32Exception(1223);});int before=calls;
  var result=cancelled.Open(path);Check(!result.Success&&result.Error.Contains("取消")&&calls==before+1,"UAC cancellation reports once without ordinary-open fallback");
  Check(!admin.Open("relative.txt").Success&&calls==before+1,"invalid admin target never launches");
  foreach(int code in new[]{1155,50,193}){var verbs=new List<string>();var unsupported=new ShellFileOpener(true,info=>{verbs.Add(info.Verb);if(info.Verb=="runas")throw new Win32Exception(code);return null;});var fallback=unsupported.Open(path);Check(fallback.Success&&fallback.OrdinaryOpenFallback&&String.Join(",",verbs)=="runas,open","unsupported admin target opens normally once / "+code);}
  foreach(int code in new[]{1223,5,2,32}){int attempts=0;var failure=new ShellFileOpener(true,info=>{attempts++;throw new Win32Exception(code);});Check(!failure.Open(path).Success&&attempts==1,"cancel/access/missing/busy cannot trigger fallback / "+code);}
  string document=Path.GetFullPath("qa/v0810/readme.txt");File.WriteAllText(document,"test only");int ordinary=0;
  var textOpener=new ShellFileOpener(true,info=>{ordinary++;Check(info.Verb=="open"&&info.FileName==document,"document directly uses ordinary open");return null;});
  Check(textOpener.Open(document).OrdinaryOpenFallback&&ordinary==1,"no admin attempt for unsupported document");
 }
 static void Runtime(){using(var pet=new Pet())try{
  ((NotifyIcon)Get(pet,"tray")).Visible=false;Set(pet,"settings",Path.GetFullPath("qa/v0810/emergency-settings.txt"));Set(pet,"wandering",false);Set(pet,"sleeping",false);
  var beam=(ShellFileOpener)Get(Get(pet,"beamFileOpen"),"opener");var bolt=(ShellFileOpener)Get(Get(pet,"castFileOpen"),"opener");
  Check(beam.Administrator&&!bolt.Administrator,"real Pet wiring: beam elevated, bolt unchanged");
  var apps=new Apps();Set(pet,"hungApplications",apps);var daily=(DailyMotion)Get(pet,"daily");var motion=(Motion)Get(pet,"motion");
  Action reset=()=>{pet.CancelDailyAction();daily.Reset();motion.Reset();};
  foreach(var form in new[]{CharacterForm.AnemoneI,CharacterForm.AnemoneII}){
   reset();string error;Check(pet.TryChangeForm(form,out error),"form / "+form);
   int before=apps.Calls;Check(pet.MiSigil(),"one-click emergency request accepted / "+form);
   Call(pet,"UpdateDaily",.01);Call(pet,"UpdateDaily",.67);Check(apps.Calls==before,"no termination before expansion / "+form);
   Set(pet,"menuOpen",true);Call(pet,"UpdateDaily",.1);Set(pet,"menuOpen",false);Call(pet,"UpdateDaily",double.NaN);Check(apps.Calls==before,"pause and invalid time cannot terminate / "+form);
   int reads=apps.Reads;Call(pet,"UpdateDaily",.02);Call(pet,"UpdateDaily",1.0);Drain(pet);Check(apps.Calls==before+2&&apps.Reads==reads+1,"all unresponsive candidates processed once; duplicate excluded / "+form);before=apps.Calls;
   reset();pet.MiSigil();Call(pet,"UpdateDaily",.01);Call(pet,"UpdateDaily",.4);pet.CancelDailyAction();Call(pet,"UpdateDaily",1.0);Drain(pet);Check(apps.Calls==before,"cancel prevents termination / "+form);
   reset();pet.MiSigil();Call(pet,"UpdateDaily",.01);Set(pet,"sigilLayout","changed");Call(pet,"UpdateDaily",.9);Drain(pet);Check(apps.Calls==before,"display change prevents termination / "+form);
   reset();pet.MiSigil();Call(pet,"UpdateDaily",.01);pet.Left+=1;Call(pet,"UpdateDaily",.9);Drain(pet);Check(apps.Calls==before,"pet movement prevents termination / "+form);
   reset();pet.MiSigilPreview();Call(pet,"UpdateDaily",.01);Call(pet,"UpdateDaily",.9);Drain(pet);Check(apps.Calls==before,"MiSigilPreview cannot terminate / "+form);
   foreach(bool throws in new[]{false,true}){reset();apps.Fail=!throws;apps.Throw=throws;pet.MiSigil();Call(pet,"UpdateDaily",.01);Call(pet,"UpdateDaily",.9);Call(pet,"UpdateDaily",.4);Drain(pet);Check(apps.Calls==before+2,"one failed target cannot prevent other targets / "+form);before=apps.Calls;}
   apps.Fail=apps.Throw=false;
   reset();apps.Empty=true;pet.MiSigil();Call(pet,"UpdateDaily",.01);Call(pet,"UpdateDaily",.9);Drain(pet);Check(apps.Calls==before,"empty scan does not terminate anything / "+form);apps.Empty=false;
   reset();apps.Hold=new TaskCompletionSource<ProgramLaunchResult>();pet.MiSigil();Call(pet,"UpdateDaily",.01);Call(pet,"UpdateDaily",.9);
   var wait=Stopwatch.StartNew();while(apps.Calls==before&&wait.ElapsedMilliseconds<5000){Application.DoEvents();System.Threading.Thread.Sleep(1);}Check(apps.Calls==before+1&&!pet.MiSigil(),"batch in progress rejects repeated activation / "+form);
   pet.CancelDailyAction();apps.Hold.SetResult(new ProgramLaunchResult(true,""));Drain(pet);Check(apps.Calls==before+1,"cancel stops remaining targets in active batch / "+form);apps.Hold=null;
  }
 }finally{typeof(Form).GetMethod("OnFormClosed",F).Invoke(pet,new object[]{new FormClosedEventArgs(CloseReason.None)});}}
 [STAThread]static void Main(string[] args){try{
  if(args.Length>0&&args[0]=="native-read"){Console.WriteLine("PASS native unresponsive scan completed / candidates="+new HungApplications().Read().Count+"; no termination requested");return;}
  Directory.CreateDirectory("qa/v0808");Policy();Elevation();Runtime();Console.WriteLine("PASS "+checks+" emergency/elevation checks; fake termination and fake launch only.");
 }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
}
