using System;
namespace JouchoPet {
 public interface IDailyActions {
  bool Wink();
  bool Think(double holdSeconds=1.5);
  bool AdjustHair();
  bool ScratchHead();
  bool LookOut(double holdSeconds=1.8);
  void CancelDailyAction();
 }
 enum DailyAction { None, Wink, Thinking, Hair, Lookout, Sigil }
 struct DailyPose {
  public double Wink,Think,Hair,Lift,Stroke,Scout,Sigil,Fist,Glyph,Glow,SigilAlpha,SigilTime;
  public int SigilSeed;
  public bool CrossOnly;
  public DailyPose Scale(double a){return new DailyPose{Wink=Wink*a,Think=Think*a,Hair=Hair*a,Lift=Lift*a,Stroke=Stroke*a,Scout=Scout*a,Sigil=Sigil*a,Fist=Fist*a,Glyph=Glyph*a,Glow=Glow*a,SigilAlpha=SigilAlpha*a,SigilTime=SigilTime,SigilSeed=SigilSeed,CrossOnly=CrossOnly};}
 }
 // One finite timeline; cancellation samples the current pose and returns it to rest.
 sealed class DailyMotion {
  public DailyAction Kind {get;private set;}
  public bool Pending {get;private set;}
  public bool Active {get{return Kind!=DailyAction.None&&!Pending;}}
  public bool Busy {get{return Kind!=DailyAction.None;}}
  internal bool Cancelling {get{return cancelAge>=0;}}
  public double Age {get;private set;}
  double hold=1.5,cancelAge=-1;DailyPose cancelled;static int sigilSequence=Environment.TickCount;int sigilSeed;
  public DailyPose Pose {get{if(cancelAge>=0)return cancelled.Scale(1-Ease(cancelAge/.24));var pose=At(Kind,Age,hold);if(Kind==DailyAction.Sigil)pose.SigilSeed=sigilSeed;return pose;}}
  internal static double Ease(double t){t=Math.Max(0,Math.Min(1,t));return t*t*t*(10+t*(-15+6*t));}
  public bool Request(DailyAction kind,double seconds=1.5){
   if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0||seconds>600)throw new ArgumentOutOfRangeException("seconds");
   if(Busy||kind==DailyAction.None)return false;
   if(kind==DailyAction.Sigil)sigilSeed=unchecked(++sigilSequence);
   Kind=kind;hold=seconds;Age=0;Pending=true;cancelAge=-1;return true;
  }
  public void Cancel(){if(Pending){Reset();return;}if(Active&&cancelAge<0){cancelled=Pose;cancelAge=0;}}
  public void Reset(){Kind=DailyAction.None;Pending=false;Age=0;cancelAge=-1;}
  public void Update(double dt,bool ready){
   if(dt<=0||!Busy)return;
   if(Pending){if(!ready)return;Pending=false;return;}
   if(cancelAge>=0){cancelAge+=dt;if(cancelAge>=.24)Reset();return;}
   Age+=dt;if(Age>=Duration(Kind,hold))Reset();
  }
  internal static double Duration(DailyAction kind,double hold=1.5){return kind==DailyAction.Sigil?2.0:kind==DailyAction.Lookout?1.1+hold:kind==DailyAction.Wink?1.48:kind==DailyAction.Thinking?.6+hold:2.05;}
  internal static DailyPose At(DailyAction kind,double t,double hold=1.5){
   if(t<0||t>=Duration(kind,hold))return new DailyPose();
   if(kind==DailyAction.Sigil){double arm=t<.35?Ease(t/.35):t<1.6?1:1-Ease((t-1.6)/.4);return new DailyPose{Sigil=arm,Fist=Ease((t-.52)/.16)*arm,Glow=Ease((t-.52)/.015)*(1-Ease((t-.54)/.075)),Glyph=Ease((t-.52)/.16),SigilTime=t,SigilAlpha=t<1.60?Ease((t-.52)/.04):0};}
   if(kind==DailyAction.Lookout){double q=t<.55?Ease(t/.55):t<.55+hold?1:1-Ease((t-.55-hold)/.55);return new DailyPose{Scout=q};}
   if(kind==DailyAction.Wink)return new DailyPose{Wink=t<.40?Ease(t/.40):t<.88?1:1-Ease((t-.88)/.60)};
   if(kind==DailyAction.Thinking)return new DailyPose{Think=t<.3?Ease(t/.3):t<.3+hold?1:1-Ease((t-.3-hold)/.3)};
   if(kind==DailyAction.Hair)return new DailyPose{
    Hair=t<.2?Ease(t/.2):t<1.65?1:1-Ease((t-1.65)/.4),
    Lift=t<.2?0:t<.90?Ease((t-.2)/.70):t<1.65?1:1-Ease((t-1.65)/.4),
    Stroke=t<.90||t>=1.65?0:.5-.5*Math.Cos(6*Math.PI*(t-.90)/.75)};
   return new DailyPose();
  }
 }
}
