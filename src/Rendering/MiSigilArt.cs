using System;using System.Drawing;using System.Drawing.Drawing2D;
namespace JouchoPet {
 // Bounded per-frame drawing on the existing overlay, without a particle collection.
 static class MiSigilArt {
  internal static double Span(Size screen){return Math.Min(screen.Width,screen.Height)*.5;}
  internal static double ArmWidth(double size){return size*.0175;}
  static double Random(int seed,int key){unchecked{uint x=(uint)(seed+key*374761393);x=(x^(x>>13))*1274126177;x^=x>>16;return (x&0xffffff)/16777215.0;}}
  internal static PointF[] Ends(PointF center,double span,int line){double a=line*Math.PI/4;return new[]{RigParts.Add(center,-span*.5*Math.Cos(a),-span*.5*Math.Sin(a)),RigParts.Add(center,span*.5*Math.Cos(a),span*.5*Math.Sin(a))};}
  internal static double BreakAt(int ray,int piece,int seed){return 1.05+.22*Random(seed,ray*97+piece*17+31);}
  internal static double PieceOpacity(double t,int ray,int piece,int seed){return 1-DailyMotion.Ease((t-BreakAt(ray,piece,seed))/.19);}
  internal static double Burst(double t){return DailyMotion.Ease((t-.52)/.015)*(1-DailyMotion.Ease((t-.54)/.075));}
  internal static void Draw(Graphics g,PointF center,double span,double size,DailyPose pose,RectangleF face=default(RectangleF)){
   if(pose.Sigil<=0)return;
   var state=g.Save();g.SmoothingMode=SmoothingMode.AntiAlias;
   // Blades and arcs stay in front of hair. Only the brief center flash is face-protected.
   double t=pose.SigilTime,length=span*.5*pose.Glyph;float w=(float)ArmWidth(size);
   if(pose.SigilAlpha>0&&length>0){
    for(int ray=0;ray<8;ray++){
     if(pose.CrossOnly&&(ray&1)!=0)continue;
     var local=g.Save();g.TranslateTransform(center.X,center.Y);g.RotateTransform(ray*45);
     DrawRay(g,(float)length,w,t,ray,pose.SigilSeed,pose.SigilAlpha);
     EnergyArcs(g,(float)length,w,t,ray,pose.SigilSeed,pose.SigilAlpha);
     Shards(g,(float)(span*.5),w,t,ray,pose.SigilSeed);
     g.Restore(local);
    }
   }
   if(!face.IsEmpty)using(var mask=new GraphicsPath()){mask.AddEllipse(face);g.SetClip(mask,CombineMode.Exclude);}
   double charge=0;
   double flash=Burst(t);SoftLight(g,center,(float)(size*.048),Color.FromArgb((int)(155*flash),255,182,65));
   SoftLight(g,center,(float)(size*.021),Color.FromArgb((int)(255*Math.Max(charge,flash)),255,249,204));
   if(flash>0)using(var light=new SolidBrush(Color.FromArgb((int)(255*flash),255,255,232))){float r=(float)(size*.006*flash);g.FillRectangle(light,center.X-r,center.Y-r,2*r,2*r);}
   g.Restore(state);
  }
  static void SoftLight(Graphics g,PointF center,float radius,Color color){if(color.A==0)return;using(var path=new GraphicsPath()){
   path.AddEllipse(center.X-radius,center.Y-radius,2*radius,2*radius);using(var b=new PathGradientBrush(path)){b.CenterPoint=center;b.CenterColor=color;b.SurroundColors=new[]{Color.FromArgb(0,color.R,color.G,color.B)};g.FillPath(b,path);}
  }}
  internal static GraphicsPath Blade(float length,float width){var p=new GraphicsPath();p.AddPolygon(new[]{new PointF(0,0),new PointF(length*.035f,-width*.5f),new PointF(length*.87f,-width*.5f),new PointF(length,0),new PointF(length*.90f,width*.5f),new PointF(length*.035f,width*.5f)});return p;}
  static void DrawRay(Graphics g,float length,float w,double t,int ray,int seed,double opacity){
   using(var edge=Blade(length,w*2))using(var core=Blade(Math.Max(0,length-w*.65f),w)){
    if(t<=1.05){PaintBlade(g,edge,core,length,w,ray,seed,opacity);return;}
    // Jagged cuts open into gaps; unaffected pieces keep their full opacity.
    for(int piece=0;piece<9;piece++){
     double alpha=PieceOpacity(t,ray,piece,seed);if(alpha<=0)continue;
     double age=Math.Max(0,t-BreakAt(ray,piece,seed));
     var saved=g.Save();g.TranslateTransform((float)(age*w*4),(float)(age*w*8*(Random(seed,ray*71+piece*13)-.5)));
     float x0=length*piece/9,x1=length*(piece+1)/9,h=w*2;
     float a=(float)((Random(seed,ray*51+piece)-.5)*w*.9),b=(float)((Random(seed,ray*51+piece+1)-.5)*w*.9);
     using(var cut=new GraphicsPath()){
      cut.AddPolygon(new[]{new PointF(x0-a,-h),new PointF(x1-b,-h),new PointF(x1+b,0),new PointF(x1-b,h),new PointF(x0-a,h),new PointF(x0+a,0)});
      g.SetClip(cut,CombineMode.Intersect);PaintBlade(g,edge,core,length,w,ray,seed,opacity*alpha);
     }g.Restore(saved);
    }
   }
  }
  static void PaintBlade(Graphics g,GraphicsPath edge,GraphicsPath core,float length,float w,int ray,int seed,double opacity){
   using(var b=new SolidBrush(Color.FromArgb((int)(255*opacity),5,2,4)))g.FillPath(b,edge);
   // Matte, mildly uneven red planes: no center stripe or cylindrical specular highlight.
   using(var b=new LinearGradientBrush(new PointF(0,0),new PointF(Math.Max(1,length),0),Color.Black,Color.Black)){
    var colors=new Color[5];for(int i=0;i<5;i++){int r=108+(int)(27*Random(seed,ray*27+i+6));colors[i]=Color.FromArgb((int)(255*opacity),r,9+(i%2)*3,23+(int)(6*Random(seed,i+ray)));}
    b.InterpolationColors=new ColorBlend{Colors=colors,Positions=new[]{0f,.24f,.51f,.77f,1f}};g.FillPath(b,core);
   }
   var saved=g.Save();g.SetClip(core,CombineMode.Intersect);
   using(var shade=new SolidBrush(Color.FromArgb((int)(24*opacity),25,0,8)))g.FillPolygon(shade,new[]{new PointF(0,0),new PointF(length*.62f,-w*.25f),new PointF(length,0),new PointF(length*.65f,w*.5f),new PointF(length*.1f,w*.35f)});
   g.Restore(saved);
  }
  static void EnergyArcs(Graphics g,float length,float w,double t,int ray,int seed,double opacity){
   // New geometry for every finite pulse and every request, no looping path.
   int current=(int)Math.Floor((t-.54)/.19);
   for(int lane=0;lane<2;lane++)for(int epoch=Math.Max(0,current-1);epoch<=current;epoch++){
    int key=ray*997+lane*131+epoch*7919;double born=.54+epoch*.19+.045*Random(seed,key+1),life=.10+.065*Random(seed,key+2),age=t-born;
    if(age<0||age>=life||born>1.34)continue;
    double u=age/life,pulse=Math.Pow(Math.Sin(Math.PI*u),.65)*opacity;
    float start=length*(float)(.14+.64*Random(seed,key+3)),reach=length*(float)(.12+.17*Random(seed,key+4));
    reach=Math.Min(reach,length-start);if(reach<w)continue;
    var points=new PointF[6];
    for(int k=0;k<points.Length;k++){
     double drift=(Random(seed,key+40+k)-.5)*w*2.2*u;
     float y=(float)((Random(seed,key+10+k)-.5)*w*6+drift);if(k==0||k==5)y*=.22f;
     points[k]=new PointF(start+reach*k/5,y);
    }
    float width=w*(float)(.12+.16*Random(seed,key+5))*(float)(.65+.35*Math.Sin(Math.PI*u));Arc(g,points,width,pulse);
    if(lane==0&&Random(seed,key+6)>.45){var p=points[2];Arc(g,new[]{p,new PointF(p.X+reach*.16f,p.Y-w*(float)(1+Random(seed,key+7))),new PointF(p.X+reach*.30f,p.Y-w*(float)(2+Random(seed,key+8)))},width*.65f,pulse*.8);}
   }
  }
  static void Arc(Graphics g,PointF[] p,float width,double alpha){
   using(var halo=new Pen(Color.FromArgb((int)(35*alpha),255,170,32),width*4))using(var gold=new Pen(Color.FromArgb((int)(235*alpha),255,206,71),width))using(var white=new Pen(Color.FromArgb((int)(210*alpha),255,249,194),width*.42f)){
    halo.LineJoin=gold.LineJoin=white.LineJoin=LineJoin.Bevel;g.DrawLines(halo,p);g.DrawLines(gold,p);g.DrawLines(white,p);
   }
  }
  static void Shards(Graphics g,float length,float w,double t,int ray,int seed){
   // At most sixteen tiny chips; evaluated by age, not stored as a particle system.
   for(int k=0;k<2;k++){
    int piece=2+k*4,key=ray*193+k*101;double age=t-BreakAt(ray,piece,seed),life=.18+.07*Random(seed,key);
    if(age<=0||age>=life)continue;double q=age/life;
    float x=length*(piece+.5f)/9+(float)(w*2*q),y=(float)((Random(seed,key+1)>.5?1:-1)*w*(.4+2.8*q));
    float len=(float)(w*(.7+.7*Random(seed,key+2))*(1-q));int alpha=(int)(220*(1-q));
    using(var b=new SolidBrush(k==0?Color.FromArgb(alpha,118,13,25):Color.FromArgb(alpha,255,222,120)))g.FillPolygon(b,new[]{new PointF(x,y),new PointF(x+len,y-len*.3f),new PointF(x+len*.25f,y+len*.35f)});
   }
  }
 }
}
