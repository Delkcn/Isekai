using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
namespace JouchoPet {
 // A coalescing UI scheduler: no concurrent rendering and no queued-frame burst
 // after a slow frame. Stopwatch time remains the animation source of truth.
 sealed class FrameClock : IDisposable {
  readonly Control owner;
  readonly Stopwatch clock=Stopwatch.StartNew();
  System.Threading.Timer pulse;
  HighResolutionPulse precisePulse;
  int queued,running;
  double next;
  bool resolution;
  // Set by the UI render path; worker pulses never read animation state.
  public bool Turning {get;set;}
  public int TargetFramesPerSecond {get{return Turning?120:60;}}
  public event EventHandler Tick;
  public FrameClock(Control control){owner=control;}
  [DllImport("winmm.dll")] static extern uint timeBeginPeriod(uint period);
  [DllImport("winmm.dll")] static extern uint timeEndPeriod(uint period);
  public void Start() {
   if(Interlocked.Exchange(ref running,1)!=0)return;
   resolution=timeBeginPeriod(1)==0;next=clock.Elapsed.TotalSeconds;
   precisePulse=HighResolutionPulse.TryStart(delegate{Pulse(null);});
   // Older Windows versions keep the prior timer-queue implementation.
   if(precisePulse==null)pulse=new System.Threading.Timer(Pulse,null,0,2);
  }
  void Pulse(object state) {
   if(Volatile.Read(ref running)==0 || !owner.IsHandleCreated || owner.IsDisposed)return;
   if(clock.Elapsed.TotalSeconds<Volatile.Read(ref next))return;
   if(Interlocked.CompareExchange(ref queued,1,0)!=0)return;
   try {owner.BeginInvoke((MethodInvoker)Dispatch);}
   catch(InvalidOperationException){Interlocked.Exchange(ref queued,0);}
  }
  void Dispatch() {
   try {
    if(Volatile.Read(ref running)==0 || owner.IsDisposed)return;
    double now=clock.Elapsed.TotalSeconds;
    EventHandler handler=Tick;if(handler!=null)handler(this,EventArgs.Empty);
    // Render has selected the current action, including entry/exit this tick.
    double period=1.0/TargetFramesPerSecond;
    // Skip elapsed deadlines; never replay stale animation snapshots.
    double deadline=Volatile.Read(ref next);
    Volatile.Write(ref next,deadline+(Math.Floor(Math.Max(0,now-deadline)/period)+1)*period);
   }finally{Interlocked.Exchange(ref queued,0);}
  }
  public void Stop() {
   Interlocked.Exchange(ref running,0);
   var old=Interlocked.Exchange(ref pulse,null);if(old!=null)old.Dispose();
   var precise=Interlocked.Exchange(ref precisePulse,null);if(precise!=null)precise.Dispose();
   if(resolution){timeEndPeriod(1);resolution=false;}
  }
  public void Dispose(){Stop();}
 }
}
