using System;using System.Drawing;using System.Drawing.Imaging;using System.IO;using System.Linq;using System.Reflection;using JouchoPet;
class RightCoatCheck {
 const string Folder="qa/v0815/";const BindingFlags F=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;static int checks;
 static void Check(bool pass,string label){if(!pass)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
 static object Field(object o,string name){return o.GetType().GetField(name,F).GetValue(o);}
 static Bitmap DrawOld(object r,RenderFrame f){var a=r.GetType().Assembly;var value=Activator.CreateInstance(a.GetType("JouchoPet.RenderFrame"));foreach(var field in typeof(RenderFrame).GetFields())if(field.FieldType.IsPrimitive||field.FieldType==typeof(string))value.GetType().GetField(field.Name).SetValue(value,field.GetValue(f));return (Bitmap)r.GetType().GetMethod("Draw").Invoke(r,new[]{value});}
 static object Old(Assembly a,CharacterForm form,int size){var r=Activator.CreateInstance(a.GetType("JouchoPet.PetRenderer"),F,null,new[]{Enum.ToObject(a.GetType("JouchoPet.CharacterForm"),(int)form)},null);r.GetType().GetMethod("Resize").Invoke(r,new object[]{size});return r;}
 [STAThread]static void Main(){try{
  Directory.CreateDirectory(Folder);Directory.CreateDirectory(Folder+"frames");AnimationParameters.UseFixedValues();var baseline=Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));
  foreach(var form in new[]{CharacterForm.AnemoneI,CharacterForm.AnemoneII})foreach(int size in new[]{220,300,400}){
   object old=Old(baseline,form,size);try{using(var now=new PetRenderer(form)){now.Resize(size);
    object oldArt=Field(Field(old,"side"),"art"),newArt=Field(Field(now,"side"),"art");
    var oldBodies=(int[][])Field(oldArt,"bodies");var newBodies=(int[][])Field(newArt,"bodies");var canvas=PetRenderer.CanvasSize(size);var pose=CharacterArt.Definition("profile-right");
    Check(oldBodies[0].SequenceEqual(newBodies[0]),"left walking body unchanged / "+form+" / "+size);
    var beforeLegs=(Array)Field(oldArt,"legPixels");var afterLegs=(Array)Field(newArt,"legPixels");for(int d=0;d<2;d++)for(int leg=0;leg<2;leg++)if(!((int[])beforeLegs.GetValue(d,leg)).SequenceEqual((int[])afterLegs.GetValue(d,leg)))throw new Exception("Leg texture changed");Check(true,"all gait leg/boot layers unchanged / "+form+" / "+size);
    int restored=0;using(var full=CharacterArt.Normalized("profile-right",canvas.Width,canvas.Height,size,form)){
     var expected=PoseMesh.Read(full);
     foreach(var p in new[]{new PointF(1610,320),new PointF(1620,325),new PointF(1630,321)}){
      int x=(int)Math.Round(canvas.Width*.5+(p.X-pose.Anchor)*CharacterArt.Scale(pose,size)),y=(int)Math.Round(CharacterArt.Y(pose,p.Y,size)),k=y*canvas.Width+x;
      Check(((uint)oldBodies[1][k]>>24)<20,"negative control: old cut removes coat / "+form+" / "+size+" / "+p);
      Check(((uint)newBodies[1][k]>>24)>230&&newBodies[1][k]==expected[k],"restored coat matches intact painting / "+form+" / "+size+" / "+p);restored++;
     }
    }
    Check(restored==3,"all missing-coat probes covered / "+form+" / "+size);
    for(int y=0;y<CharacterArt.Y(pose,307,size)-2;y++)for(int x=0;x<canvas.Width;x++){int k=y*canvas.Width+x;if(oldBodies[1][k]!=newBodies[1][k])throw new Exception("Right head/upper coat changed");}Check(true,"right head and upper coat unchanged / "+form+" / "+size);
    foreach(int facing in new[]{0,-1})foreach(double run in new[]{0.0,1.0})for(int i=0;i<=12;i++){
     var frame=new RenderFrame{MotionState=facing==0?"idle":"walk",Facing=facing,Time=.7,Gait=i/12.0,Speed=36,RunBlend=run};
     if(!PoseMesh.Read(DrawOld(old,frame)).SequenceEqual(PoseMesh.Read(now.Draw(frame))))throw new Exception("Unrelated front/left output changed");
    }Check(true,"front + left walk/run unchanged in 52 samples / "+form+" / "+size);
    foreach(double run in new[]{0.0,1.0})for(int i=0;i<=24;i++){
     var frame=new RenderFrame{MotionState="walk",Facing=1,Time=.7,Gait=i/24.0,Speed=36,RunBlend=run};var b=now.Draw(frame);var p=PoseMesh.Read(b);
     Check(p.Any(v=>((uint)v>>24)>200),"right "+(run==0?"walk":"run")+" phase "+i+" / "+form+" / "+size);
    }
   }}finally{((IDisposable)old).Dispose();}
  }
  object a=Old(baseline,CharacterForm.AnemoneI,300),b2=Old(baseline,CharacterForm.AnemoneII,300);
  try{using(var n1=new PetRenderer())using(var n2=new PetRenderer(CharacterForm.AnemoneII)){
   n1.Resize(300);n2.Resize(300);
   for(int i=0;i<24;i++)using(var board=new Bitmap(1280,490))using(var g=Graphics.FromImage(board)){
    g.Clear(Color.FromArgb(46,51,61));var frame=new RenderFrame{MotionState="walk",Facing=1,Gait=i/24.0,Speed=36,Settle=0};
    var images=new[]{DrawOld(a,frame),n1.Draw(frame),DrawOld(b2,frame),n2.Draw(frame)};
    for(int col=0;col<4;col++){
     g.DrawString(new[]{"A1 / before","A1 / fixed","A2 / before","A2 / fixed"}[col],SystemFonts.DefaultFont,Brushes.White,col*320+12,12);
     g.DrawImageUnscaled(images[col],col*320+160-images[col].Width/2,30);
     g.DrawImage(images[col],new Rectangle(col*320+80,380,160,96),new Rectangle(images[col].Width/2-18,215,40,24),GraphicsUnit.Pixel);
    }
    board.Save(Folder+"frames/right-"+i.ToString("00")+".png",ImageFormat.Png);if(i==6)board.Save(Folder+"right-coat-comparison.png",ImageFormat.Png);
   }
  }}finally{((IDisposable)a).Dispose();((IDisposable)b2).Dispose();}
  Console.WriteLine("PASS "+checks+" targeted right-coat checks. Offline only.");
 }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
}
