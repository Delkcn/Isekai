using System;
using System.Drawing;
namespace JouchoPet {
 public interface ISwordSlashAction {
  void SwordSlash(PointF targetPosition,int facingDirection);
  Guid RequestId {get;}
  event EventHandler<SwordHitEventArgs> HitFrame;
 }
 public sealed class SwordHitEventArgs:EventArgs {
  public Guid RequestId {get;private set;}
  public PointF TargetPosition {get;private set;}
  public int FacingDirection {get;private set;}
  public SwordHitEventArgs(Guid id,PointF target,int facing){RequestId=id;TargetPosition=target;FacingDirection=facing;}
 }
 struct SwordPose {
  internal double Age,Amount,Upper,Elbow,Wrist,Opacity,RootX,RootY,Lag;
  internal int Facing;
 }
 // Five authored poses, not a continuously spinning shoulder. Elbow is local
 // to the upper arm and the wrist is local to the forearm.
 public sealed class SwordMotion:ISwordSlashAction {
  internal const double RevealAt=.37,RevealDuration=.10;
  internal const double Duration=1.13,StrikeStart=.47,StrikeEnd=.63,HitAt=.60,FadeStart=.88;
  // Hold the authored highest pose while the independent conjuring clock runs.
  internal static double PoseTime(double age){return age<=RevealAt?age:age<StrikeStart?RevealAt:age-RevealDuration;}
  internal bool Pending,Active;internal bool Busy{get{return Pending||Active;}}
  internal int Facing;internal double Age;
  public Guid RequestId {get;private set;}
  public event EventHandler<SwordHitEventArgs> HitFrame;
  PointF target;bool hit;
  public void SwordSlash(PointF targetPosition,int facingDirection){
   if(float.IsNaN(targetPosition.X)||float.IsInfinity(targetPosition.X)||float.IsNaN(targetPosition.Y)||float.IsInfinity(targetPosition.Y))throw new ArgumentException("Target must be a finite screen position.");
   Cancel();RequestId=Guid.NewGuid();target=targetPosition;Facing=facingDirection<0?-1:1;Pending=true;
  }
  internal void Request(int direction){SwordSlash(PointF.Empty,direction);}
  internal void Cancel(){Pending=Active=false;Age=0;hit=false;}
  internal int ApproachDirection(Motion motion,bool ready){return Pending&&ready&&motion.Facing!=Facing?Facing:0;}
  internal bool Begin(Motion motion,bool ready){if(!Pending||!ready||motion.Facing!=Facing||motion.Speed!=0||motion.Settle<1||(motion.State!="hold"&&motion.State!="walk"))return false;Pending=false;Active=true;Age=0;return true;}
  internal void Update(double dt){
   if(!Active||double.IsNaN(dt)||dt<=0)return;
   Age=Math.Min(Duration,Age+dt);if(Age>=Duration)Active=false;
   if(!hit&&Age>=HitAt){hit=true;var callback=HitFrame;if(callback!=null)callback(this,new SwordHitEventArgs(RequestId,target,Facing));}
  }
  internal SwordPose Pose{get{return At(Age,Facing);}}
  static readonly double[] times={0,.15,.37,.46,.53,.64,.78,1.03};
  // Shoulder and elbow carry the grip across the torso; no full-circle swing.
  static readonly double[] upper={90,55,-15,60,110,115,105,90};
  static readonly double[] elbow={0,-55,-70,-55,-40,-35,-20,0};
  static readonly double[] wrist={0,-15,-25,-25,-25,-20,-10,0};
  static readonly double[] rootY={0,.008,.007,.007,.010,.008,.003,0},rootX={0,0,0,.004,.007,.007,.003,0};
  static double Tangent(double[] v,int i){if(i==0||i==v.Length-1)return 0;double a=(v[i]-v[i-1])/(times[i]-times[i-1]),b=(v[i+1]-v[i])/(times[i+1]-times[i]);return a*b<=0?0:2*a*b/(a+b);}
  static double Curve(double[] v,double t){int i=0;while(i<times.Length-2&&t>times[i+1])i++;double dt=times[i+1]-times[i],u=Math.Max(0,Math.Min(1,(t-times[i])/dt)),u2=u*u,u3=u2*u;return (2*u3-3*u2+1)*v[i]+(u3-2*u2+u)*dt*Tangent(v,i)+(-2*u3+3*u2)*v[i+1]+(u3-u2)*dt*Tangent(v,i+1);}
  internal static SwordPose At(double age,int facing){age=Math.Max(0,Math.Min(Duration,age));double t=PoseTime(age);return new SwordPose{Age=age,Facing=facing<0?-1:1,Amount=LeapMotion.Ease(t/.15)*(1-LeapMotion.Ease((t-.78)/.25)),Upper=Curve(upper,t),Elbow=Curve(elbow,t),Wrist=Curve(wrist,t),RootX=Curve(rootX,t),RootY=Curve(rootY,t),Opacity=LeapMotion.Ease((age-RevealAt)/RevealDuration)*(1-LeapMotion.Ease((age-FadeStart)/.25)),Lag=.006*Math.Sin(Math.PI*LeapMotion.Ease((t-.46)/.48))};}
 }
}
