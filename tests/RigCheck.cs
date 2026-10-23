using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using JouchoPet;

class RigCheck {
 static int checks;
 static void Check(bool condition,string label){if(!condition)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
 static double Difference(Bitmap a,Bitmap b) {
  int[] x=PoseMesh.Read(a),y=PoseMesh.Read(b);double sum=0;
  for(int i=0;i<x.Length;i++)for(int s=0;s<32;s+=8)sum+=Math.Abs(((uint)x[i]>>s&255)- (double)((uint)y[i]>>s&255));
  return sum/(x.Length*4);
 }
 internal static string WalkHash(int size) {
  Size c=PetRenderer.CanvasSize(size);
  using(var side=new SideWalk(c.Width,c.Height,size))using(var sha=SHA256.Create()) {
   foreach(int facing in new[]{-1,1})foreach(double phase in new[]{0.0,.12,.35,.65,.7,.85,.999})foreach(double settle in new[]{0.0,.5,1.0})foreach(double carry in new[]{0.0,.65}) {
    int[] pixels=PoseMesh.Read(side.Draw(.4,phase,.8,facing,settle,carry));var data=new byte[pixels.Length*4];Buffer.BlockCopy(pixels,0,data,0,data.Length);sha.TransformBlock(data,0,data.Length,null,0);
   }
   sha.TransformFinalBlock(new byte[0],0,0);return BitConverter.ToString(sha.Hash).Replace("-","");
  }
 }
 static void Main() {
  try {
   SmileChecks();
   foreach(int size in new[]{220,300,400})Run(size);
   Console.WriteLine("All "+checks+" rig checks passed.");
  }catch(Exception e){Console.WriteLine(e.GetType().FullName+": "+e.Message);Console.WriteLine(e.StackTrace);Environment.ExitCode=1;}
 }
 static void SmileChecks() {
  foreach(int size in new[]{220,300,400}) {
   int width=PetRenderer.CanvasSize(size).Width;uint skin=0xffedc8bb;
   int changed=0,maxColumn=0;
   for(int x=0;x<width;x++) {
    int column=0;
    for(int y=0;y<size+48;y++) {
     CheckQuiet(SmileRig.Apply(skin,x,y,width,size,0)==skin,"neutral face must not change");
     uint p=SmileRig.Apply(skin,x,y,width,size,1);
     CheckQuiet((p>>24)==255,"smile must not alter face opacity");
     if(p!=skin){column++;changed++;CheckQuiet(y>28+size*.25 && y<28+size*.30,"smile escaped mouth region");}
    }
    maxColumn=Math.Max(maxColumn,column);
   }
   Check(changed>0 && maxColumn<=3,"closed lip line stays within face and never forms an open cavity "+size);
  }
 }
 static void CheckQuiet(bool condition,string label){if(!condition)throw new Exception(label);}
 static bool RoseRed(uint p) {
  double a=p>>24;if(a<200)return false;
  double r=((p>>16)&255)*255/a,g=((p>>8)&255)*255/a,b=(p&255)*255/a;
  return r>160 && r-g>30 && r-b>20 && b-g>3;
 }
 static void RightIrisPixels(Bitmap picture,Bitmap shut,int size,out int opened,out int closed,out int fixedFringe) {
  opened=closed=fixedFringe=0;int[] pixels=PoseMesh.Read(picture),other=PoseMesh.Read(shut);
  // The visible iris aperture in the v067 source, excluding reddish hair accents
  // above and to the left. Count the same fixed region before/after closure.
  var face=CharacterArt.Definition("front");double scale=CharacterArt.Scale(face,size);
  for(int y=(int)CharacterArt.Y(face,127,size);y<CharacterArt.Y(face,141,size);y++)
   for(int x=(int)(picture.Width*.5+(853-face.Anchor)*scale);x<picture.Width*.5+(877-face.Anchor)*scale;x++) {
    uint p=(uint)pixels[y*picture.Width+x],q=(uint)other[y*picture.Width+x];
    // Unchanged reddish hair in front of the eye is deliberately preserved.
    // Exclude that identical ink from both counts, not just the closed image.
    if(p==q){if(RoseRed(p))fixedFringe++;continue;}
    if(RoseRed(p))opened++;if(RoseRed(q))closed++;
   }
 }
 static void Run(int size) {
  Size c=PetRenderer.CanvasSize(size);var sprites=SpriteAssets.LoadFront();
  try {
   using(var front=new SoftRig(sprites,c.Width,c.Height,size))using(var side=new SideWalk(c.Width,c.Height,size))using(var turn=new TurnRig(c.Width,c.Height,size)) {
    using(Bitmap open=(Bitmap)front.Draw(0,0,0,0,0,0,0,new[]{1.0,0,0}).Clone())
    using(Bitmap shut=(Bitmap)front.Draw(0,0,0,0,0,0,0,new[]{1.0,0,0},0,0,1).Clone()) {
     int opened,closed,fixedFringe;RightIrisPixels(open,shut,size,out opened,out closed,out fixedFringe);
     using(var eyes=new Bitmap(800,300))using(Graphics eg=Graphics.FromImage(eyes)) {
      eg.Clear(Color.FromArgb(235,235,235));
      var crop=new RectangleF((float)(c.Width*.5-size*.15),(float)(28+size*.12),(float)(size*.30),(float)(size*.225));
      eg.DrawImage(open,new RectangleF(0,0,400,300),crop,GraphicsUnit.Pixel);
      eg.DrawImage(shut,new RectangleF(400,0,400,300),crop,GraphicsUnit.Pixel);
      eyes.Save("eyes-"+size+".png",ImageFormat.Png);
     }
     Console.WriteLine("Changing iris pixels open="+opened+", shut="+closed+", fixed fringe="+fixedFringe+" at "+size);
     // Two pixels allow the red-brown fringe edge and bilinear rounding at 220px.
     Check(opened>2 && closed<=Math.Ceiling(opened*.25)+2,"right iris closes at corrected face position "+size);
     var covered=new CoveredEyeRig(PoseMesh.Read(open),c.Width,c.Height,size);
     foreach(double amount in new[]{0.0,.25,.5,.75,1})
      for(double x=c.Width*.5-size*.12;x<c.Width*.5;x+=.5) {
       double last=-1;
       for(double y=28+size*.15;y<28+size*.34;y+=.25) {
        double sy=covered.SourceY(x,y,amount);
        CheckQuiet(sy>=last,"covered-eye map must not fold the face");
        if(amount==0 || y<28+size*.19)CheckQuiet(sy==y,"covered-eye map must preserve neutral face and upper fringe");
        last=sy;
       }
      }
     Check(true,"covered-eye map stays monotone and leaves upper fringe fixed "+size);
    }
    Bitmap neutral=front.Draw(0,0,0,0,0,0,0,new[]{1.0,0,0});turn.Prepare(neutral,side);
    Check(turn.MaterialCount==5,"five directional materials at "+size);
    foreach(int direction in new[]{-1,1}) {
     using(Bitmap exact=(Bitmap)turn.Draw(direction,neutral,side,0).Clone()) {
      Check(Difference(exact,side.Draw(0,0,0,direction,1))==0,"turn/walk endpoint pixel identity "+size+" / "+direction);
      double error=Difference(exact,turn.Draw(direction*(1-1e-6),neutral,side,0));
      Console.WriteLine("Near endpoint mean channel error: "+error.ToString("F5"));
      Check(error<.35,"approach converges to walk endpoint "+size+" / "+direction);
     }
    }
    Check(Difference(neutral,turn.Draw(0,neutral,side,0))==0,"front endpoint identity "+size);
    var timer=Stopwatch.StartNew();double minimum=double.MaxValue;
    for(int i=0;i<=160;i++){turn.Draw(-1+i/80.0,neutral,side,0);minimum=Math.Min(minimum,turn.LastMinimumJacobian);}
    timer.Stop();Check(minimum>0,"161 angles have positive triangles "+size);
    Console.WriteLine("Turn average render ms at "+size+": "+(timer.Elapsed.TotalMilliseconds/161).ToString("F2"));
    foreach(double seam in new[]{-.75,-.25,.25,.75})using(Bitmap before=(Bitmap)turn.Draw(seam-1e-6,neutral,side,0).Clone()) {
     double difference=Difference(before,turn.Draw(seam+1e-6,neutral,side,0));
     Console.WriteLine("Material seam "+seam+" mean channel difference: "+difference.ToString("F3"));
     Check(difference<.25,"no full material jump at "+seam+" / "+size);
    }
    using(var skin=new Bitmap(c.Width,c.Height,PixelFormat.Format32bppPArgb))using(Graphics g=Graphics.FromImage(skin)) {
     g.Clear(Color.FromArgb(240,240,240));g.DrawImageUnscaled(turn.Draw(.35,neutral,side,0),0,0);turn.DrawSkeleton(g,.35);skin.Save("skeleton-"+size+".png",ImageFormat.Png);
    }
   }
   using(var renderer=new PetRenderer()) {
    renderer.Resize(size);
    foreach(int direction in new[]{-1,1}) {
     var motion=new Motion();bool crossed=false;
     for(int i=0;i<120;i++) {
      string previous=motion.State;motion.Update(1.0/60,direction,size);
      if(previous=="turn" && motion.State=="walk") {
       using(Bitmap a=(Bitmap)renderer.Draw(new RenderFrame{MotionState="turn",Angle=motion.Angle}).Clone())
       using(Bitmap b=(Bitmap)renderer.Draw(new RenderFrame{MotionState=motion.State,Facing=motion.Facing,Speed=motion.Speed,Gait=motion.Phase,Settle=motion.Settle}).Clone())
        Check(Difference(a,b)==0,"state-machine handoff identity "+size+" / "+direction);
       crossed=true;break;
      }
     }
     Check(crossed,"state-machine handoff reached "+size+" / "+direction);
    }
    using(var contact=new Bitmap(6*180,180))using(Graphics g=Graphics.FromImage(contact)) {
     g.Clear(Color.FromArgb(240,240,240));
     for(int i=0;i<6;i++) {
      var f=new RenderFrame{MotionState="idle",Time=i*.2,Affection=i==0?0:1,ClosedEyes=i==0?0:1,PatSide=i<3?-1:1,LookX=(i%3-1)*.8,Nod=.4};
      Bitmap posed=renderer.Draw(f);g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
      g.DrawImage(posed,new Rectangle(i*180,0,180,180),new Rectangle((int)(c.Width*.5-size*.15),(int)(28+size*.16),(int)(size*.30),(int)(size*.30)),GraphicsUnit.Pixel);
     }
     contact.Save("smile-"+size+".png",ImageFormat.Png);
    }
   }
   // v064 deliberately replaces walking artwork. The historical pixel hashes
   // remain on disk; numeric gait/contact regression coverage is in SelfTest.
   Console.WriteLine("New artwork render hash at "+size+": "+WalkHash(size));
  }finally{foreach(Bitmap sprite in sprites)sprite.Dispose();}
 }
}
