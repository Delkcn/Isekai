using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using JouchoPet;
class RunCheck {
 static int checks;
 static void Check(bool yes,string name){if(!yes)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 static double Difference(Bitmap a,Bitmap b){int[] x=PoseMesh.Read(a),y=PoseMesh.Read(b);double sum=0;for(int i=0;i<x.Length;i++)for(int s=0;s<32;s+=8)sum+=Math.Abs(((uint)x[i]>>s&255)-(double)((uint)y[i]>>s&255));return sum/(x.Length*4);}
 static void Advance(Motion m,double seconds,int desired,int size,bool run){for(int i=0;i<(int)(seconds*120);i++)m.Update(1.0/120,desired,size,false,run);}
 static void Main(){try {
  foreach(int size in new[]{220,300,400})foreach(int direction in new[]{-1,1}) {
   string label=size+" / "+direction;
   var m=new Motion();bool handoff=false;
   for(int i=0;i<100;i++) {
    string before=m.State;double travel=m.Update(1.0/120,direction,size,false,true);
    if(before=="turn")CheckQuiet(travel*direction>=0,"run entry travels away from requested direction");
    if(before=="turn" && m.State=="walk"){handoff=true;Check(m.Settle==1 && m.Speed*direction>0 && m.RunBlend==1,"moving neutral-pose turn/run handoff "+label);break;}
   }
   Check(handoff,"run starts from front "+label);
   Advance(m,3,direction,size,true);
   Check(Math.Abs(m.Speed-direction*198.0*size/300)<.02 && m.RunBlend>.999,"run reaches intended pace "+label);
   double phase=m.Phase;
   Advance(m,2,direction,size,false);
   Check(Math.Abs(m.Speed-direction*36)<.1 && m.RunBlend==0 && m.Phase>phase,"run smoothly returns to exact ordinary walk mode "+label);
   Advance(m,2,direction,size,true);
   bool gathered=false,turned=false,coasted=false;
   for(int i=0;i<600;i++) {
    double travel=m.Update(1.0/120,-direction,size,false,true);
    CheckQuiet(m.State!="settle" && m.State!="brake","running reversal uses ordinary stop/settle");
    if(m.State=="rungather")gathered=true;
    if(m.State=="turn"){CheckQuiet(gathered && m.RunningTurn && m.RunBlend==0,"reverse skipped gathering or running turn");turned=true;if(Math.Abs(travel)>.001)coasted=true;}
   }
   Check(gathered && turned && coasted && m.Facing==-direction,"run gathers and coasts through reversal "+label);
   Advance(m,3,0,size,false);
   Check(m.State=="idle" && m.Speed==0 && m.RunBlend==0,"run stops and faces user "+label);
   Advance(m,3,direction,size,true);m.Boundary();Advance(m,2,-direction,size,true);
   Check(m.Facing==-direction && Math.Sign(m.Speed)==-direction,"screen edge permits run reversal "+label);
   m.FollowPush(2,12,direction,size);
   Check(m.RunBlend==0 && Math.Abs(m.Speed)==12,"push clears running pose "+label);
   m.Reset();Check(m.RunBlend==0 && m.State=="idle","reset clears run "+label);
   var walk=new Motion();Advance(walk,4,direction,size,false);
   Check(Math.Abs(walk.Speed-direction*36)<.01 && walk.RunBlend==0,"ordinary walking retains speed and pose "+label);
  }
  foreach(double blend in new[]{0.0,.25,.5,.75,1}) {
   double stance=RunGait.Contact(blend),stride=RunGait.Stride(300,blend),phase=.13,travel=2;
   var a=RunGait.Sample(phase,300,blend);var b=RunGait.Sample(phase+travel*stance/stride,300,blend);
   Check(a.Planted && b.Planted && Math.Abs(travel+b.X-a.X)<1e-8,"planted foot follows ground at blend "+blend);
   foreach(double seam in new[]{0.0,stance,1.0}) {
    a=RunGait.Sample(seam-1e-7,300,blend);b=RunGait.Sample(seam+1e-7,300,blend);
    CheckQuiet(Math.Abs(a.X-b.X)<.001 && Math.Abs(a.Lift-b.Lift)<.001,"foot seam jumps");
   }
   Check(true,"foot cycle is continuous at blend "+blend);
  }
  bool flight=false;for(int i=0;i<1000;i++){double p=i/1000.0;var a=RunGait.Sample(p,300,1);var b=RunGait.Sample(p+.5,300,1);if(!a.Planted&&!b.Planted && a.Lift>0 && b.Lift>0)flight=true;}
  Check(flight,"running includes genuine two-foot flight interval");
  foreach(int size in new[]{220,300,400})foreach(double blend in new[]{0.0,.25,.5,1}) {
   double stance=RunGait.Contact(blend);
   for(int i=0;i<120;i++) {
    double p=i/120.0,k=RunGait.KneeFlex(p,size,blend);
    CheckQuiet(k>=size*blend*.008-1e-8 && k<=size*blend*.013+1e-8,"knee bend exceeds subtle range");
    CheckQuiet(Math.Abs(k-RunGait.KneeFlex(p+1,size,blend))<1e-8,"knee cycle is not periodic");
   }
   foreach(double seam in new[]{0.0,stance,1.0})CheckQuiet(Math.Abs(RunGait.KneeFlex(seam-1e-7,size,blend)-RunGait.KneeFlex(seam+1e-7,size,blend))<1e-5,"knee pose jumps at contact seam");
   Check(true,"subtle knee flex blends and loops continuously "+size+" / "+blend);
  }
  Preview();Console.WriteLine("All "+checks+" run checks passed.");
 }catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static void CheckQuiet(bool yes,string label){if(!yes)throw new Exception(label);}
 static void Preview(){
  string folder=Path.Combine("qa","v0634","preview");Directory.CreateDirectory(folder);
  int size=300;Size c=PetRenderer.CanvasSize(size);
  using(var side=new SideWalk(c.Width,c.Height,size))using(var atlas=new Bitmap(c.Width*24,c.Height*2,PixelFormat.Format32bppPArgb))using(Graphics g=Graphics.FromImage(atlas)) {
   foreach(int direction in new[]{-1,1}) {
    using(Bitmap idle=(Bitmap)side.Draw(0,0,0,direction,1).Clone())
     Check(Difference(idle,side.Draw(0,0,0,direction,1,0,0,0,.2))==0,"render handoff retains neutral pixels "+direction);
    using(Bitmap push=(Bitmap)side.Draw(0,.3,1,direction,0,.7,.5).Clone())
     Check(Difference(push,side.Draw(0,.3,1,direction,0,.7,.5,0,1))==0,"carrying ignores run blend "+direction);
    using(Bitmap start=(Bitmap)side.Draw(0,0,1,direction,0,0,0,0,1).Clone()) {
     Check(Difference(start,side.Draw(0,1,1,direction,0,0,0,0,1))<.01,"run loop render continuity "+direction);
     Check(Difference(start,side.Draw(0,.25,1,direction,0,0,0,0,1))>.15,"run visibly changes pose "+direction);
    }
    for(int i=0;i<24;i++) {
     Bitmap frame=side.Draw(0,i/24.0,1,direction,0,0,0,0,1);
     int[] pixels=PoseMesh.Read(frame);int count=0;
     for(int y=0;y<c.Height;y++)for(int x=0;x<c.Width;x++)if(((uint)pixels[y*c.Width+x]>>24)>20){count++;CheckQuiet(x>1 && y>1 && x<c.Width-2 && y<c.Height-2,"run clipped at canvas edge");}
     CheckQuiet(count>10000,"run lost artwork");
     g.DrawImageUnscaled(frame,i*c.Width,direction<0?0:c.Height);
    }
   }
   atlas.Save(Path.Combine(folder,"run-atlas.png"),ImageFormat.Png);
   using(var sheet=new Bitmap(c.Width*6,c.Height*2))using(Graphics sg=Graphics.FromImage(sheet)) {
    sg.Clear(Color.FromArgb(238,235,230));
    for(int d=0;d<2;d++)for(int i=0;i<6;i++)sg.DrawImage(atlas,new Rectangle(i*c.Width,d*c.Height,c.Width,c.Height),new Rectangle(i*4*c.Width,d*c.Height,c.Width,c.Height),GraphicsUnit.Pixel);
    sheet.Save(Path.Combine(folder,"run-contact.png"),ImageFormat.Png);
   }
   Check(true,"48 run frames retain artwork and stay inside canvas");
  }
 }
}
