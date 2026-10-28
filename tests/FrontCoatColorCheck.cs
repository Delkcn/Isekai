using System;using System.Drawing;using System.Drawing.Imaging;using System.IO;using System.Linq;using System.Reflection;using JouchoPet;
class FrontCoatColorCheck {
 const string Folder="qa/v0819/";const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;static int checks;
 static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 static void Same(int[] a,int[] b,int x0,int y0,int x1,int y1,string name){for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(a[y*380+x]!=b[y*380+x])throw new Exception(name+" / "+x+","+y);Check(true,name);}
 static double[] Patch(Bitmap b,int x,int y){double[] c=new double[3];for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++){var p=b.GetPixel(x+dx,y+dy);c[0]+=p.R/25.0;c[1]+=p.G/25.0;c[2]+=p.B/25.0;}return c;}
 static double ChromaDistance(double[] a,double[] b){return Math.Sqrt(Math.Pow(a[0]-a[1]-b[0]+b[1],2)+Math.Pow(a[0]-a[2]-b[0]+b[2],2));}
 static Bitmap DrawOld(object r,RenderFrame f){var a=r.GetType().Assembly;var converted=Activator.CreateInstance(a.GetType("JouchoPet.RenderFrame"));foreach(var field in typeof(RenderFrame).GetFields())if(field.FieldType.IsPrimitive||field.FieldType==typeof(string))converted.GetType().GetField(field.Name).SetValue(converted,field.GetValue(f));return (Bitmap)r.GetType().GetMethod("Draw").Invoke(r,new[]{converted});}
 static object OldRenderer(Assembly a,int form){var r=Activator.CreateInstance(a.GetType("JouchoPet.PetRenderer"),F,null,new[]{Enum.ToObject(a.GetType("JouchoPet.CharacterForm"),form)},null);r.GetType().GetMethod("Resize").Invoke(r,new object[]{300});return r;}
 [STAThread]static void Main(){try{
  var old=Assembly.LoadFile(Path.GetFullPath(Folder+"baseline/IsekaijouchoPet.exe"));
  using(var before=(Bitmap)old.GetType("JouchoPet.A2FrontArt").GetMethod("Load",F).Invoke(null,null))using(var now=A2FrontArt.Load())using(var side=CharacterArt.Load("left",CharacterForm.AnemoneII)){
   var a=PoseMesh.Read(before);var b=PoseMesh.Read(now);int changed=0;
   for(int k=0;k<a.Length;k++){if(((uint)a[k]>>24)!=((uint)b[k]>>24))throw new Exception("Opacity/silhouette changed");if(a[k]!=b[k])changed++;uint p=(uint)a[k];int r=(int)(p>>16&255),g=(int)(p>>8&255),bl=(int)(p&255);if((r-g>=24&&r-bl>=26)||(g-r>24&&bl-r>24))if(a[k]!=b[k])throw new Exception("Skin/hair/cyan pigment changed");}
   Check(changed>5000,"coat recolored with exact original alpha and protected saturated pigments");Console.WriteLine("changed pixels="+changed);
   Same(a,b,0,0,380,160,"head and flowers unchanged");Same(a,b,0,400,380,465,"feet and lower boots unchanged");Same(a,b,240,217,265,240,"hair outside long sleeve unchanged");Same(a,b,200,303,211,316,"white shorts unchanged");Same(a,b,165,340,178,450,"near boot unchanged");Same(a,b,202,327,218,450,"far boot unchanged");Same(a,b,125,266,140,280,"left glove unchanged");Same(a,b,241,285,246,292,"right hand unchanged");
   var refColor=Patch(side,161,247);foreach(var point in new[]{new Point(175,240),new Point(210,250),new Point(169,290)}){var from=Patch(before,point.X,point.Y);var to=Patch(now,point.X,point.Y);double previous=ChromaDistance(from,refColor),current=ChromaDistance(to,refColor);Console.WriteLine("patch "+point+" RGB "+String.Join(",",from.Select(v=>v.ToString("F1")))+" -> "+String.Join(",",to.Select(v=>v.ToString("F1")))+"; chroma distance "+previous.ToString("F2")+" -> "+current.ToString("F2"));Check(previous>12&&current<2,"front cloth chroma agrees with side reference / "+point);}
  }
  foreach(string path in new[]{"A2FrontGarment.cs","A2CoatLength.cs","A2SideCoatLength.cs","WalkArt.cs","SkeletalTurn.cs"})Check(File.ReadAllBytes("src/Rendering/"+path).SequenceEqual(File.ReadAllBytes(Folder+"baseline/src/Rendering/"+path)),"geometry and rig unchanged / "+path);
  foreach(var form in new[]{CharacterForm.AnemoneI,CharacterForm.AnemoneII}){object previous=OldRenderer(old,(int)form);try{using(var current=new PetRenderer(form)){
   current.Resize(300);for(int i=0;i<=32;i++){var f=new RenderFrame{MotionState="turn",Settle=1,Angle=-1+i/16.0};var a=PoseMesh.Read(DrawOld(previous,f));var b=PoseMesh.Read(current.Draw(f));for(int k=0;k<a.Length;k++){if(((uint)a[k]>>24)!=((uint)b[k]>>24))throw new Exception("Rendered silhouette changed");if((form==CharacterForm.AnemoneI||k<PetRenderer.CanvasSize(300).Width*118)&&a[k]!=b[k])throw new Exception("A1/head render changed");}}
   Check(true,"33 turn samples: identical alpha, protected head/A1 / "+form);
   foreach(int d in new[]{-1,1}){for(int i=0;i<16;i++){var f=new RenderFrame{MotionState="walk",Facing=d,Gait=i*Math.PI/8,Speed=80,Settle=0};if(!PoseMesh.Read(DrawOld(previous,f)).SequenceEqual(PoseMesh.Read(current.Draw(f))))throw new Exception("Side walking changed");}Check(true,"16 actual side walking frames identical / "+form+" / "+d);}
  }}finally{((IDisposable)previous).Dispose();}}
  object prior=OldRenderer(old,1);try{using(var current=new PetRenderer(CharacterForm.AnemoneII)){current.Resize(300);Directory.CreateDirectory(Folder+"turn-frames");
   using(var board=new Bitmap(1280,600))using(var g=Graphics.FromImage(board)){g.Clear(Color.FromArgb(47,52,61));for(int i=0;i<4;i++){var f=new RenderFrame{MotionState="turn",Settle=1,Angle=i==0?-.5:i==3?.5:0};var b=i==1?DrawOld(prior,f):current.Draw(f);g.DrawImageUnscaled(b,i*320+160-b.Width/2,32);g.DrawString(new[]{"A2 / left reference","A2 / front before","A2 / front matched","A2 / right reference"}[i],SystemFonts.DefaultFont,Brushes.White,i*320+10,10);g.DrawImage(b,new Rectangle(i*320+35,410,250,170),new Rectangle(b.Width/2-48,133,96,65),GraphicsUnit.Pixel);}board.Save(Folder+"color-comparison.png",ImageFormat.Png);}
   using(var board=new Bitmap(1600,400))using(var g=Graphics.FromImage(board)){g.Clear(Color.FromArgb(47,52,61));for(int i=0;i<5;i++){var b=current.Draw(new RenderFrame{MotionState="turn",Settle=1,Angle=-1+i*.5});g.DrawImageUnscaled(b,i*320+160-b.Width/2,32);g.DrawString(new[]{"profile-left","left","front","right","profile-right"}[i],SystemFonts.DefaultFont,Brushes.White,i*320+20,8);}board.Save(Folder+"current-five-views.png",ImageFormat.Png);}
   foreach(int d in new[]{-1,1})for(int i=0;i<24;i++)using(var board=new Bitmap(640,400))using(var g=Graphics.FromImage(board)){g.Clear(Color.FromArgb(47,52,61));var f=new RenderFrame{MotionState="turn",Settle=1,Angle=d*LeapMotion.Ease(i/23.0)};int col=0;foreach(var b in new[]{DrawOld(prior,f),current.Draw(f)}){g.DrawImageUnscaled(b,col*320+160-b.Width/2,32);g.DrawString(col==0?"0.8.18 / before":"0.8.19 / matched",SystemFonts.DefaultFont,Brushes.White,col*320+10,8);col++;}board.Save(Folder+"turn-frames/"+(d<0?"left":"right")+"-"+i.ToString("00")+".png",ImageFormat.Png);}
  }}finally{((IDisposable)prior).Dispose();}
  Console.WriteLine("PASS "+checks+" front coat color checks.");
 }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
}



