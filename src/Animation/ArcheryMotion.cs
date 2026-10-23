using System;
using System.Drawing;
namespace JouchoPet {
 struct ArcheryPose {public double Age,Yaw,Ready,Reach,Pull,Draw,Recoil,Bow,Arrow,Release,Lower,Conjure;public int Facing;}
 sealed class ArcheryMotion {
  internal const double ReleaseAt=2.5,Duration=4.85;
  internal const double AimDegrees=25;
  internal const double ConjureStart=.15,BowAppearAt=.95,FullDrawAt=2.2;
  internal bool Pending {get;private set;}internal bool Active {get;private set;}internal bool Busy {get{return Pending||Active;}}
  internal int Facing {get;private set;}internal double Age {get;private set;}
  bool fired;
  internal void Request(int direction){Cancel();Facing=direction<0?-1:1;Pending=true;}
  internal void Cancel(){Pending=Active=fired=false;Age=0;}
  internal int ApproachDirection(Motion motion,bool ready){return Pending && ready && motion.Facing!=Facing?Facing:0;}
  internal bool Begin(Motion motion,bool ready){if(!Pending || !ready || motion.Facing!=Facing || motion.Speed!=0 || motion.Settle<1 || (motion.State!="hold" && motion.State!="walk"))return false;Pending=false;Active=true;Age=0;return true;}
  internal bool Update(double dt){if(!Active)return false;Age=Math.Min(Duration,Age+Math.Max(0,dt));bool release=!fired && Age>=ReleaseAt;if(release)fired=true;if(Age>=Duration)Active=false;return release;}
  internal ArcheryPose Pose {get{return At(Age,Facing);}}
  static double Ramp(double t,double from,double to){return LeapMotion.Ease((t-from)/(to-from));}
  // One uninterrupted pull eases into the full-draw hold.
  internal static double DrawProgress(double t){return Ramp(t,1.15,FullDrawAt);}
  internal static ArcheryPose At(double t,int direction){double lower=Ramp(t,3.15,4.65),draw=DrawProgress(t);return new ArcheryPose{Age=t,Facing=direction<0?-1:1,Yaw=0,Ready=Ramp(t,ConjureStart,BowAppearAt)*(1-lower),Lower=lower,Reach=1,Draw=draw,Pull=draw*(1-Ramp(t,ReleaseAt,ReleaseAt+.14)),Release=Ramp(t,ReleaseAt,2.77),Recoil=t>=ReleaseAt?Math.Sin(Math.PI*Math.Min(1,(t-ReleaseAt)/.35))*.012:0,Conjure=Ramp(t,ConjureStart,BowAppearAt),Bow=t>=BowAppearAt?1-Ramp(t,2.55,3.10):0,Arrow=t<ReleaseAt?Ramp(t,BowAppearAt,1.4):0};}
  internal static PointF Axis(int facing){double angle=AimDegrees*Math.PI/180;return new PointF((float)((facing<0?-1:1)*Math.Cos(angle)),(float)-Math.Sin(angle));}
  // The left, covered arm holds the bow in both views. Targets are registered
  // to each approved side drawing; the bow wrist stays within its arm reach.
  internal static PointF BowWrist(int width,double size,int facing){var art=CharacterArt.Definition(facing<0?"profile-left":"profile-right");double scale=CharacterArt.Scale(art,size);PointF shoulder=new PointF((float)(width*.5+((facing<0?171:1618)-art.Anchor)*scale),(float)CharacterArt.Y(art,184,size));if(facing>0)shoulder=RigParts.Add(shoulder,-size*.022,size*.004);
   // Intersect the jaw's aiming ray with the original fully extended wrist
   // radius. Bow grip, arrow shaft and drawing finger then share one axis.
   PointF axis=Axis(facing),cheek=FaceSide(width,size,facing);double x=shoulder.X-cheek.X,y=shoulder.Y-cheek.Y,along=x*axis.X+y*axis.Y,perpendicular=x*axis.Y-y*axis.X,radius=size*Math.Sqrt(.252*.252+.11*.11);
   double distance=along+Math.Sqrt(Math.Max(0,radius*radius-perpendicular*perpendicular));return RigParts.Add(cheek,axis.X*distance,axis.Y*distance);
  }
  internal static PointF Grip(int width,double size,int facing){PointF axis=Axis(facing);var art=CharacterArt.Definition("profile-left");double hand=CharacterArt.Y(art,311,size)-CharacterArt.Y(art,301,size);return RigParts.Add(BowWrist(width,size,facing),axis.X*hand,axis.Y*hand);}
  internal static PointF FaceSide(int width,double size,int facing){return new PointF((float)(width*.5+(facing<0?.008:.005)*size),(float)(28+size*.305));}
  // Old cubic bow projects .0306172839506173s ahead of its grip. Including
  // the string plane at -.045s, the complete original depth is .07561728395s.
  internal static double BowDepth(double size){return size*.151234567901235;}
  internal static double ArrowLength(double size){return size*.56;}
  internal static PointF Nock(int width,double size,ArcheryPose pose){
   PointF grip=Grip(width,size,pose.Facing),axis=Axis(pose.Facing),cheek=FaceSide(width,size,pose.Facing);
   // The nock slides along the same 25-degree ray through the grip. Keeping
   // it on this ray also gives the independent painted string exact contact.
   double fullDepth=(grip.X-cheek.X)*axis.X+(grip.Y-cheek.Y)*axis.Y;
   double travel=Math.Max(0,fullDepth-BowDepth(size));
   return RigParts.Add(cheek,axis.X*(travel*(1-pose.Pull)-size*.025*pose.Pull),axis.Y*(travel*(1-pose.Pull)-size*.025*pose.Pull));
  }
  internal static PointF DrawingHand(int width,double size,ArcheryPose pose){
   if(pose.Age<ReleaseAt)return Nock(width,size,pose);
   // Fingers release the string; the wrist follows gently behind the cheek
   // instead of chasing the string forward when it springs back.
   PointF axis=Axis(pose.Facing);
   return RigParts.Add(FaceSide(width,size,pose.Facing),-axis.X*size*.025-pose.Facing*size*.018*pose.Release,-axis.Y*size*.025+size*.002*pose.Release);
  }
 }
 sealed class MagicArrow {
  internal readonly PointF Origin,Impact,Direction;internal readonly Rectangle Screen;
  internal readonly double Size,FlightTime;
  internal const double RingDuration=.65,RingInterval=.1,ImpactDuration=RingDuration+2*RingInterval;
  internal const int RingCount=3;
  internal const double TrailHold=.22,TrailFade=.11;
  internal double Age {get;private set;}
  internal bool Done {get{return Age>=FlightTime+Math.Max(ImpactDuration,TrailHold+TrailFade);}}
  internal MagicArrow(PointF origin,int facing,Rectangle screen,double size){Origin=origin;Direction=ArcheryMotion.Axis(facing);Screen=screen;Size=size;
   double tx=Direction.X>0?(screen.Right-origin.X)/Direction.X:(screen.Left-origin.X)/Direction.X,ty=(screen.Top-origin.Y)/Direction.Y;
   double distance=Math.Max(0,Math.Min(tx,ty));Impact=RigParts.Add(origin,Direction.X*distance,Direction.Y*distance);FlightTime=distance/(size*12.8);}
  internal void Update(double dt){Age=Math.Min(FlightTime+Math.Max(ImpactDuration,TrailHold+TrailFade),Age+Math.Max(0,dt));}
  internal double TrailOpacity(double emittedAt){return 1-LeapMotion.Ease((Age-emittedAt-TrailHold)/TrailFade);}
  // Emit from the actual arrowhead path, including the final impact point.
  internal PointF TrailPoint(double emittedAt){if(FlightTime<=0||emittedAt>=FlightTime)return Impact;if(emittedAt<=0)return Origin;return RigParts.Mix(Origin,Impact,emittedAt/FlightTime);}
  internal PointF Tip {get{return RigParts.Mix(Origin,Impact,FlightTime<=0?1:Math.Min(1,Age/FlightTime));}}
  internal double ImpactAge {get{return Age-FlightTime;}}
  internal double ProgressAt(int ring){return Math.Max(0,Math.Min(1,(ImpactAge-ring*RingInterval)/RingDuration));}
  internal double OpacityAt(int ring){return ImpactAge<ring*RingInterval?0:Math.Pow(1-ProgressAt(ring),1.5);}
  internal double RadiusAt(int ring){return Size*.75*ProgressAt(ring);}
  internal double RingProgress {get{return ProgressAt(0);}}
  internal double Radius {get{return RadiusAt(0);}}
  internal double RingOpacity {get{return OpacityAt(0);}}
 }
}
