using System;
using System.Drawing;
namespace JouchoPet {
 // Reach the icon's height with the existing jump/drop pose and its forward
 // component. Horizontal staging makes room for that component even when the
 // icon is directly above/below; the icon stays still until actual pickup.
 sealed class ApproachLeap {
  internal readonly Point Launch,Landing;
  internal readonly int Facing,Vertical;
  readonly double arc,arcX;
  internal double Windup {get{return Vertical<0?.38:.32;}}
  internal double Flight {get{return Vertical<0?.95:.85;}}
  internal double Duration {get{return Windup+Flight+.62;}}
  internal static ApproachLeap Plan(Point start,Point contact,Rectangle area,Size canvas,int size,int preferredFacing,bool running) {
   int right=area.Right-canvas.Width,bottom=area.Bottom-canvas.Height;
   if(right<area.Left || bottom<area.Top || contact.Y<area.Top || contact.Y>bottom || Math.Abs(contact.Y-start.Y)<=1)return null;
   int vertical=Math.Sign(contact.Y-start.Y),lead=running?(int)Math.Ceiling(Math.Max(size*.18,RunGait.SpeedAt300*size/300*.42)):0;
   int landingX=Math.Max(area.Left,Math.Min(right,contact.X-preferredFacing*lead));
   int facing=Math.Abs(landingX-start.X)<5?preferredFacing:Math.Sign(landingX-start.X);
   double forward=size*(vertical<0?AnimationParameters.Get("leapUpForward")/100:.28)*Math.Min(1,Math.Abs(contact.Y-start.Y)/(size*.55));
   int distance=Math.Max(1,(int)Math.Round(forward));
   int launchX=landingX-facing*distance;
   if(launchX<area.Left || launchX>right){int alternate=landingX+facing*distance;if(alternate>=area.Left && alternate<=right){facing=-facing;launchX=alternate;}else launchX=Math.Max(area.Left,Math.Min(right,launchX));}
   return new ApproachLeap(new Point(launchX,start.Y),new Point(landingX,contact.Y),facing,area,canvas,size);
  }
  ApproachLeap(Point start,Point end,int facing,Rectangle area,Size canvas,int size){Launch=start;Landing=end;Facing=facing;Vertical=end.Y<start.Y?-1:1;arc=Math.Min(size*.055,Math.Max(0,Math.Min(start.Y,end.Y)-area.Top)*.5);arcX=facing*Math.Min(size*.015,Math.Max(0,facing<0?end.X-area.Left:area.Right-canvas.Width-end.X)*.5);}
  internal ApproachLeap FromActual(Point actual,Rectangle area,Size canvas,int size){int dx=Landing.X-Launch.X;int endX=Math.Max(area.Left,Math.Min(area.Right-canvas.Width,actual.X+dx));return new ApproachLeap(actual,new Point(endX,Landing.Y),Facing,area,canvas,size);}
  internal PointF At(double age){double t=Math.Max(0,Math.Min(1,(age-Windup)/Flight)),u=LeapMotion.Ease(t),lift=Math.Pow(Math.Sin(Math.PI*t),2);return new PointF((float)(Launch.X+(Landing.X-Launch.X)*u+arcX*lift),(float)(Launch.Y+(Landing.Y-Launch.Y)*u-arc*lift));}
  internal LeapPose Pose(double age){return LeapMotion.At(age,Vertical);}
 }
}
