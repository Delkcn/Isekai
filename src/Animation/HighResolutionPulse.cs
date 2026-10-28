using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
namespace JouchoPet {
 // Windows high-resolution waitable timer: unlike a timer-queue interval,
 // this can wake the existing coalescing UI scheduler between 15.6 ms ticks.
 sealed class HighResolutionPulse : IDisposable {
  sealed class TimerHandle : WaitHandle {public TimerHandle(IntPtr handle){SafeWaitHandle=new SafeWaitHandle(handle,true);}}
  readonly TimerHandle timer;readonly ManualResetEvent stop=new ManualResetEvent(false);readonly Thread worker;int disposed;
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr CreateWaitableTimerExW(IntPtr attributes,string name,uint flags,uint access);
  [DllImport("kernel32.dll",SetLastError=true)]static extern bool SetWaitableTimer(SafeWaitHandle timer,ref long dueTime,int period,IntPtr completion,IntPtr argument,bool resume);
  HighResolutionPulse(TimerHandle handle,Action callback){
   timer=handle;worker=new Thread(delegate(){var waits=new WaitHandle[]{stop,timer};while(WaitHandle.WaitAny(waits)==1)callback();});
   worker.IsBackground=true;worker.Name="Pet frame pulse";worker.Start();
  }
  internal static HighResolutionPulse TryStart(Action callback){
   IntPtr handle;try{handle=CreateWaitableTimerExW(IntPtr.Zero,null,2,0x100002);}catch(EntryPointNotFoundException){return null;}
   if(handle==IntPtr.Zero)return null;
   var timer=new TimerHandle(handle);long due=-20000;
   if(!SetWaitableTimer(timer.SafeWaitHandle,ref due,2,IntPtr.Zero,IntPtr.Zero,false)){timer.Dispose();return null;}
   try{return new HighResolutionPulse(timer,callback);}catch{timer.Dispose();throw;}
  }
  public void Dispose(){if(Interlocked.Exchange(ref disposed,1)!=0)return;stop.Set();worker.Join();timer.Dispose();stop.Dispose();}
 }
}
