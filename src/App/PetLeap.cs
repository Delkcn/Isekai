using System;
using System.Drawing;
using System.Windows.Forms;
namespace JouchoPet {
 sealed partial class Pet {
  readonly LeapMotion leap=new LeapMotion();
  void StartLeap(int vertical) {
   CancelCast();CancelSword();CancelArchery();
   CancelLeap();SetSleep(false);greeting.Cancel();walking=false;running=false;
   leap.Request(vertical,motion.Facing==0?direction:motion.Facing);
   stateUntil=DateTime.UtcNow.AddSeconds(6);
   Say(vertical<0?(CurrentForm==CharacterForm.AnemoneII?"借一点羽毛的光。\n……到上面看看。":"借一点花瓣的力气。\n……到上面看看。"):"往下挪一点。\n放心，我会站稳的。");
  }
  void CancelLeap() {
   bool wasActive=leap.Active;leap.Cancel();
   if(wasActive){motion.Speed=0;motion.RunBlend=0;motion.Settle=1;motion.State="hold";moveRemainder=0;}
  }
  void UpdateLeap(double dt) {
   if(menuOpen)return;
   Rectangle area=Screen.FromControl(this).WorkingArea;
   if(leap.CanBegin(motion,!rest.BlocksWalking && !greeting.Active)) {
    if(!leap.Begin(Location,area,ClientSize,petHeight,motion.Facing))Say("这里已经到边缘了。\n换个位置再试吧。");
    moveRemainder=0;
   }
   if(!leap.Active)return;
   // A display/work-area change cancels the trajectory instead of replaying stale coordinates.
   if(!area.Contains(new Rectangle(leap.Target,ClientSize)) || !area.Contains(new Rectangle(leap.Start,ClientSize))) {
    CancelLeap();Clamp();return;
   }
   bool done=leap.Update(dt);
   Location=new Point((int)Math.Round(leap.Position.X),(int)Math.Round(leap.Position.Y));
   if(done) {
    motion.Speed=0;motion.RunBlend=0;motion.Settle=1;motion.State="hold";
    stateUntil=DateTime.UtcNow.AddSeconds(4);
   }
  }
 }
}
