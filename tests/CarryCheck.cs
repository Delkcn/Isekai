using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Forms;
using JouchoPet;
class CarryCheck {
 static int count;
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static void Set(object obj,string name,object value){obj.GetType().GetField(name,Flags).SetValue(obj,value);}
 static object Get(object obj,string name){return obj.GetType().GetField(name,Flags).GetValue(obj);}
 static object Call(object obj,string name,params object[] values){return obj.GetType().GetMethod(name,Flags).Invoke(obj,values);}
 static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
 [STAThread] static void Main() {
  try {
   PathChecks();
   foreach(int height in new[]{220,300,400}) {
    int oldTop=500-(int)(28+height*.60),newTop=IconCarryPlan.PetTop(500,height);
    Check(newTop-oldTop==height/5,"pet moves down by 20 percent of character height: "+height);
    foreach(int delta in new[]{-100,100})Check(IconCarryPlan.PetTop(500+delta,height)-newTop==delta,"vertical push retains relative offset: "+height+" / "+delta);
   }
   // Exercise the real completion/cancellation methods without opening a pet,
   // loading settings, creating tray icons, or moving a real desktop item.
   var pet=(Pet)FormatterServices.GetUninitializedObject(typeof(Pet));GC.SuppressFinalize(pet);
   using(var status=new ToolStripMenuItem()) {
    Set(pet,"carryStatus",status);Set(pet,"carryFacing",1);Set(pet,"iconStart",new Point(100,100));Set(pet,"iconEnd",new Point(200,100));
    foreach(int moved in new[]{-100,0,10,100}) {
     Set(pet,"carryPhase","release");Set(pet,"iconLast",new Point(100+moved,100));Call(pet,"FinishCarry");
     Check(status.Text.Contains(moved==100?"搬运完成":"搬运中断"),"completion requires confirmed displacement: "+moved);
     Check((string)Get(pet,"carryPhase")=="","completion/cancellation closes active transaction");
    }
    foreach(int sign in new[]{-1,1}) {
     Set(pet,"iconEnd",new Point(100,100+sign*100));
     foreach(int moved in new[]{0,94,100}) {
      Set(pet,"carryPhase","release");Set(pet,"iconLast",new Point(100,100+sign*moved));Call(pet,"FinishCarry");
      Check(status.Text.Contains(moved==100?"搬运完成":"搬运中断"),"vertical completion requires exact Explorer endpoint: "+sign+" / "+moved);
     }
    }
    Set(pet,"carryPhase","reach");Call(pet,"CancelCarry","测试外部遮挡");
    Check(status.Text.Contains("测试外部遮挡") && status.Text.Contains("reach"),"cancellation preserves stage and reason");
    Set(pet,"carryPhase","reach");Set(pet,"carryLayout",IconPositions.Layout());Set(pet,"rest",new RestMotion());Set(pet,"menuOpen",true);Set(pet,"carryAge",.3);
    Check((int)Call(pet,"CarryDesired",.05)==0,"own menu pauses carry before shell visibility lookup");
    Check((double)Get(pet,"carryAge")==.3 && (string)Get(pet,"carryPhase")=="reach","own menu does not advance or cancel reach");
    Call(pet,"CarryAfterMove",.05);Check((string)Get(pet,"carryPhase")=="reach","own menu does not move icon");
   }
   Size c=PetRenderer.CanvasSize(300);var sprites=SpriteAssets.LoadFront();
   try {using(var front=new SoftRig(sprites,c.Width,c.Height,300))using(var sides=new SideWalk(c.Width,c.Height,300))using(var turn=new TurnRig(c.Width,c.Height,300)) {
    turn.Prepare(front.Draw(0,0,0,0,0,0,0,new[]{1.0,0,0}),sides);object model=Get(turn,"model");
    var source=(PointF[][])Get(model,"uv");var destination=(PointF[][])Get(model,"proportions");
    foreach(int view in new[]{1,3})foreach(int row in new[]{4,16}) {
     int k=row*13;double expected=(source[view-1][k+9].X-source[view-1][k+3].X+source[view+1][k+9].X-source[view+1][k+3].X)*.5;
     double actual=destination[view][k+9].X-destination[view][k+3].X;
     Check(Math.Abs(source[view][k+9].X-source[view][k+3].X-actual)<.1,"new slim painting is not widened by old correction: "+view+" / "+row);
    }
   }}finally{foreach(Bitmap b in sprites)b.Dispose();}
   Console.WriteLine("All "+count+" carry/proportion checks passed.");
  }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}
 }
 static void PathChecks() {
  Point start=new Point(300,300),spacing=new Point(80,100);
  foreach(Point axis in new[]{new Point(-1,0),new Point(1,0),new Point(0,-1),new Point(0,1)}) {
   Point end=new Point(start.X+axis.X*120,start.Y+axis.Y*120);
   var obstacles=new System.Collections.Generic.List<DesktopIcons.Item>{new DesktopIcons.Item{Id="self",Position=start}};
   Check(IconCarryPlan.Clear(start,end,"self",obstacles,spacing),"free four-direction corridor "+axis);
   obstacles.Add(new DesktopIcons.Item{Id="blocker",Position=new Point(start.X+axis.X*60,start.Y+axis.Y*60)});
   Check(!IconCarryPlan.Clear(start,end,"self",obstacles,spacing),"obstacle midway in four-direction corridor "+axis);
   obstacles[1].Position=new Point(start.X-axis.Y*200,start.Y+axis.X*200);
   Check(IconCarryPlan.Clear(start,end,"self",obstacles,spacing),"off-path icon is not blocked "+axis);
   foreach(double dt in new[]{1.0/30,1.0/60,.05}) {
    Point previous=start;double duration=IconCarryPlan.Duration(start,end);var motion=new Motion();double oldDistance=0;
    for(double time=0;time<duration+dt;time+=dt) {
     Point p=IconCarryPlan.Position(start,end,time);
     if((p.X-previous.X)*axis.X+(p.Y-previous.Y)*axis.Y<0 || IconCarryPlan.Distance(previous,p)>Math.Ceiling(45*dt)+1 || IconCarryPlan.Distance(start,p)>120)throw new Exception("trajectory reversed, jumped or overshot");
     if((axis.X==0 && p.X!=start.X)||(axis.Y==0 && p.Y!=start.Y))throw new Exception("trajectory escaped axis");
     double u=Math.Min(1,time/duration),distance=120*IconCarryPlan.Smooth(u),velocity=24*30*u*u*(1-u)*(1-u);
     motion.FollowPush(distance-oldDistance,velocity,1,300);oldDistance=distance;previous=p;
    }
    Check(IconCarryPlan.Position(start,end,duration)==end && IconCarryPlan.Position(start,end,-1)==start,"smooth path clamps exact endpoints "+axis+" / "+dt);
    Check(Math.Abs(motion.Phase-120*SideWalk.Stance/(300*SideWalk.StrideRatio))<1e-7,"gait follows distance independent of frame rate "+axis+" / "+dt);
   }
   Check(IconCarryPlan.Position(start,end,.05)==start && IconCarryPlan.Position(start,end,IconCarryPlan.Duration(start,end)-.05)==end,"soft start and stop "+axis);
  }
 }
}
