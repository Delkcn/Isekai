using System;
using System.Drawing;
namespace JouchoPet {
 public interface IUtilityActions {
  bool Teleport(Point screenFootPoint);
  bool LightBeamFile(string desktopFilePath);
  bool LevitateIcon(string iconId,Point screenDestination);
  bool LookOut(double holdSeconds=1.8);
  bool MiSigil();
  bool CrossSigil();
  void CancelUtilityAction();
 }
 enum UtilityAction {None,Teleport,Beam,Levitate}
 // Three short, independent timelines. Screen coordinates are owned by Pet.
 sealed class UtilityMotion {
  internal UtilityAction Kind;internal double Age,Transit=1.05;internal int Facing;
  internal bool Pending,Relocated;
  internal bool Busy{get{return Kind!=UtilityAction.None;}}
  internal bool Active{get{return Busy&&!Pending;}}
  internal double Duration{get{return Kind==UtilityAction.Teleport?1.3:Kind==UtilityAction.Beam?1.65:2.20+Transit;}}
  internal double Amount{get{return Reach(Kind,Age,Transit);}}
  internal static double Reach(UtilityAction kind,double t,double transit=1.05){double end=kind==UtilityAction.Beam?1.10:1.65+transit;return t<.45?DailyMotion.Ease(t/.45):t<end?1:1-DailyMotion.Ease((t-end)/.55);}
  internal static double BeamAlpha(double t){return t<.50||t>=1.10?0:Math.Sin(Math.PI*(t-.50)/.60);}
  internal static double Opacity(double t){return t<.55?1-DailyMotion.Ease(t/.55):t<.70?0:DailyMotion.Ease((t-.70)/.60);}
  internal static PointF IconAt(PointF start,PointF end,double t,double lift,double transit=1.05){
   if(t<.45)return start;
   if(t<1.05)return RigParts.Add(start,0,-lift*DailyMotion.Ease((t-.45)/.60));
   if(t<1.05+transit)return RigParts.Add(RigParts.Mix(start,end,DailyMotion.Ease((t-1.05)/transit)),0,-lift);
   return RigParts.Add(end,0,-lift*(1-DailyMotion.Ease((t-1.05-transit)/.60)));
  }
  internal void Request(UtilityAction k,int facing){Kind=k;Facing=facing;Pending=true;Age=0;Relocated=false;}
  internal void Reset(){Kind=UtilityAction.None;Pending=false;Age=0;Relocated=false;}
 }
}
