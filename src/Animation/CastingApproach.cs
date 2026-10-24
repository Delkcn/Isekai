using System;
namespace JouchoPet {
 // Horizontal approach only. Motion still owns speed, gait, turn and braking.
 sealed class CastingApproach {
  internal bool Moving,Braking;
  internal double Destination;
  internal int Direction,Facing;
  internal void Cancel(){Moving=Braking=false;}
  internal bool Plan(double x,double min,double max,double size,int preferred,Func<double,int,bool> usable){
   Cancel();if(max<min)return false;
   double step=Math.Max(4,size*.04),range=Math.Max(Math.Abs(x-min),Math.Abs(max-x));
   for(double distance=0;distance<=range+step;distance+=step)for(int side=0;side<2;side++){
    double candidate=Math.Max(min,Math.Min(max,x+(side==0?-distance:distance)));
    foreach(int facing in new[]{preferred,-preferred})if(usable(candidate,facing)){
     Destination=candidate;Facing=facing;Direction=candidate<x?-1:1;Moving=Math.Abs(candidate-x)>1;return true;
    }
   }
   return false;
  }
  internal static bool Stopped(Motion m){return m.Speed==0&&m.Settle>=1&&(m.State=="hold"||m.State=="walk"||m.State=="idle");}
  internal int Desired(Motion m,double x,bool ready){
   if(!ready||!Moving)return 0;
   double remaining=(Destination-x)*Direction;
   // Motion's brake decays at 9/s. Include the first unbraked tick (<=50ms)
   // and pixel rounding; the selected position has angular safety margin.
   double forward=Math.Max(0,m.Speed*Direction),stopDistance=forward/9+forward*.05+2;
   if(remaining<=stopDistance)Braking=true;
   if(Braking){if(Stopped(m))Moving=false;return 0;}
   return Direction;
  }
 }
}
