using System;
using System.Drawing;
namespace JouchoPet {
 // Forward kinematics; painted correctives supply projection and occlusion.
 sealed class RestSkeleton {
  internal sealed class Vertex {internal PointF Bind;internal double[] Weights;}
  static readonly int[] Parent={-1,0,1,2,2,1,5,1,7,0,9,0,11};
  static readonly PointF[] Pivot={new PointF(0,.63f),new PointF(0,.40f),new PointF(0,.30f),new PointF(-.22f,.32f),new PointF(.22f,.32f),new PointF(-.09f,.36f),new PointF(-.11f,.51f),new PointF(.09f,.36f),new PointF(.11f,.51f),new PointF(-.05f,.65f),new PointF(-.05f,.80f),new PointF(.05f,.65f),new PointF(.05f,.80f)};
  static double Smooth(double a,double b,double x){x=Math.Max(0,Math.Min(1,(x-a)/(b-a)));return x*x*(3-2*x);}
  internal Vertex Bind(PointF p,int width,double size) {
   double x=(p.X-width*.5)/size,y=(p.Y-28)/size,side=Math.Abs(x);
   var weights=new double[Parent.Length];double head=1-Smooth(.28,.37,y),hair=Smooth(.15,.27,side)*Smooth(.18,.34,y);
   double arms=Smooth(.075,.11,side)*(1-Smooth(.14,.21,side))*Smooth(.34,.44,y)*(1-Smooth(.60,.68,y))*(1-hair)*(1-head);
   weights[2]=head*(1-hair);weights[x<0?3:4]=hair;double lower=Smooth(.48,.55,y);
   weights[x<0?5:7]=arms*(1-lower);weights[x<0?6:8]=arms*lower;
   double legs=Smooth(.65,.75,y)*(1-hair),knee=Smooth(.77,.85,y);weights[x<0?9:11]=legs*(1-knee);weights[x<0?10:12]=legs*knee;
   double sum=0;foreach(double w in weights)sum+=w;if(sum>1)for(int i=0;i<weights.Length;i++)weights[i]/=sum;else weights[1]=1-sum;
   return new Vertex{Bind=new PointF((float)x,(float)y),Weights=weights};
  }
  static double Angle(int bone,double t) {
   double bend=Math.Sin(Math.PI*t);
   switch(bone){case 1:return -.055*bend;case 2:return -.12*t+.055*bend;case 3:case 4:return .08*t;
    case 5:return -.10*t;case 6:return -.22*t;case 7:return .08*t;case 8:return .25*t;
    case 9:return -.12*bend;case 11:return .12*bend;case 10:return .18*bend;case 12:return -.18*bend;default:return 0;}
  }
  static PointF Transform(PointF p,int bone,double t) {
   if(bone==0)return new PointF((float)(p.X-.035*t),(float)(p.Y+.29*t));
   PointF o=Pivot[bone];double angle=Angle(bone,t),x=p.X-o.X,y=p.Y-o.Y;
   return Transform(new PointF((float)(o.X+x*Math.Cos(angle)-y*Math.Sin(angle)),(float)(o.Y+x*Math.Sin(angle)+y*Math.Cos(angle))),Parent[bone],t);
  }
  internal PointF Skin(Vertex v,double t,int width,double size) {
   double x=0,y=0;for(int i=0;i<Parent.Length;i++)if(v.Weights[i]>0){PointF p=Transform(v.Bind,i,t);x+=p.X*v.Weights[i];y+=p.Y*v.Weights[i];}
   return new PointF((float)(width*.5+x*size),(float)(28+y*size));
  }
 }
}
