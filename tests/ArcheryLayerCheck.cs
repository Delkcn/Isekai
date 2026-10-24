using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using JouchoPet;
class ArcheryLayerCheck {
 static int count;
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);count++;Console.WriteLine("PASS "+label);}
 [STAThread]static void Main(){try{
  AnimationParameters.UseFixedValues();
  foreach(int size in new[]{220,300,400})using(var renderer=new PetRenderer())using(var overlay=new MagicOverlay()){
   renderer.Resize(size);int frames=0;double worst=0;Point origin=new Point(-30000,-30000);
   // Check every 60 Hz frame, including formation, release, follow-through and lowering.
   for(int i=0;i<=Math.Ceiling(ArcheryMotion.Duration*60);i++){
    double age=i/60.0;var pose=ArcheryMotion.At(age,-1);Bitmap character=renderer.Draw(new RenderFrame{Archery=true,BowPose=pose});MagicArrow arrow=null;
    if(age>=ArcheryMotion.ReleaseAt){var release=ArcheryMotion.At(ArcheryMotion.ReleaseAt,-1);var axis=ArcheryMotion.Axis(-1);var nock=ArcheryMotion.Nock(character.Width,size,release);arrow=new MagicArrow(RigParts.Add(nock,origin.X+axis.X*ArcheryMotion.ArrowLength(size),origin.Y+axis.Y*ArcheryMotion.ArrowLength(size)),-1,new Rectangle(-31000,-31000,3000,3000),size);arrow.Update(age-ArcheryMotion.ReleaseAt);}
    if(i%20==0)overlay.Hide();bool topmost=i%40<20;
    Bitmap composed=overlay.PaintEffect(origin,character.Width,size,pose,arrow,topmost,character);
    if(!ArcheryArt.HasBowEffect(pose)&&(arrow==null||arrow.Done)){if(composed!=character)throw new Exception("empty effect lost character");continue;}
    overlay.BringToFront(); // Force the previously failing z-order; overlap must still be impossible.
    Rectangle bounds=overlay.EffectBounds;Point offset=new Point(origin.X-bounds.Left,origin.Y-bounds.Top);int[] outside=PoseMesh.Read(overlay.DiagnosticCanvas);
    for(int y=0;y<character.Height;y++)for(int x=0;x<character.Width;x++)if(outside[(offset.Y+y)*bounds.Width+offset.X+x]!=0)throw new Exception("overlay covers character rectangle / "+age);
    using(var expected=new Bitmap(character.Width,character.Height,PixelFormat.Format32bppPArgb))using(var g=Graphics.FromImage(expected)){
     g.SmoothingMode=SmoothingMode.AntiAlias;ArcheryArt.Bow(g,Point.Empty,character.Width,size,pose);if(arrow!=null){g.TranslateTransform(-origin.X,-origin.Y);ArcheryArt.Projectile(g,arrow);g.ResetTransform();}g.DrawImageUnscaled(character,0,0);
     int[] a=PoseMesh.Read(expected),b=PoseMesh.Read(composed);double error=0;for(int p=0;p<a.Length;p++)for(int c=0;c<32;c+=8)error+=Math.Abs((int)((uint)a[p]>>c&255)-(int)((uint)b[p]>>c&255));error/=a.Length*4;worst=Math.Max(worst,error);if(error>.15)throw new Exception("split differs from single-frame composite / "+age+" error="+error);
    }frames++;
   }
   Check(true,"all "+frames+" left frames match single-frame composition, transparent overlay overlap, show/hide/topmost/forced front / "+size+" / max error="+worst);
  }
  foreach(int facing in new[]{-1,1}){
   var first=ArcheryMotion.At(ArcheryMotion.BowAppearAt+1.0/60,facing);Check(first.Bow==1&&first.Arrow>0,"arrow forms on first frame after complete bow / "+facing);
   var axis=ArcheryMotion.Axis(facing);var end=ArcheryArt.HelixPoint(PointF.Empty,axis,168,300,.1,1);Check(RigParts.Length(end,RigParts.Add(PointF.Empty,-axis.X*336,-axis.Y*336))<.001,"helix is two lengths, 2/3 of previous / "+facing);
  }
  Check(Math.Abs(ArcheryMotion.ReleaseAt-ArcheryMotion.FullDrawAt-.3)<1e-12,"full draw still holds 0.3 seconds");
  Console.WriteLine("All "+count+" composition and timing checks passed.");
 }catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
}
