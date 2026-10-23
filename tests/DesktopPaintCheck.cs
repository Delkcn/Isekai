using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using JouchoPet;
class DesktopPaintCheck {
 static int checks;
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);Console.WriteLine("PASS "+message);checks++;}
 sealed class QuietForm : Form {
  protected override bool ShowWithoutActivation {get{return true;}}
  protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x08000020;return p;}}
 }
 sealed class View : Control {
  public Point Accepted,Painted;public int Paints;
  public View(){SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.UserPaint,true);}
  protected override void OnPaint(PaintEventArgs e) {
   e.Graphics.Clear(Color.White);e.Graphics.FillRectangle(Brushes.SteelBlue,Accepted.X,Accepted.Y,32,32);
   Painted=Accepted;Paints++;base.OnPaint(e);
  }
 }
 [STAThread] static void Main() {
  try {
   foreach(Point axis in new[]{new Point(-1,0),new Point(1,0),new Point(0,-1),new Point(0,1)}) {
    Point a=new Point(150,150),b=new Point(a.X+axis.X*2,a.Y+axis.Y*2),spacing=new Point(80,100);
    Rectangle r=DesktopIconPaint.Region(a,b,spacing,32);
    Check(r.Contains(new Rectangle(a.X,a.Y,32,32)) && r.Contains(new Rectangle(b.X,b.Y,32,32)) &&
     r.Contains(new Point(a.X-35,a.Y+90)) && r.Contains(new Point(b.X+65,b.Y+90)),"dirty region includes old/new icon and label "+axis);
    Check(r.Width<200 && r.Height<200,"redraw is local "+axis);
   }
   // A separate STA simulates Explorer's independently owned child view.
   // The fixture is transparent and non-activating; it never touches real icons.
   View view=null;Form host=null;Exception workerError=null;
   using(var ready=new ManualResetEvent(false)) {
    var thread=new Thread(delegate() {
     try {
      host=new QuietForm{Opacity=0,ShowInTaskbar=false,FormBorderStyle=FormBorderStyle.None,StartPosition=FormStartPosition.Manual,Bounds=new Rectangle(Screen.PrimaryScreen.WorkingArea.Location,new Size(1400,320))};
      view=new View{Dock=DockStyle.Fill};host.Controls.Add(view);
      host.Shown+=delegate{view.Update();ready.Set();};Application.Run(host);
     }catch(Exception ex){workerError=ex;ready.Set();}
    });
    thread.SetApartmentState(ApartmentState.STA);thread.IsBackground=true;thread.Start();
    if(!ready.WaitOne(5000))throw new Exception("paint fixture timeout");
    if(workerError!=null)throw workerError;
    try {
     foreach(Point axis in new[]{new Point(-1,0),new Point(1,0),new Point(0,-1),new Point(0,1)})foreach(CarryStyle style in new[]{CarryStyle.Walk,CarryStyle.Run})foreach(int cells in axis.Y==0 && style==CarryStyle.Run?new[]{1,4,6}:new[]{1}) {
      int distance=axis.Y==0 && style==CarryStyle.Run?IconCarryPlan.PushDistance(96,true,true,cells):100;
      Point start=new Point(700,150),end=new Point(start.X+axis.X*distance,start.Y+axis.Y*distance),previous=start;
      var plan=new CarryTravel(start,end,300,style);
      var pickup=axis.Y==0 && style==CarryStyle.Run?new RunningPickup(new Point(150-axis.X*150,100),new Point(150,100),start,end,300,0):null;
      double duration=pickup==null?plan.Duration:pickup.Duration;
      view.Invoke((MethodInvoker)delegate{view.Accepted=start;});
      DesktopIconPaint.Present(view.Handle,start,start,new Point(80,100),32);
      Check(view.Painted==start,"initial frame painted "+axis);
      int steps=0;
      for(double t=0;t<duration+.05;t+=.05) {
       Point target=pickup==null?plan.At(t).Position:pickup.At(t).Icon;if(target==previous)continue;
       int before=view.Paints;
       view.Invoke((MethodInvoker)delegate{view.Accepted=target;});
       // Updating the position model alone must not count as visible progress.
       Check(view.Painted==previous,"model acknowledgement alone leaves old image "+axis+" step "+steps);
       DesktopIconPaint.Present(view.Handle,previous,target,new Point(80,100),32);
       if(view.Painted!=target || view.Paints<=before)throw new Exception("intermediate position was not painted before return");
       previous=target;steps++;
      }
      Check(steps>10 && view.Painted==end,"all intermediate child frames painted before acknowledgement "+axis+" / "+plan.Style+" / cells="+cells+" / "+steps);
     }
    }finally {
     if(host!=null && !host.IsDisposed)host.BeginInvoke((MethodInvoker)delegate{host.Close();});
     if(!thread.Join(5000))throw new Exception("paint fixture did not close");
    }
   }
   bool rejected=false;try{DesktopIconPaint.Present(IntPtr.Zero,Point.Empty,Point.Empty,new Point(80,100),32);}catch(InvalidOperationException){rejected=true;}
   Check(rejected,"missing view never redraws entire desktop");
   Console.WriteLine("All "+checks+" desktop paint checks passed.");
  }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}
 }
}
