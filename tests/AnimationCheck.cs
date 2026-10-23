using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using JouchoPet;
class AnimationCheck {
 static void Main() { try { Run(); } catch(Exception e) { Console.WriteLine(e.GetType().FullName);Console.WriteLine(e.Message);Console.WriteLine(e.StackTrace);Environment.ExitCode=1; } }
 static void Run() {
  const int size=300;Size canvas=PetRenderer.CanvasSize(size);int w=canvas.Width,h=canvas.Height;
  Bitmap[] sprites=SpriteAssets.LoadFront();
  try {
   using(SoftRig front=new SoftRig(sprites,w,h,size))
   using(SideWalk side=new SideWalk(w,h,size))
   using(TurnRig turn=new TurnRig(w,h,size))
   using(Bitmap sheet=new Bitmap(9*144,9*174)) {
    Bitmap neutral=front.Draw(0,0,0,0,0,0,0,new double[]{1,0,0});turn.Prepare(neutral,side);
    using(var resting=new RestRig(sprites,w,h,size,neutral))ExportPreview(TurnSnapshots(turn,neutral,side,w,h,size),resting.DiagnosticKeys,w,h);
    double minimum=1;
    using(Graphics g=Graphics.FromImage(sheet)) {
     g.Clear(Color.FromArgb(235,235,235));
     for(int i=0;i<=80;i++) {
      Bitmap frame=turn.Draw(-1+i/40.0,neutral,side,0);
      minimum=Math.Min(minimum,turn.LastMinimumJacobian);
      if(turn.LastMinimumJacobian<=0)throw new Exception("Folded mesh at frame "+i);
      g.DrawImage(frame,new Rectangle((i%9)*144,(i/9)*174,144,174));
     }
    }
    sheet.Save("Animation-check.png",ImageFormat.Png);
    File.WriteAllText("Animation-check.txt","81 angles rendered using the actual C# renderer. Minimum registered triangle area (px^2): "+minimum+Environment.NewLine+"Positive area rules out reversed row ordering; it does not prove natural anatomy or monitor frame rate.");
    Console.WriteLine("Saved Animation-check.png / Animation-check.txt. Minimum triangle area: "+minimum);
   }
  }finally {foreach(Bitmap b in sprites)if(b!=null)b.Dispose();}
  RenderRest();
  RenderPush();
 }
 static PoseMesh.Pose[] TurnSnapshots(TurnRig turn,Bitmap neutral,SideWalk side,int w,int h,int size) {
  var keys=new PoseMesh.Pose[81];
  for(int i=0;i<keys.Length;i++)keys[i]=DrawnFrames.Register(turn.Draw(-1+i/40.0,neutral,side,0),size,-1);
  return keys;
 }
 static void RenderRest() {
  using(var renderer=new PetRenderer())
  using(var sheet=new Bitmap(12*144,5*194)) {
   renderer.Resize(300);
   var motion=new RestMotion();
   using(Graphics g=Graphics.FromImage(sheet)) {
    g.Clear(Color.FromArgb(235,235,235));
    string[] labels={"neutral -> sleep","sleep -> wake","neutral -> pat -> neutral","sleep -> pat -> neutral","repeated pat / sleep reversal"};
    double time=0;
    for(int row=0;row<5;row++) {
     if(row==0 || row==3) { motion.RequestSleep(true); }
     if(row==1)motion.RequestSleep(false);
     if(row==2 || row==4)motion.RequestPat(1);
     if(row==3)for(int i=0;i<240;i++){motion.Update(1.0/60,true);time+=1.0/60;}
     if(row==3)motion.RequestPat(-1);
     for(int column=0;column<12;column++) {
      if(row==4 && column==2)motion.RequestPat(-1);
      if(row==4 && column==4)motion.RequestSleep(true);
      if(row==4 && column==6)motion.RequestSleep(false);
      for(int i=0;i<30;i++){motion.Update(1.0/60,true);time+=1.0/60;}
      Bitmap frame=renderer.Draw(new RenderFrame {Time=time,MotionState="idle",RestAmount=motion.Sleep,
       Affection=motion.Affection,ClosedEyes=motion.Eyes,Nod=motion.Nod,PatSide=motion.Side});
      if(renderer.LastRestJacobian<=0)throw new Exception("Folded rest mesh at row "+row+", column "+column);
      g.DrawImage(frame,new Rectangle(column*144,row*194+20,144,174));
     }
     g.DrawString(labels[row],SystemFonts.DefaultFont,Brushes.Black,4,row*194+3);
    }
   }
   sheet.Save("Rest-transitions-check.png",ImageFormat.Png);
   Console.WriteLine("Saved Rest-transitions-check.png (five transition sequences, 0.5s samples).");
  }
 }

 static object ExportKeys(PoseMesh.Pose[] keys,int w,int h) {
  var list=new System.Collections.Generic.List<object>();
  foreach(var key in keys) {
   var points=new System.Collections.Generic.List<object>();
   foreach(PointF p in key.Points)points.Add(new double[]{p.X,p.Y});
   using(var picture=new Bitmap(w,h,PixelFormat.Format32bppPArgb))using(var bytes=new MemoryStream()) {
    var bits=picture.LockBits(new Rectangle(0,0,w,h),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
    try{for(int y=0;y<h;y++)System.Runtime.InteropServices.Marshal.Copy(key.Pixels,y*w,IntPtr.Add(bits.Scan0,y*bits.Stride),w);}finally{picture.UnlockBits(bits);}
    picture.Save(bytes,ImageFormat.Png);
    list.Add(new System.Collections.Generic.Dictionary<string,object>{{"points",points},{"image","data:image/png;base64,"+Convert.ToBase64String(bytes.ToArray())}});
   }
  }
  return list;
 }
 static void ExportPreview(PoseMesh.Pose[] turning,PoseMesh.Pose[] resting,int w,int h) {
  var data=new System.Collections.Generic.Dictionary<string,object>{{"turn",ExportKeys(turning,w,h)},{"rest",ExportKeys(resting,w,h)}};
  string template=File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"tools","preview-template.html"));
  // Use a serializer with enough capacity for embedded PNGs; no network needed.
  var serializer=new System.Web.Script.Serialization.JavaScriptSerializer();serializer.MaxJsonLength=int.MaxValue;
  File.WriteAllText("Animation-Preview.html",template.Replace("/*FRAME_DATA*/",serializer.Serialize(data)));
  Console.WriteLine("Updated Animation-Preview.html from actual C# normalized artwork.");
 }

 static void RenderPush() {
  using(var rig=new SideWalk(287,348,300))using(var sheet=new Bitmap(12*144,2*174))using(Graphics g=Graphics.FromImage(sheet)) {
   g.Clear(Color.FromArgb(235,235,235));
   for(int row=0;row<2;row++)for(int col=0;col<12;col++) {
    double reach=col<3?IconCarryPlan.Smooth(col/2.0):col>8?1-IconCarryPlan.Smooth((col-8)/3.0):1;
    bool moving=col>=3 && col<=8;
    Bitmap frame=rig.Draw(col*.25,col*.12,moving?1:0,row==0?-1:1,moving?0:1,reach,reach*(moving?1:.65));
    g.DrawImage(frame,new Rectangle(col*144,row*174,144,174));
   }
   sheet.Save("Icon-push-check.png",ImageFormat.Png);
   Console.WriteLine("Saved Icon-push-check.png (pose samples only, no desktop modification).");
  }
 }

}
