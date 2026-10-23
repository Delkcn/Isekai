using System;using System.Drawing;using JouchoPet;
class GestureCheck {
 static int checks;
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
 static int Error(int[] a,int[] b,int width,Rectangle region) {
  int error=0;for(int y=region.Top;y<region.Bottom;y++)for(int x=region.Left;x<region.Right;x++)for(int s=0;s<32;s+=8)
   error=Math.Max(error,Math.Abs((int)((uint)a[y*width+x]>>s&255)-(int)((uint)b[y*width+x]>>s&255)));
  return error;
 }
 static void Main(){try {
  var motion=new GreetingMotion();motion.Request();
  for(int i=0;i<120;i++)motion.Update(1.0/60,false);
  Check(motion.Active && motion.Amount==0,"greeting waits for front-facing awake pose");
  double previous=0,largest=0;
  for(int i=0;i<100;i++){motion.Update(1.0/60,true);largest=Math.Max(largest,Math.Abs(motion.Amount-previous));previous=motion.Amount;}
  Check(motion.Amount>.99 && largest<.12,"head tilt and expression ease in continuously");
  double before=motion.Amount;motion.Request();Check(motion.Amount==before,"repeated greeting preserves current pose");
  motion.Cancel();Check(motion.Amount==before,"cancel does not snap head upright");
  for(int i=0;i<120;i++)motion.Update(1.0/60,true);
  Check(!motion.Active && motion.Amount==0,"cancel settles and releases walking");
  motion.Request();for(int i=0;i<360;i++)motion.Update(1.0/60,true);
  Check(!motion.Active && motion.Amount==0,"greeting finishes without sticking");
  foreach(int size in new[]{220,300,400}) {
   Size c=PetRenderer.CanvasSize(size);
   Rectangle body=new Rectangle(0,(int)(28+size*.40),c.Width,c.Height-(int)(28+size*.40));
   Rectangle head=new Rectangle(0,28,c.Width,(int)(size*.30));
   Rectangle all=new Rectangle(0,0,c.Width,c.Height);
   using(var renderer=new PetRenderer()) {
    renderer.Resize(size);
    int[] neutral=PoseMesh.Read(renderer.Draw(new RenderFrame{MotionState="idle"}));
    foreach(double amount in new[]{.1,.4,.7,1}) {
     int[] straight=PoseMesh.Read(renderer.Draw(new RenderFrame{MotionState="idle",Affection=amount,ClosedEyes=Math.Min(1,amount*1.2),PatSide=0}));
     int[] tilted=PoseMesh.Read(renderer.Draw(new RenderFrame{MotionState="idle",Greeting=amount}));
     Check(Error(straight,tilted,c.Width,body)==0,"both arms torso and legs stay exactly at rest "+size+" / "+amount);
     Check(Error(straight,tilted,c.Width,head)>10,"greeting visibly tilts only head region "+size+" / "+amount);
    }
    int[] first=PoseMesh.Read(renderer.Draw(new RenderFrame{MotionState="idle",Greeting=1,WavePhase=0}));
    int[] second=PoseMesh.Read(renderer.Draw(new RenderFrame{MotionState="idle",Greeting=1,WavePhase=Math.PI/2}));
    Check(Error(first,second,c.Width,all)==0,"old wave phase no longer changes any pixels "+size);
    Check(Error(neutral,PoseMesh.Read(renderer.Draw(new RenderFrame{MotionState="idle"})),c.Width,all)==0,"greeting returns to exact neutral "+size);
    int[] left=PoseMesh.Read(renderer.Draw(new RenderFrame{MotionState="idle",Affection=1,ClosedEyes=1,PatSide=-1}));
    int[] right=PoseMesh.Read(renderer.Draw(new RenderFrame{MotionState="idle",Affection=1,ClosedEyes=1,PatSide=1}));
    Check(Error(left,right,c.Width,head)>10,"head petting still follows touched side "+size);
    Check(Error(left,right,c.Width,body)==0,"head petting leaves torso and arms unchanged "+size);
   }
  }
  Console.WriteLine("All "+checks+" gesture checks passed.");
 }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
}