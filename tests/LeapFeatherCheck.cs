using System;using System.Drawing;using System.Drawing.Imaging;using System.IO;using System.Linq;using System.Reflection;using JouchoPet;
class LeapFeatherCheck {
 const string Root="qa/v0822/";const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;static int checks,samples;
 static void Check(bool yes,string text){if(!yes)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
 static object OldPose(Assembly a,LeapPose p){var t=a.GetType("JouchoPet.LeapPose");var o=Activator.CreateInstance(t);foreach(var f in typeof(LeapPose).GetFields())t.GetField(f.Name).SetValue(o,f.GetValue(p));return o;}
 static void Compare(){
  var old=Assembly.LoadFile(Path.GetFullPath(Root+"baseline/IsekaijouchoPet.exe"));var type=old.GetType("JouchoPet.SideWalk");
  foreach(int size in new[]{220,300,400})foreach(CharacterForm form in new[]{CharacterForm.AnemoneI,CharacterForm.AnemoneII}){
   Size c=PetRenderer.CanvasSize(size);int changes=0;using(var current=new SideWalk(c.Width,c.Height,size,form))using(var previous=(IDisposable)Activator.CreateInstance(type,F,null,new object[]{c.Width,c.Height,size,Enum.ToObject(old.GetType("JouchoPet.CharacterForm"),(int)form)},null)){
    foreach(int vertical in new[]{-1,1})foreach(int facing in new[]{-1,1})for(int i=0;i<12;i++){
     double duration=vertical<0?1.95:1.79;var pose=LeapMotion.At(duration*i/11,vertical);var prior=(Bitmap)type.GetMethod("DrawLeap",F).Invoke(previous,new object[]{facing,OldPose(old,pose),0.0,0.0,0.0});int[] a=PoseMesh.Read(prior),b=PoseMesh.Read(current.DrawLeap(facing,pose));
     for(int y=0;y<c.Height;y++)for(int x=0;x<c.Width;x++){int n=y*c.Width+x;if(a[n]!=b[n]){if(form==CharacterForm.AnemoneI||pose.Petals==0)throw new Exception("Unexpected A1 or no-effect change");if(y<28+size*.82||Math.Abs(x-c.Width*.5)>size*.25)throw new Exception("Changed pixels outside the foot effect region");changes++;}if((x==0||y==0||x==c.Width-1||y==c.Height-1)&&((uint)b[n]>>24)>12)throw new Exception("Clipped image");}samples++;
    }
    Check(form==CharacterForm.AnemoneI?changes==0:changes>0,"both verticals and facings: "+form+" / "+size+"; A1 unchanged, A2 changes confined to foot effects, endpoints intact");
   }
  }
 }
 static void Glow(){
  foreach(int size in new[]{220,300,400})using(var b=new Bitmap(size,size,PixelFormat.Format32bppPArgb))using(var g=Graphics.FromImage(b)){
   var pose=LeapMotion.At(.38,-1);LeapFeathers.Draw(g,size,size,size*.65,pose);var pixels=PoseMesh.Read(b);int glow=0,core=0;
   foreach(int p in pixels){Color c=Color.FromArgb(p);if(c.A>=12&&c.A<65)glow++;if(c.A>110&&c.R>110&&c.B>120)core++;}
   Check(glow>20&&core>20,"feathers have luminous cores and translucent halos / "+size);
   var first=pixels;g.Clear(Color.Transparent);pose.Time+=.1;LeapFeathers.Draw(g,size,size,size*.65,pose);Check(!first.SequenceEqual(PoseMesh.Read(b)),"feathers drift on existing action clock / "+size);
  }
 }
 static void Transition(){
  var initial=new PetRenderer(CharacterForm.AnemoneI);initial.Resize(300);using(var t=new FormTransition(initial,300)){
   var timer=System.Diagnostics.Stopwatch.StartNew();while(t.Loading){t.Advance(0);if(timer.Elapsed.TotalSeconds>30)throw new Exception("preload timeout");System.Threading.Thread.Sleep(2);}
   t.Request(CharacterForm.AnemoneII,0);t.Advance(.225);
   foreach(int v in new[]{-1,1})foreach(int facing in new[]{-1,1}){
    var f=new RenderFrame{Leaping=true,Facing=facing,Leap=LeapMotion.At(.45,v),MotionState="hold"};int[] a=PoseMesh.Read(t.Draw(r=>r.Draw(f)));f.Leap=LeapMotion.At(.55,v);int[] b=PoseMesh.Read(t.Draw(r=>r.Draw(f)));Check(t.Mix>.49&&t.Mix<.51&&!a.SequenceEqual(b),"live mid-leap fade continues / "+v+" / "+facing);
   }
  }
 }
 static void Preview(){
  Directory.CreateDirectory(Root+"frames");const int size=300;Size c=PetRenderer.CanvasSize(size);
  using(var a1=new SideWalk(c.Width,c.Height,size,CharacterForm.AnemoneI))using(var a2=new SideWalk(c.Width,c.Height,size,CharacterForm.AnemoneII))using(var sheet=new Bitmap(1200,720))using(var sg=Graphics.FromImage(sheet)){
   sg.Clear(Color.FromArgb(29,34,45));int row=0;
   foreach(int v in new[]{-1,1})foreach(int facing in new[]{-1,1}){
    string clip=(v<0?"up":"down")+(facing<0?"-left":"-right");double duration=v<0?1.95:1.79;
    for(int i=0;i<40;i++)using(var frame=new Bitmap(c.Width*2, c.Height+32))using(var g=Graphics.FromImage(frame)){
     g.Clear(Color.FromArgb(29,34,45));var pose=LeapMotion.At(duration*i/39,v);g.DrawImageUnscaled(a1.DrawLeap(facing,pose),0,24);g.DrawImageUnscaled(a2.DrawLeap(facing,pose),c.Width,24);g.DrawString("A1 / petals",SystemFonts.DefaultFont,Brushes.White,12,5);g.DrawString("A2 / luminous feathers",SystemFonts.DefaultFont,Brushes.White,c.Width+12,5);frame.Save(Root+"frames/"+clip+"-"+i.ToString("00")+".png");
     if(i==8||i==12||i==31){int col=i==8?0:i==12?1:2;sg.DrawImage(frame,new Rectangle(col*400,row*180,400,180));}
    }row++;
   }sheet.Save(Root+"leap-comparison.png");
  }
  using(var zoom=new Bitmap(900,260))using(var g=Graphics.FromImage(zoom)){g.Clear(Color.FromArgb(29,34,45));LeapFeathers.Draw(g,900,850,170,LeapMotion.At(.5,-1));zoom.Save(Root+"feather-detail.png");}
 }
 [STAThread]static void Main(){try{AnimationParameters.UseFixedValues();Compare();Glow();Transition();Preview();Console.WriteLine("PASS "+checks+" checks; "+samples+" baseline frame comparisons; 160 preview frames. No live desktop interaction.");}catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
}
