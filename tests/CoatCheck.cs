using System;using System.Drawing;using System.Drawing.Imaging;using System.IO;using System.Linq;using System.Reflection;using JouchoPet;
class CoatCheck {
 const string Folder="qa/v0814/"; const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static int checks;
 static void Check(bool b,string s){if(!b)throw new Exception(s);checks++;Console.WriteLine("PASS "+s);}
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
 static Bitmap DrawOld(object renderer,RenderFrame f){var a=renderer.GetType().Assembly;var converted=Activator.CreateInstance(a.GetType("JouchoPet.RenderFrame"));foreach(var field in typeof(RenderFrame).GetFields())if(field.FieldType.IsPrimitive||field.FieldType==typeof(string))converted.GetType().GetField(field.Name).SetValue(converted,field.GetValue(f));return (Bitmap)renderer.GetType().GetMethod("Draw").Invoke(renderer,new[]{converted});}
 [STAThread]static void Main(){try{
  Directory.CreateDirectory(Folder);AnimationParameters.UseFixedValues();var old=Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));var form=Enum.ToObject(old.GetType("JouchoPet.CharacterForm"),1);var art=old.GetType("JouchoPet.CharacterArt");
  string[] views={"profile-left","left","front","right","profile-right"};double min=999,max=0,oldMin=999,oldMax=0;
  foreach(string view in views)using(var before=view=="front"?(Bitmap)old.GetType("JouchoPet.A2FrontArt").GetMethod("Load",F).Invoke(null,null):(Bitmap)art.GetMethod("Load",F).Invoke(null,new[]{(object)view,form}))using(var after=view=="front"?A2FrontArt.Load():CharacterArt.Load(view,CharacterForm.AnemoneII)){
   before.Tag=view=="front"?after.Tag:CharacterArt.Definition(view);
   using(var oldNormalized=CharacterArt.Normalize(before,347,356,300))using(var newNormalized=CharacterArt.Normalize(after,347,356,300)){
   var a=PoseMesh.Read(oldNormalized);var b=PoseMesh.Read(newNormalized);int changed=0;
   for(int k=0;k<a.Length;k++)if(a[k]!=b[k]){changed++;int y=k/newNormalized.Width;if(view=="front"||y<CharacterArt.Y(CharacterArt.Definition(view),310,300)-1||y>CharacterArt.Y(CharacterArt.Definition(view),410,300)+1)throw new Exception("Unexpected edit outside side lower coat / "+view+" / "+y);}
   Check(view=="front"?changed==0:changed>0,view=="front"?"entire approved front unchanged":"only lower side coat changes / "+view);
   double previous=Tip(oldNormalized,view),current=Tip(newNormalized,view);min=Math.Min(min,current);max=Math.Max(max,current);oldMin=Math.Min(oldMin,previous);oldMax=Math.Max(oldMax,previous);
   }
  }
  Console.WriteLine("Front/left/right tip height spread: "+(oldMax-oldMin).ToString("F2")+" -> "+(max-min).ToString("F2")+" px at size 300");
  Check(oldMax-oldMin>12,"negative control detects old left/front/right mismatch");
  Check(max-min<=3,"five-view coat tips within 3 pixels at size 300");
  object prior=Activator.CreateInstance(old.GetType("JouchoPet.PetRenderer"),F,null,new[]{form},null);prior.GetType().GetMethod("Resize").Invoke(prior,new object[]{300});
  try{using(var now=new PetRenderer(CharacterForm.AnemoneII))using(var a1=new PetRenderer()){
   now.Resize(300);a1.Resize(300);
   Func<object,Array> legs=renderer=>{var side=renderer.GetType().GetField("side",F).GetValue(renderer);var walk=side.GetType().GetField("art",F).GetValue(side);return (Array)walk.GetType().GetField("legPixels",F).GetValue(walk);};
   var previousLegs=legs(prior);var currentLegs=legs(now);
   for(int d=0;d<2;d++)for(int leg=0;leg<2;leg++)Check(((int[])previousLegs.GetValue(d,leg)).SequenceEqual((int[])currentLegs.GetValue(d,leg)),"complete gait leg/boot layer identical / "+d+" / "+leg);
   using(var board=new Bitmap(1500,1170))using(var g=Graphics.FromImage(board)){
    g.Clear(Color.FromArgb(46,51,61));
    for(int row=0;row<3;row++)for(int i=0;i<5;i++){
     var frame=new RenderFrame{MotionState="turn",Angle=(i-2)*.5,Settle=1};var image=row==0?a1.Draw(frame):row==1?DrawOld(prior,frame):now.Draw(frame);
     g.DrawImageUnscaled(image,i*300+150-image.Width/2,row*390+34);g.DrawString((row==0?"A1":row==1?"A2 / 0.8.13":"A2 / corrected")+" / "+views[i],SystemFonts.DefaultFont,Brushes.White,i*300+10,row*390+8);
     using(var pen=new Pen(Color.FromArgb(100,145,222,235)))foreach(int y in new[]{255,280,322})g.DrawLine(pen,i*300+10,row*390+34+y,i*300+290,row*390+34+y);
    }board.Save(Folder+"coat-five-views.png",ImageFormat.Png);
    using(var compact=board.Clone(new Rectangle(0,780,1500,390),PixelFormat.Format32bppArgb))compact.Save(Folder+"coat-current.png",ImageFormat.Png);
   }
   // Normal turn endpoints must use exactly the same side walk texture at rest.
   foreach(int direction in new[]{-1,1}){
    var turn=PoseMesh.Read(now.Draw(new RenderFrame{MotionState="turn",Angle=direction,Settle=1}));
    var walk=PoseMesh.Read(now.Draw(new RenderFrame{MotionState="walk",Facing=direction,Settle=1,Speed=0}));
    Check(turn.SequenceEqual(walk),"turn/side-walk endpoint identical / "+direction);
   }
  }}finally{((IDisposable)prior).Dispose();}
  Console.WriteLine("PASS "+checks+" cross-view coat checks.");
 }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
}
