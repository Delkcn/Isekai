using System;
namespace JouchoPet {
 // Ordinary walking keeps its neutral turn. Running gathers the last step while
 // decelerating, then coasts through a shorter turn without a stationary hold.
 sealed class Motion {
  public string State="idle";
  public int Facing;
  public double Speed,Phase,Angle,RunBlend,Settle=1;
  double age,fromAngle,toAngle,duration;
  bool flowTurn,runArrival,runLaunch;
  double gatherSpeed,gatherSettle,gatherBlend,turnStartSpeed,turnEndSpeed;
  public bool RunningTurn {get{return State=="turn" && flowTurn;}}
  public Func<bool> SelectLightTurn;
  public bool LightTurn {get;private set;}
  public double TurnProgress {get{return State=="turn"?Math.Max(0,Math.Min(1,age/duration)):1;}}
  public double TurnFrom {get{return fromAngle;}}
  public bool SupportNear {get{return toAngle<fromAngle;}}
  static double Ease(double x) { x=Math.Max(0,Math.Min(1,x));return x*x*x*(x*(x*6-15)+10); }
  void Turn(int to,bool flowing=false,bool runAfter=false,double size=300) {
   if(Facing==to) { State=to==0?"idle":"walk";Angle=Facing;flowTurn=runArrival=false;runLaunch=flowing && runAfter && to!=0;RunBlend=runLaunch?1:0;if(to==0)Speed=0;age=0;return; }
   fromAngle=Facing;toAngle=to;duration=Math.Abs(to-fromAngle)>1?0.92:0.64;
   LightTurn=SelectLightTurn!=null && SelectLightTurn();
   if(LightTurn)duration=AnimationParameters.Get(Math.Abs(to-fromAngle)>1?"turnDuration":"frontTurnDuration");
   flowTurn=flowing;runArrival=runAfter && to!=0;
   turnStartSpeed=flowing?Speed:0;turnEndSpeed=runArrival?to*RunGait.SpeedAt300*size/300*.22:0;
   if(flowing)duration*=.65;
   age=0;State="turn";Speed=turnStartSpeed;Settle=1;RunBlend=0;
  }
  public void Reset() { State="idle";Facing=0;Angle=0;Speed=0;RunBlend=0;Settle=1;LightTurn=false;age=0;flowTurn=runArrival=runLaunch=false; }
  public double Update(double dt,int desired,double size,bool holdFacing=false,bool running=false) {
   age+=dt;
   if(State=="idle") { Speed=0;Settle=1;if(desired!=0) Turn(desired,running && !holdFacing,running && !holdFacing,size); }
   else if(State=="turn") {
    if(holdFacing){flowTurn=runArrival=false;Speed=0;}
    if(!running || desired!=(int)toAngle){runArrival=false;turnEndSpeed=0;}
    double progress=age/duration;
    Angle=fromAngle+(toAngle-fromAngle)*(LightTurn?PivotTurn.Progress(progress):Ease(progress));
    if(flowTurn)Speed=turnStartSpeed+(turnEndSpeed-turnStartSpeed)*Ease(progress);
    if(age>=duration) {
     bool wasFlow=flowTurn;Facing=(int)toAngle;Angle=Facing;State=Facing==0?"idle":"walk";
     if(Facing!=0)Phase=0;else Speed=0;
     runLaunch=flowTurn && runArrival;RunBlend=runLaunch?1:0;flowTurn=false;age=0;
     // A changed request completes the current facing safely, then continues
     // directly into the new turn instead of inserting an ordinary stop.
     if(wasFlow && !holdFacing && desired!=Facing)Turn(desired,true,running && desired!=0,size);
    }
   } else if(State=="walk") {
    if(desired!=Facing) {if(RunBlend>.5 && !holdFacing){State="rungather";gatherSpeed=Speed;gatherSettle=Settle;gatherBlend=RunBlend;}else State="brake";age=0;runLaunch=false;}
    else { double target=running?RunGait.SpeedAt300*size/300:36;Speed+=(Facing*target-Speed)*(1-Math.Exp(-3.8*dt));Settle=1-Ease(age/(runLaunch?.18:.28)); }
   } else if(State=="rungather") {
    if(holdFacing){State="brake";age=0;}
    else {double t=Ease(age/.18);Speed=gatherSpeed*(1-.78*t);Settle=gatherSettle+(1-gatherSettle)*t;
     if(age>=.18)Turn(desired,true,running && desired!=0,size);}
   } else if(State=="brake") {
    Speed*=Math.Exp(-9*dt);
    if(Math.Abs(Speed)<0.6) { Speed=0;State="settle";age=0; }
   } else if(State=="settle") {
    Settle=Ease(age/0.28);
    if(age>=0.28) { if(holdFacing && desired==0) { State="hold";age=0; } else Turn(desired); }
   }
   if(State=="hold") { Speed=0;Settle=1;if(desired!=0 || !holdFacing)Turn(desired); }
   // Retain the run through braking, then ease the pose down before turning.
   double runTarget=State=="walk" && running && desired==Facing?1:0;
   if(State=="brake")runTarget=RunBlend;
   if(State=="rungather")runTarget=gatherBlend;
   RunBlend+=(runTarget-RunBlend)*(1-Math.Exp(-7*dt));
   // Exponential tails must eventually release the dedicated running renderer.
   if(runTarget==0 && RunBlend<.001)RunBlend=0;
   if(runTarget==1 && RunBlend>.999)RunBlend=1;
   if(State=="turn" || State=="idle" || State=="hold")RunBlend=0;
   if(State=="walk" || State=="brake" || State=="rungather" || (State=="turn" && flowTurn)) {
    double travel=Speed*dt;
    if(State!="turn")Phase+=Math.Abs(travel)*RunGait.Contact(RunBlend)/RunGait.Stride(size,RunBlend);
    return travel;
   }
   return 0;
  }
  public void Boundary() { Speed=0;State="settle";age=0;flowTurn=runArrival=runLaunch=false; }
  // The carry trajectory owns translation; gait follows its continuous distance.
  public void FollowPush(double distance,double velocity,int facing,double size,bool runningCarry=false) {
   Facing=facing;Angle=facing;Speed=facing*velocity;RunBlend=runningCarry?1:0;State=velocity>0?"walk":"hold";age=0;
   flowTurn=runArrival=runLaunch=false;
   Phase+=Math.Max(0,distance)*RunGait.Contact(RunBlend)/RunGait.Stride(size,RunBlend);
   Settle=1-Ease(velocity/12);
  }
 }
}
