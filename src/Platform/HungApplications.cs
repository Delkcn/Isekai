using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
namespace JouchoPet {
 sealed class HungApplication {
  internal int Id,Session;internal long Created;internal IntPtr Window;
  internal string Name,Title;internal bool Hung,Critical;
 }
 interface IHungApplications {
  List<HungApplication> Read();
  Task<ProgramLaunchResult> EndAsync(HungApplication target);
 }
 interface IHungProcess:IDisposable {
  HungApplication Inspect();
  ProgramLaunchResult End();
 }
 sealed class HungApplications:IHungApplications {
  readonly int ownId,session;readonly Func<HungApplication,IHungProcess> open;
  internal HungApplications(Func<HungApplication,IHungProcess> open=null){
   using(var self=Process.GetCurrentProcess()){ownId=self.Id;session=self.SessionId;}
   this.open=open??(target=>new NativeHungProcess(target.Id,target.Window,true));
  }
  internal bool Eligible(HungApplication app){return app!=null&&app.Id>0&&app.Id!=ownId&&app.Session==session&&app.Session!=0&&app.Window!=IntPtr.Zero&&app.Hung&&!app.Critical&&!String.Equals(app.Name,"explorer",StringComparison.OrdinalIgnoreCase)&&!String.Equals(app.Name,"ApplicationFrameHost",StringComparison.OrdinalIgnoreCase);}
  public List<HungApplication> Read(){
   var result=new List<HungApplication>();
   foreach(var process in Process.GetProcesses())using(process){
    try{if(process.Id==ownId||process.SessionId!=session)continue;IntPtr window=process.MainWindowHandle;
     if(window==IntPtr.Zero||!NativeHungProcess.IsHungAppWindow(window))continue;
     using(var handle=new NativeHungProcess(process.Id,window,false)){var app=handle.Inspect();if(Eligible(app))result.Add(app);}
    }catch(Win32Exception){}catch(InvalidOperationException){}catch(ArgumentException){}
   }
   result.Sort((a,b)=>StringComparer.CurrentCultureIgnoreCase.Compare(a.Name,b.Name));return result;
  }
  public Task<ProgramLaunchResult> EndAsync(HungApplication target){return Task.Run(()=>End(target));}
  internal ProgramLaunchResult End(HungApplication target){
   try{
    if(!Eligible(target))return new ProgramLaunchResult(false,"目标不属于可结束的未响应程序。");
    // Pin one process handle through identity verification and termination. Never kill by name/PID alone.
    using(var process=open(target)){
     var current=process.Inspect();
     if(!Eligible(current)||current.Id!=target.Id||current.Created!=target.Created||current.Window!=target.Window)
      return new ProgramLaunchResult(false,"程序已恢复响应、退出或身份改变，已取消结束操作。");
     return process.End();
    }
   }catch(Win32Exception ex){return new ProgramLaunchResult(false,ex.NativeErrorCode==5?"权限不足，未结束程序。可从米字法阵菜单打开任务管理器处理。":ex.Message);}
   catch(Exception ex){return new ProgramLaunchResult(false,ex.Message);}
  }
 }
 sealed class NativeHungProcess:IHungProcess {
  IntPtr handle;readonly int id;readonly IntPtr window;
  internal NativeHungProcess(int id,IntPtr window,bool terminate){this.id=id;this.window=window;handle=OpenProcess(terminate?0x101001u:0x1000u,false,id);if(handle==IntPtr.Zero)throw new Win32Exception();}
  public HungApplication Inspect(){
   long created,exited,kernel,user;bool critical;uint owner,session;
   if(!GetProcessTimes(handle,out created,out exited,out kernel,out user)||!IsProcessCritical(handle,out critical)||!ProcessIdToSessionId(id,out session))throw new Win32Exception();
   GetWindowThreadProcessId(window,out owner);
   if(owner!=(uint)id||!IsWindowVisible(window)||exited!=0)throw new InvalidOperationException("目标窗口已关闭或改变。");
   var title=new StringBuilder(512);GetWindowText(window,title,title.Capacity);
   using(var process=Process.GetProcessById(id))return new HungApplication{Id=id,Session=(int)session,Created=created,Window=window,Name=process.ProcessName,Title=title.ToString(),Hung=IsHungAppWindow(window),Critical=critical};
  }
  public ProgramLaunchResult End(){
   if(!TerminateProcess(handle,1))throw new Win32Exception();
   return WaitForSingleObject(handle,2000)==0?new ProgramLaunchResult(true,""):new ProgramLaunchResult(false,"结束请求已发出，程序尚未退出；请在任务管理器查看。");
  }
  public void Dispose(){if(handle!=IntPtr.Zero){CloseHandle(handle);handle=IntPtr.Zero;}}
  [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr OpenProcess(uint rights,bool inherit,int id);
  [DllImport("kernel32.dll",SetLastError=true)]static extern bool GetProcessTimes(IntPtr handle,out long created,out long exited,out long kernel,out long user);
  [DllImport("kernel32.dll",SetLastError=true)]static extern bool IsProcessCritical(IntPtr handle,out bool critical);
  [DllImport("kernel32.dll",SetLastError=true)]static extern bool ProcessIdToSessionId(int id,out uint session);
  [DllImport("kernel32.dll",SetLastError=true)]static extern bool TerminateProcess(IntPtr handle,uint code);
  [DllImport("kernel32.dll")]static extern uint WaitForSingleObject(IntPtr handle,uint ms);
  [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr handle);
  [DllImport("user32.dll")]internal static extern bool IsHungAppWindow(IntPtr window);
  [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr window);
  [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint owner);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr window,StringBuilder title,int size);
 }
}
