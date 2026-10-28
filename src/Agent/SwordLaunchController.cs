using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
namespace JouchoPet {
 public sealed class ProgramLaunchResult:EventArgs {
  public bool Success {get;private set;}
  public string Error {get;private set;}
  internal bool OrdinaryOpenFallback {get;set;}
  public ProgramLaunchResult(bool success,string error){Success=success;Error=error??"";}
 }
 public interface IProgramLauncher {ProgramLaunchResult Launch(string executablePath);}
 // Explicit application launch tool. No command string, file move/delete or
 // provider-specific API. Merely constructing it never starts an application.
 public sealed class ProgramLaunchController:IProgramLauncher {
  public ProgramLaunchResult Launch(string executablePath){
   try{
    if(string.IsNullOrWhiteSpace(executablePath)||!Path.IsPathRooted(executablePath))return new ProgramLaunchResult(false,"An absolute executable path is required.");
    string path=Path.GetFullPath(executablePath);
    if(!File.Exists(path)||!string.Equals(Path.GetExtension(path),".exe",StringComparison.OrdinalIgnoreCase))return new ProgramLaunchResult(false,"The selected executable does not exist or is not an .exe.");
    using(var process=Process.Start(new ProcessStartInfo(path){UseShellExecute=true,WorkingDirectory=Path.GetDirectoryName(path)})){return new ProgramLaunchResult(process!=null,process==null?"Process creation failed.":"");}
   }catch(Exception ex){return new ProgramLaunchResult(false,ex.Message);}
  }
 }
 // Optional Agent Runtime adapter. Only an explicit RequestLaunch arms it;
 // ordinary menu animations emit notifications without launching anything.
 public sealed class SwordAgentRuntime:IDisposable {
  readonly ISwordSlashAction action;readonly IProgramLauncher launcher;
  Guid expected;string pending;
  public event EventHandler<ProgramLaunchResult> LaunchCompleted;
  public SwordAgentRuntime(ISwordSlashAction animation,IProgramLauncher tool){if(animation==null||tool==null)throw new ArgumentNullException();action=animation;launcher=tool;action.HitFrame+=Hit;}
  public void RequestLaunch(string executablePath,PointF target,int facing){pending=null;action.SwordSlash(target,facing);expected=action.RequestId;pending=executablePath??"";}
  public void CancelPendingLaunch(){pending=null;}
  void Hit(object sender,SwordHitEventArgs hit){
   if(pending==null||hit.RequestId!=expected)return;
   string path=pending;pending=null;ProgramLaunchResult result;
   try{result=launcher.Launch(path);}catch(Exception ex){result=new ProgramLaunchResult(false,ex.Message);}
   var handler=LaunchCompleted;if(handler!=null)handler(this,result);
  }
  public void Dispose(){pending=null;action.HitFrame-=Hit;}
 }
}
