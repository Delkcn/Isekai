using System;
namespace JouchoPet {
 // Front garment import only. Each column stretches from inside the coat to
 // its painted front edge; the shorts follow the same foreground mapping.
 static class A2FrontGarment {
  static double Between(double x,double a,double b,double u,double v){return u+(v-u)*(x-a)/(b-a);}
  static double Hem(double x){
   if(x<140)return Between(x,110,140,363,344);
   if(x<160)return Between(x,140,160,344,327);
   if(x<180)return Between(x,160,180,327,308);
   if(x<187)return Between(x,180,187,308,303);
   if(x<218)return Between(x,187,218,303,307);
   if(x<240)return Between(x,218,240,307,337);
   return Between(x,240,264,337,360);
  }
  static double LowerHem(double x){
   // Trace the A1 front flap, independently of the shorts hem. Moving both
   // down by 35 pixels hid the thigh and broke correspondence with side views.
   if(x<140)return Between(x,110,140,363,348);
   if(x<160)return Between(x,140,160,348,337);
   if(x<180)return Between(x,160,180,337,327);
   if(x<187)return Between(x,180,187,327,314);
   if(x<218)return Between(x,187,218,314,318);
   if(x<240)return Between(x,218,240,318,343);
   return Between(x,240,264,343,360);
  }
  internal static void Apply(int[] pixels,int width,int height){
   if(width!=380||height!=465)throw new ArgumentException("Front garment registration requires canonical A2 dimensions.");
   var original=(int[])pixels.Clone();
   for(int x=111;x<265;x++){
    double hem=Hem(x),drop=LowerHem(x)-hem;
    double root=Math.Max(235,x<175?240+(168-x)*2.5:x>218?240+(x-218)*3.1:235);
    root=Math.Min(root,hem-12);
    double lead=Math.Min(hem-4,root+32);if(drop<.01)continue;
    double targetLead=lead+drop,targetHem=hem+drop;
    for(int y=(int)Math.Ceiling(root);y<=Math.Min(height-2,targetHem);y++){
     if(y<303&&(x<155||x>238))continue;
     double sy=y<targetLead?Between(y,root,targetLead,root,lead):y-drop;
     int row=(int)Math.Floor(sy),k=y*width+x;double f=sy-row;
     uint a=(uint)original[row*width+x],b=(uint)original[(row+1)*width+x],result=0;
     // The root lies below the hands and inside the painted coat at this X.
     // Do not fill outside the original character silhouette.
     if(((uint)original[k]>>24)<200||(a>>24)<200||(b>>24)<200)continue;
     for(int s=0;s<32;s+=8)result|=(uint)Math.Round(((a>>s)&255)*(1-f)+((b>>s)&255)*f)<<s;
     pixels[k]=(int)result;
    }
   }
  }
 }
}
