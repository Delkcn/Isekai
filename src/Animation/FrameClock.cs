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
  int queued,running;
  double next;
  bool resolution;
  public event EventHandler Tick;
  public FrameClock(Control control){owner=control;}
  [DllImport("winmm.dll")] static extern uint timeBeginPeriod(uint period);
  [DllImport("winmm.dll")] static extern uint timeEndPeriod(uint period);
  public void Start() {
   if(Interlocked.Exchange(ref running,1)!=0)return;
   resolution=timeBeginPeriod(1)==0;next=clock.Elapsed.TotalSeconds;
   pulse=new System.Threading.Timer(Pulse,null,0,2);
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
    double now=clock.Elapsed.TotalSeconds,period=1.0/60;
    // Skip elapsed deadlines; never replay stale animation snapshots.
    double deadline=Volatile.Read(ref next);
    Volatile.Write(ref next,deadline+(Math.Floor(Math.Max(0,now-deadline)/period)+1)*period);
    EventHandler handler=Tick;if(handler!=null)handler(this,EventArgs.Empty);
   }finally{Interlocked.Exchange(ref queued,0);}
  }
  public void Stop() {
   Interlocked.Exchange(ref running,0);
   var old=Interlocked.Exchange(ref pulse,null);if(old!=null)old.Dispose();
   if(resolution){timeEndPeriod(1);resolution=false;}
  }
  public void Dispose(){Stop();}
 }
}
