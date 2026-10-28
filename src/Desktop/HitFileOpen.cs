using System;
using System.Diagnostics;
using System.IO;
namespace JouchoPet {
 internal interface IFileOpener { ProgramLaunchResult Open(string path); }
 // Use Windows' default Open action, exactly as opening the selected desktop file.
 sealed class ShellFileOpener:IFileOpener {
  internal readonly bool Administrator;
  readonly Func<ProcessStartInfo,Process> start;
  static bool SupportsRunAs(string path){
   string ext=Path.GetExtension(path).ToLowerInvariant();
   // Shortcut verbs depend on their target; let Shell resolve it, then handle
   // its explicit unsupported-verb result. Never flatten shortcut arguments.
   if(ext==".lnk"||ext==".exe"||ext==".com"||ext==".bat"||ext==".cmd")return true;
   foreach(var verb in new ProcessStartInfo(path).Verbs)if(String.Equals(verb,"runas",StringComparison.OrdinalIgnoreCase))return true;
   return false;
  }
  internal ShellFileOpener(bool administrator=false,Func<ProcessStartInfo,Process> start=null){Administrator=administrator;this.start=start??Process.Start;}
  internal ProcessStartInfo StartInfo(string path){return new ProcessStartInfo(Path.GetFullPath(path)){UseShellExecute=true,Verb=Administrator?"runas":"open",WorkingDirectory=Path.GetDirectoryName(Path.GetFullPath(path))};}
  public ProgramLaunchResult Open(string path){
   try{
    if(String.IsNullOrWhiteSpace(path)||!Path.IsPathRooted(path)||!File.Exists(path))return new ProgramLaunchResult(false,"目标文件已不存在。");
    var info=StartInfo(path);bool fallback=Administrator&&!SupportsRunAs(path);if(fallback)info.Verb="open";
    try{using(var process=start(info)) {}}
    catch(System.ComponentModel.Win32Exception ex){
     // Only unsupported launch/association errors fall back. Cancellation,
     // denied access, missing targets and busy files remain failures.
     if(info.Verb!="runas"||(ex.NativeErrorCode!=1155&&ex.NativeErrorCode!=50&&ex.NativeErrorCode!=193))throw;
     if(!File.Exists(path))throw;info.Verb="open";using(var process=start(info)){}fallback=true;
    }
    // Shell activation may hand off to an existing application and return no process.
    return new ProgramLaunchResult(true,""){OrdinaryOpenFallback=fallback};
   }catch(System.ComponentModel.Win32Exception ex){return new ProgramLaunchResult(false,Administrator&&ex.NativeErrorCode==1223?"已取消管理员授权。":ex.Message);}
   catch(Exception ex){return new ProgramLaunchResult(false,ex.Message);}
  }
 }
 // One selected target per action. Consume before calling external code so even
 // a duplicate/reentrant hit or an open failure cannot launch it twice.
 sealed class HitFileOpen {
  readonly IFileOpener opener;DesktopFileTarget pending;
  internal HitFileOpen(IFileOpener opener){this.opener=opener;}
  internal void Arm(DesktopFileTarget target){pending=target;}
  internal void Cancel(){pending=null;}
  internal ProgramLaunchResult Hit(){
   var target=pending;pending=null;if(target==null)return null;
   try{if(!target.Valid())return new ProgramLaunchResult(false,"目标文件已消失或图标位置改变。");return opener.Open(target.Path);}
   catch(Exception ex){return new ProgramLaunchResult(false,ex.Message);}
  }
 }
}
