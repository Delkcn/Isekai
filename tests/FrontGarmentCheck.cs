using System;using System.Drawing;using System.Drawing.Imaging;using System.IO;using System.Linq;using System.Reflection;using JouchoPet;
class FrontGarmentCheck {
 const string Folder="qa/v0818/";const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;static int checks;
 static void Check(bool b,string s){if(!b)throw new Exception(s);checks++;Console.WriteLine("PASS "+s);}
 static void Same(int[] a,int[] b,int x0,int y0,int x1,int y1,string s){for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(a[y*380+x]!=b[y*380+x])throw new Exception(s+" / "+x+","+y);Check(true,s);}
 static int Skin(int[] p,int x){for(int y=295;y<340;y++){uint v=(uint)p[y*380+x];int r=(int)(v>>16&255),g=(int)(v>>8&255),b=(int)(v&255);if(r-g>22&&r-b>28)return y;}return -1;}
 static int PaintedHem(int[] source,int[] picture,int x){
  // Locate the painted shorts hem from its texture, not an arbitrary pink
  // pixel: the exposed waist above the shorts otherwise gives a false pass.
  for(int dy=0;dy<=40;dy++){bool same=true;for(int y=294;y<=302;y++)if(source[y*380+x]!=picture[(y+dy)*380+x]){same=false;break;}if(same)return Skin(source,x)+dy;}
  return -1;
 } static int Flap(int[] p,int x){for(int y=300;y<365;y++){uint v=(uint)p[y*380+x];if((v>>16&255)+(v>>8&255)+(v&255)<340)return y;}throw new Exception("Front flap boundary not found");}
 static Bitmap DrawOld(object renderer,RenderFrame frame){var assembly=renderer.GetType().Assembly;var f=Activator.CreateInstance(assembly.GetType("JouchoPet.RenderFrame"));foreach(var field in typeof(RenderFrame).GetFields())if(field.FieldType.IsPrimitive||field.FieldType==typeof(string))f.GetType().GetField(field.Name).SetValue(f,field.GetValue(frame));return (Bitmap)renderer.GetType().GetMethod("Draw").Invoke(renderer,new[]{f});}
 static void RenderChecks(Assembly old){
  foreach(int size in new[]{220,300,400})foreach(var form in new[]{CharacterForm.AnemoneI,CharacterForm.AnemoneII}){
   object prior=Activator.CreateInstance(old.GetType("JouchoPet.PetRenderer"),F,null,new[]{Enum.ToObject(old.GetType("JouchoPet.CharacterForm"),(int)form)},null);
   try{prior.GetType().GetMethod("Resize").Invoke(prior,new object[]{size});using(var now=new PetRenderer(form)){
    now.Resize(size);
    for(int i=0;i<=32;i++){
     var f=new RenderFrame{MotionState="turn",Angle=-1+i/16.0,Settle=1};var a=PoseMesh.Read(DrawOld(prior,f));var b=PoseMesh.Read(now.Draw(f));
     int count=form==CharacterForm.AnemoneI?a.Length:PetRenderer.CanvasSize(size).Width*(int)(28+size*.42);
     for(int k=0;k<count;k++)if(a[k]!=b[k])throw new Exception("A1 or A2 head changed during turn");
     if(!b.Any(v=>((uint)v>>24)>200))throw new Exception("Empty turn frame");
    }Check(true,"33 turn samples: A1 identical / A2 head unchanged, valid render / "+form+" / "+size);
   }}finally{((IDisposable)prior).Dispose();}
  }
  Directory.CreateDirectory(Folder+"turn-frames");object previous=Activator.CreateInstance(old.GetType("JouchoPet.PetRenderer"),F,null,new[]{Enum.ToObject(old.GetType("JouchoPet.CharacterForm"),1)},null);
  try{previous.GetType().GetMethod("Resize").Invoke(previous,new object[]{300});using(var a1=new PetRenderer())using(var a2=new PetRenderer(CharacterForm.AnemoneII)){
   a1.Resize(300);a2.Resize(300);
   foreach(int direction in new[]{-1,1})for(int i=0;i<56;i++)using(var image=new Bitmap(960,400))using(var g=Graphics.FromImage(image)){
    g.Clear(Color.FromArgb(46,51,61));var f=i<24?new RenderFrame{MotionState="turn",Angle=direction*LeapMotion.Ease(i/23.0),Settle=1}:new RenderFrame{MotionState="walk",Facing=direction,Time=(i-24)/30.0,Gait=(i-24)*Math.PI/15,Speed=80,Settle=Math.Max(0,1-(i-24)/8.0)};int col=0;
    foreach(var b in new[]{a1.Draw(f),DrawOld(previous,f),a2.Draw(f)}){g.DrawString(new[]{"A1","A2 / 0.8.17","A2 / revised"}[col],SystemFonts.DefaultFont,Brushes.White,col*320+12,12);g.DrawImageUnscaled(b,col*320+160-b.Width/2,40);col++;}
    if(i>=24&&!PoseMesh.Read(DrawOld(previous,f)).SequenceEqual(PoseMesh.Read(a2.Draw(f))))throw new Exception("Side walking changed / "+direction+" / "+i);image.Save(Folder+"turn-frames/"+(direction<0?"left":"right")+"-"+i.ToString("00")+".png",ImageFormat.Png);
   }
  }Check(true,"64 actual side-walk frames unchanged across both directions");}finally{((IDisposable)previous).Dispose();}
 }
 static double Tip(Bitmap image,string name){
  int x0=name=="front"?100:name=="profile-left"?85:name=="left"?55:name=="right"?115:145;
  var pose=CharacterArt.Definition(name);double scale=CharacterArt.Scale(pose,300);
  int xStart=(int)(image.Width*.5+(x0-pose.Anchor+pose.Cell.X)*scale),xEnd=(int)(xStart+30*scale);
  var pixels=PoseMesh.Read(image);int at=0,min=image.Width;
  for(int y=(int)CharacterArt.Y(pose,335,300);y<CharacterArt.Y(pose,390,300);y++)for(int x=xStart;x<xEnd;x++){
   uint p=(uint)pixels[y*image.Width+x];int r=(int)(p>>16&255),g=(int)(p>>8&255),b=(int)(p&255);
   if((p>>24)<200||r-g>22||r-b>22||r+g+b<270)continue;
   if(x<min){min=x;at=y;}
  }
  if(at==0)throw new Exception("No coat tip: "+name);
  double normalized=at;
  Console.WriteLine(name+" tip raw="+at+" normalized="+normalized.ToString("F2"));return normalized;
 }
 static int Shorts(Bitmap b,int dx){
  int x=b.Width/2+dx;for(int y=220;y<243;y++){var p=b.GetPixel(x,y);if(p.A>200&&p.R-p.G>22&&p.R-p.B>28)return y;}return -1;
 }
 static void CrossView(Assembly old){
  using(var current=new PetRenderer(CharacterForm.AnemoneII)){
   current.Resize(300);double[] angles={-1,-.5,0,.5,1};int[] xs={0,2,14,12,20};int min=999,max=0;double tipMin=999,tipMax=0;string[] names={"profile-left","left","front","right","profile-right"};
   for(int i=0;i<5;i++){
    var b=current.Draw(new RenderFrame{MotionState="turn",Angle=angles[i],Settle=1});int y=Shorts(b,xs[i]);
    Check(y>=0,"visible shorts/thigh boundary in actual renderer / "+angles[i]);
    double tip=Tip(b,names[i]);tipMin=Math.Min(tipMin,tip);tipMax=Math.Max(tipMax,tip);min=Math.Min(min,y);max=Math.Max(max,y);Console.WriteLine("actual shorts boundary angle="+angles[i]+" y="+y);
   }
   Check(tipMax-tipMin<=3,"actual five-view outer coat tips within 3 px at size 300");Check(max-min<=6,"actual five-view shorts height spread <= 6 px at size 300");
   foreach(int direction in new[]{-1,1}){
    var a=PoseMesh.Read(current.Draw(new RenderFrame{MotionState="turn",Angle=direction,Settle=1}));
    var b=PoseMesh.Read(current.Draw(new RenderFrame{MotionState="walk",Facing=direction,Settle=1}));
    Check(a.SequenceEqual(b),"turn-to-side-walk handoff is pixel identical / "+direction);
   }
  }
 } [STAThread]static void Main(){try{
  Directory.CreateDirectory(Folder);var old=Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));
  using(var before=(Bitmap)old.GetType("JouchoPet.A2FrontArt").GetMethod("Load",F).Invoke(null,null))using(var after=A2FrontArt.Load())using(var a1=CharacterArt.Load("front")){
   var a=PoseMesh.Read(before);var b=PoseMesh.Read(after);
   Same(a,b,0,0,380,236,"head, hair and upper torso unchanged");Same(a,b,0,365,380,465,"entire lower rear hem and feet unchanged");
   Same(a,b,120,235,150,302,"left hand and cuff unchanged");Same(a,b,240,235,265,301,"right hand and cuff unchanged");
   Same(a,b,165,365,178,450,"near boot core unchanged");Same(a,b,200,365,215,450,"far boot/lace core unchanged");
   var reference=PoseMesh.Read(a1);var originalAssembly=Assembly.LoadFile(Path.GetFullPath("qa/v0816/baseline/IsekaijouchoPet.exe"));int[] source;using(var raw=(Bitmap)originalAssembly.GetType("JouchoPet.A2FrontArt").GetMethod("Load",F).Invoke(null,null))source=PoseMesh.Read(raw);
   foreach(int x in new[]{200,205,210}){
    int target=Skin(reference,x),current=PaintedHem(source,b,x),previous=PaintedHem(source,a,x);
    Console.WriteLine("Shorts hem x="+x+": A1="+target+", 0.8.17="+previous+", revised="+current);
    Check(current>=0&&Math.Abs(current-target)<=2,"shorts/thigh boundary aligned to A1 / "+x);
    Check(previous<0||Math.Abs(previous-target)>12,"negative control detects 0.8.17 shorts overshoot / "+x);
   }
   foreach(int x in new[]{170,175}){
    int target=x==170?330:327,current=Flap(b,x),previous=Flap(a,x);
    Console.WriteLine("Inner front flap x="+x+": trace="+target+", 0.8.17="+previous+", revised="+current);
    Check(Math.Abs(current-target)<=2,"inner flap follows independently traced A1 diagonal / "+x);
    Check(previous-current>10,"negative control detects 0.8.17 flap overshoot / "+x);
   }
   using(var image=new Bitmap(1140,710))using(var g=Graphics.FromImage(image)){
    g.Clear(Color.FromArgb(46,51,61));int col=0;
    before.Tag=after.Tag;
    foreach(var raw in new[]{a1,before,after}){
     g.DrawString(new[]{"A1 / reference","A2 / 0.8.17","A2 / aligned garment"}[col],SystemFonts.DefaultFont,Brushes.White,col*380+15,15);
     using(var normalized=CharacterArt.Normalize(raw,347,356,300))g.DrawImageUnscaled(normalized,col*380+16,35);
     g.DrawImage(raw,new Rectangle(col*380+30,420,320,270),new Rectangle(135,265,110,93),GraphicsUnit.Pixel);col++;
    }image.Save(Folder+"front-garment-comparison.png",ImageFormat.Png);
   }
  }
  var form=Enum.ToObject(old.GetType("JouchoPet.CharacterForm"),1);var art=old.GetType("JouchoPet.CharacterArt");
  foreach(string name in new[]{"profile-left","left","right","profile-right"})using(var previous=(Bitmap)art.GetMethod("Normalized",F).Invoke(null,new object[]{name,347,356,300,form}))using(var current=CharacterArt.Normalized(name,347,356,300,CharacterForm.AnemoneII))Check(PoseMesh.Read(previous).SequenceEqual(PoseMesh.Read(current)),"entire side registration unchanged / "+name);
  Check(File.ReadAllBytes("src/Rendering/WalkArt.cs").SequenceEqual(File.ReadAllBytes(Folder+"baseline/src/Rendering/WalkArt.cs")),"0.8.15 right-walk cutout fix retained verbatim");
  CrossView(old);RenderChecks(old);
  Console.WriteLine("PASS "+checks+" front garment checks.");
 }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
}



