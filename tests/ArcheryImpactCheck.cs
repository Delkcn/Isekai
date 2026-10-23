using System;using System.IO;using System.Reflection;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using JouchoPet;
class ArcheryImpactCheck {
 static int count;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 static Bitmap Image(Action<Graphics> paint){var b=new Bitmap(1000,900,PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(b)){g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;paint(g);}return b;}
 static double Alpha(Bitmap b){double sum=0;foreach(uint p in Array.ConvertAll(PoseMesh.Read(b),v=>(uint)v))sum+=p>>24;return sum;}
 static MagicArrow Shot(int size,int facing){return new MagicArrow(new PointF(500,450),facing,new Rectangle(0,0,1000,900),size);}
 static void Main(){try{
  foreach(int size in new[]{220,300,400})foreach(int facing in new[]{-1,1}){
   var shot=Shot(size,facing);double distance=RigParts.Length(shot.Origin,shot.Impact);
   Check(Math.Abs(shot.FlightTime-distance/(size*6.4))<.000001,"flight is exactly twice previous speed / "+size+" / "+facing);
   foreach(int ring in new[]{0,1,2}){
    var before=Shot(size,facing);before.Update(before.FlightTime+ring*.1-.00001);var after=Shot(size,facing);after.Update(after.FlightTime+ring*.1+.00001);
    Check(before.OpacityAt(ring)==0&&after.OpacityAt(ring)>.99,"ring begins at impact plus "+ring*100+" ms / "+size+" / "+facing);
    after.Update(.65);Check(Math.Abs(after.RadiusAt(ring)-size*.75)<.001&&after.OpacityAt(ring)==0,"each ring reaches triple radius and fades / "+ring);
   }
   shot.Update(shot.FlightTime+.70);Check(!shot.Done&&shot.OpacityAt(0)==0&&shot.OpacityAt(2)>0,"last ring survives first ring completion");shot.Update(.150001);Check(shot.Done,"effect completes after final ring");
  }
  var old=Assembly.LoadFile(Path.GetFullPath("dist/IsekaijouchoPet-v0.6.45-preview/IsekaijouchoPet.exe"));var art=old.GetType("JouchoPet.ArcheryArt");var flags=BindingFlags.Static|BindingFlags.NonPublic;var spiral=art.GetMethod("Spiral",flags);
  foreach(bool back in new[]{false,true})using(var before=Image(g=>spiral.Invoke(null,new object[]{g,new PointF(800,350),ArcheryMotion.Axis(1),168.0,300.0,.123,back})))using(var after=Image(g=>ArcheryArt.Spiral(g,new PointF(800,350),ArcheryMotion.Axis(1),168,300,.123,back))){double ratio=MeanAlpha(after)/MeanAlpha(before);Console.WriteLine("Length-normalized spiral integrated brightness ratio "+ratio);Check(ratio>.8&&ratio<1.2,"shortened helix retains 0.6.45 per-pixel brightness / "+back);}
  var oldPose=old.GetType("JouchoPet.ArcheryMotion").GetMethod("At",flags).Invoke(null,new object[]{3.3,1});var particles=art.GetMethod("BowParticles",flags);
  using(var before=Image(g=>particles.Invoke(null,new object[]{g,new PointF(500,450),ArcheryMotion.Axis(1),300.0,oldPose})))using(var after=Image(g=>ArcheryArt.BowParticles(g,new PointF(500,450),ArcheryMotion.Axis(1),300,HeldPose()))){double ratio=Alpha(after)/Alpha(before);Console.WriteLine("Particle brightness ratio including saturated cores "+ratio);Check(ArcheryArt.GlowGain==2&&ratio>.98&&ratio<1.02,"particle glow remains identical to 0.6.45");}
  using(var sheet=new Bitmap(2100,1400))using(var g=Graphics.FromImage(sheet)){
   g.Clear(Color.FromArgb(35,39,50));g.SmoothingMode=SmoothingMode.AntiAlias;
   double[] ages={.05,.15,.25,.40,.60,.78};for(int i=0;i<ages.Length;i++){
    var shot=Shot(300,1);shot.Update(shot.FlightTime+ages[i]);var state=g.Save();g.TranslateTransform(i%3*700+350-shot.Impact.X,i/3*700+350-shot.Impact.Y);ArcheryArt.Impact(g,shot);g.Restore(state);g.DrawString("Impact + "+ages[i].ToString("F2")+" s",SystemFonts.DefaultFont,Brushes.White,i%3*700+25,i/3*700+25);
   }sheet.Save("qa/v0646/gold-impact.png",ImageFormat.Png);
  }
  var golden=Shot(300,1);golden.Update(golden.FlightTime+.25);using(var b=Image(g=>{g.TranslateTransform(500-golden.Impact.X,450-golden.Impact.Y);ArcheryArt.Impact(g,golden);})){int gold=0;foreach(uint pixel in Array.ConvertAll(PoseMesh.Read(b),v=>(uint)v)){int a=(int)(pixel>>24),r=(int)(pixel>>16&255),gr=(int)(pixel>>8&255),bl=(int)(pixel&255);if(a>50&&r>gr&&gr>bl*1.8)gold++;CheckPixel(a,r,gr,bl);}Check(gold>500,"three rings and stars render gold with valid premultiplied alpha");}
  Console.WriteLine("All "+count+" impact and brightness checks passed.");
 }catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static double MeanAlpha(Bitmap b){int n=0;double sum=0;foreach(uint p in Array.ConvertAll(PoseMesh.Read(b),v=>(uint)v))if(p>>24>8){sum+=p>>24;n++;}return sum/Math.Max(1,n);}
 static ArcheryPose HeldPose(){var p=ArcheryMotion.At(2.25,1);p.Age=3.3;return p;}
 static void CheckPixel(int a,int r,int g,int b){if(r>a||g>a||b>a)throw new Exception("invalid premultiplied color");}
}
