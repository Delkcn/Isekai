using System;
using System.Drawing;
namespace JouchoPet {
 struct LeapPose {
  public double Crouch,Air,Reach,Hair,Petals,Flare,Time;
 }
 // Window trajectory and local pose share one clock. Menus pause both together.
 sealed class LeapMotion {
  public bool Pending {get;private set;}
  public bool Active {get;private set;}
  public bool Busy {get{return Pending || Active;}}
  public int Vertical {get;private set;}
  public int Facing {get;private set;}
  public double Age {get;private set;}
  public PointF Position {get;private set;}
  public Point Start {get;private set;}
  public Point Target {get;private set;}
  double arcX,arcY;
  public double Windup {get{return Vertical<0?.38:.32;}}
  public double Flight {get{return Vertical<0?.95:.85;}}
  public double Duration {get{return Windup+Flight+.62;}}
  public static double Ease(double t){t=Math.Max(0,Math.Min(1,t));return t*t*t*(t*(t*6-15)+10);}
  public void Request(int vertical,int facing){Cancel();Vertical=vertical<0?-1:1;Facing=facing<0?-1:1;Pending=true;}
  public void Cancel(){Pending=false;Active=false;Age=0;}
  public int ApproachDirection(Motion motion,bool ready){return Pending && ready && motion.Facing==0?Facing:0;}
  public bool CanBegin(Motion motion,bool ready){return Pending && ready && motion.Facing!=0 && motion.Speed==0 &&
    (motion.State=="hold" || (motion.State=="walk" && motion.Settle==1));}
  public bool Begin(Point start,Rectangle area,Size canvas,int size,int facing=0) {
   if(!Pending)return false;
   Pending=false;
   if(facing!=0)Facing=facing<0?-1:1;
   // No movement if the entire canvas cannot fit, or the requested edge is reached.
   int bottom=area.Bottom-canvas.Height,right=area.Right-canvas.Width;
   if(bottom<area.Top || right<area.Left || start.X<area.Left || start.X>right || start.Y<area.Top || start.Y>bottom)return false;
   Start=start;Target=new Point(start.X,Math.Max(area.Top,Math.Min(bottom,start.Y+Vertical*(int)Math.Round(size*.55))));
   if(Math.Abs(Target.Y-Start.Y)<3)return false;
   double forward=size*(Vertical<0?AnimationParameters.Get("leapUpForward")/100:.28)*Math.Min(1,Math.Abs(Target.Y-Start.Y)/(size*.55));
   Target=new Point(Math.Max(area.Left,Math.Min(right,start.X+Facing*(int)Math.Round(forward))),Target.Y);
   arcX=Facing*Math.Min(size*.015,Math.Max(0,Facing<0?Target.X-area.Left:right-Target.X)*.5);
   arcY=Math.Min(size*.055,Math.Max(0,Math.Min(Start.Y,Target.Y)-area.Top)*.5);
   Position=start;Age=0;Active=true;return true;
  }
  public bool Update(double dt) {
   if(!Active)return false;
   Age=Math.Min(Duration,Age+Math.Max(0,dt));
   double flight=Math.Max(0,Math.Min(1,(Age-Windup)/Flight));
   double arc=Math.Pow(Math.Sin(Math.PI*flight),2);
   Position=new PointF((float)(Start.X+(Target.X-Start.X)*Ease(flight)+arcX*arc),(float)(Start.Y+(Target.Y-Start.Y)*Ease(flight)-arcY*arc));
   if(Age>=Duration){Position=Target;Active=false;return true;}
   return false;
  }
  public LeapPose Pose {get{return At(Age,Vertical);}}
  public static LeapPose At(double age,int vertical) {
   double windup=vertical<0?.38:.32,flight=vertical<0?.95:.85;
   var p=new LeapPose{Time=age};
   if(age<=0 || age>=windup+flight+.62)return p;
   if(age<windup) {
    double t=Ease(age/windup);
    p.Crouch=(vertical<0?1:.38)*t;p.Reach=vertical>0?.65*t:0;p.Petals=.7*t;
   }else if(age<windup+flight) {
    double t=(age-windup)/flight;
    p.Crouch=(vertical<0?1:.38)*(1-Ease(t/.22));
    p.Air=Math.Pow(Math.Sin(Math.PI*t),2);
    p.Reach=vertical>0?.65*(1-Ease(t))+ .3*p.Air:.2*p.Air;
    p.Hair=.7*Math.Sin(2*Math.PI*t);
    p.Petals=.7*(1-Ease(t));p.Flare=Ease(t/.45);
   }else {
    double t=(age-windup-flight)/.62;
    p.Crouch=.85*Math.Pow(Math.Sin(Math.PI*Math.Min(1,t/.65)),2);
    p.Hair=Math.Sin(3*Math.PI*t)*Math.Pow(1-t,2);
    p.Petals=.6*Math.Pow(Math.Sin(Math.PI*t),2);p.Flare=t;
   }
   return p;
  }
 }
}
