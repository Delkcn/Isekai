using System;
using System.Drawing;
namespace JouchoPet {
 public interface IMagicCastAction {
  Guid Cast(PointF targetPosition);
  void CancelCast();
  event EventHandler<ProjectileHitEventArgs> ProjectileHit;
 }
 public interface IFileCastAction:IMagicCastAction { Guid CastFile(string path); }
 public sealed class ProjectileHitEventArgs:EventArgs {
  public Guid RequestId{get;private set;}
  public PointF Origin{get;private set;}
  public PointF TargetPosition{get;private set;}
  public string TargetFile{get;private set;}
  public ProjectileHitEventArgs(Guid id,PointF origin,PointF target,string targetFile=null){RequestId=id;Origin=origin;TargetPosition=target;TargetFile=targetFile;}
 }
 // Screen-space simulation: immutable target, deterministic time, one impact.
 // No window, model-provider, file or program-launch dependency.
 sealed class MagicBolt {
  internal readonly Guid Id;internal readonly PointF Origin,Target;
  internal readonly string TargetFile;
  internal const double ImpactDuration=MagicArrow.ImpactDuration,Speed=5.5*.70;
  internal readonly double Size,FlightDuration;
  internal double Age{get;private set;}
  internal bool Cancelled{get;private set;}
  bool notified;
  internal event EventHandler<ProjectileHitEventArgs> ProjectileHit;
  internal MagicBolt(Guid id,PointF origin,PointF target,double size,string targetFile=null){
   CastingMotion.Validate(origin);CastingMotion.Validate(target);
   if(double.IsNaN(size)||double.IsInfinity(size)||size<=0)throw new ArgumentException("Invalid size.");
   Id=id;Origin=origin;Target=target;Size=size;TargetFile=targetFile;
   FlightDuration=Math.Max(.06,RigParts.Length(origin,target)/(size*Speed));
  }
  internal double ImpactAge{get{return Math.Max(0,Age-FlightDuration);}}
  internal bool Hit{get{return !Cancelled&&Age>=FlightDuration;}}
  internal bool Done{get{return Cancelled||Age>=FlightDuration+ImpactDuration;}}
  internal PointF PositionAt(double age){if(age<=0)return Origin;if(age>=FlightDuration)return Target;return RigParts.Mix(Origin,Target,age/FlightDuration);}
  internal PointF Position{get{return PositionAt(Age);}}
  internal void Cancel(){Cancelled=true;}
  internal void Update(double dt){
   if(Done||!CastingMotion.ValidDelta(dt))return;
   Age+=dt;
   if(!notified&&Hit){notified=true;var e=ProjectileHit;if(e!=null)e(this,new ProjectileHitEventArgs(Id,Origin,Target,TargetFile));}
  }
 }
 struct CastPose {
  // Adapter to the existing planted body/JointArm hierarchy, no new body rig.
  internal SwordPose Body;
  internal double Core,ReleaseRing,HeadTilt,Push,Aim,Retract;
 }
 sealed class CastingMotion {
  internal const double MaxAim=65,MaxHeadTilt=24;
  internal const double LiftStart=.10,LiftDuration=.30/.65/.70,ChargeStart=LiftStart+LiftDuration,ChargeDuration=.70/.70,
   PushStart=ChargeStart+ChargeDuration,PushEnd=PushStart+.18,ReleaseAt=PushStart+.17,RecoverDuration=.35/.50/.70,Duration=PushEnd+RecoverDuration;
  internal Guid Id;internal PointF Target;
  internal bool Pending,Active;internal int Facing;internal double Age,Aim,HeadAim;
  bool released;
  internal bool Busy{get{return Pending||Active;}}
  internal static void Validate(PointF p){if(float.IsNaN(p.X)||float.IsNaN(p.Y)||float.IsInfinity(p.X)||float.IsInfinity(p.Y))throw new ArgumentException("Target must be a finite screen position.");}
  internal static bool ValidDelta(double dt){return dt>0&&!double.IsNaN(dt)&&!double.IsInfinity(dt);}
  internal Guid Request(PointF target,int facing){Validate(target);Cancel();Target=target;Facing=facing<0?-1:1;Id=Guid.NewGuid();Pending=true;return Id;}
  internal void Cancel(){Pending=Active=false;Age=0;released=false;}
  internal int ApproachDirection(Motion m,bool ready){return Pending&&ready&&m.Facing!=Facing?Facing:0;}
  internal bool Begin(Motion m,bool ready,PointF shoulder){
   if(!Pending||!ready||m.Facing!=Facing||m.Speed!=0||m.Settle<1||(m.State!="hold"&&m.State!="walk"))return false;
   Aim=Math.Max(-MaxAim,Math.Min(MaxAim,Math.Atan2(Target.Y-shoulder.Y,Math.Max(1,Math.Abs(Target.X-shoulder.X)))*180/Math.PI));
   HeadAim=Aim;Active=true;Pending=false;Age=0;return true;
  }
  internal bool Update(double dt){
   if(!Active||!ValidDelta(dt))return false;
   Age=Math.Min(Duration,Age+dt);Active=Age<Duration;
   if(!released&&Age>=ReleaseAt){released=true;return true;}return false;
  }
  internal CastPose Pose{get{var p=At(Age,Facing,Aim);p.HeadTilt=LookTilt(HeadAim,Facing,p.Body.Amount);return p;}}
  internal static double LookTilt(double aim,int facing,double amount){return (facing<0?-1:1)*Math.Max(-MaxHeadTilt,Math.Min(MaxHeadTilt,aim*.60))*Math.PI/180*amount;}
  internal static CastPose At(double t,int facing,double aim=0){
   t=Math.Max(0,Math.Min(Duration,t));aim=Math.Max(-MaxAim,Math.Min(MaxAim,aim));
   double lift=LeapMotion.Ease((t-LiftStart)/LiftDuration),recover=1-LeapMotion.Ease((t-PushEnd)/RecoverDuration);
   double push=LeapMotion.Ease((t-PushStart)/(PushEnd-PushStart)),amount=lift*recover;
   double retract=LeapMotion.Ease((t-ChargeStart)/ChargeDuration);
   return new CastPose{Body=new SwordPose{Age=t,Facing=facing<0?-1:1,Amount=amount,
    Upper=60,Elbow=-85,Wrist=-8,RootY=.004*LeapMotion.Ease(t/.15)*recover,
    RootX=0,Lag=.0015*Math.Sin(Math.PI*t/Duration)},Push=push,Aim=aim,Retract=retract,
    HeadTilt=LookTilt(aim,facing,amount),
    Core=t<ReleaseAt?LeapMotion.Ease((t-ChargeStart)/ChargeDuration):0,
    ReleaseRing=t>=ReleaseAt&&t<ReleaseAt+.10?(t-ReleaseAt)/.10:-1};
  }
 }
}
