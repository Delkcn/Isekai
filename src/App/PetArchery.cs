using System;
using System.Drawing;
using System.Windows.Forms;
namespace JouchoPet {
 sealed partial class Pet {
  readonly ArcheryMotion archery=new ArcheryMotion();
  MagicArrow magicArrow;MagicOverlay magicOverlay;Rectangle arrowScreen;
  void AddArcheryMenu(){var item=new ToolStripMenuItem("白弓 · 魔法箭");item.DropDownItems.Add("向左斜上方发射",null,delegate{StartArchery(-1);});item.DropDownItems.Add("向右斜上方发射",null,delegate{StartArchery(1);});item.DropDownItems.Add("随机选择一侧",null,delegate{StartArchery(random.Next(2)==0?-1:1);});menu.Items.Add(item);}
  void StartArchery(int facing){CancelCast();CancelSword();CancelArchery();CancelLeap();CancelCarry();greeting.Cancel();rest.RequestSleep(false);sleeping=false;sleepItem.Checked=false;walking=running=false;happyUntil=DateTime.MinValue;lastInput=DateTime.UtcNow;stateUntil=lastInput.AddSeconds(15);archery.Request(facing);if(bubble!=null)bubble.Dismiss();}
  void CancelArchery(){if(archery!=null)archery.Cancel();magicArrow=null;if(magicOverlay!=null)magicOverlay.Hide();}
  void UpdateArchery(double dt){if(menuOpen || dragging)return;
   Rectangle screen=Screen.FromControl(this).Bounds;
   if((archery.Active || magicArrow!=null) && screen!=arrowScreen){CancelCast();CancelSword();CancelArchery();return;}
   if(archery.Begin(motion,!rest.BlocksWalking && !greeting.Active)){arrowScreen=screen;return;}
   bool wasActive=archery.Active;
   if(archery.Update(dt)){ArcheryPose pose=ArcheryMotion.At(ArcheryMotion.ReleaseAt,archery.Facing);PointF nock=ArcheryMotion.Nock(Width,petHeight,pose),axis=ArcheryMotion.Axis(pose.Facing);double length=ArcheryMotion.ArrowLength(petHeight);PointF tip=RigParts.Add(nock,Left+axis.X*length,Top+axis.Y*length);magicArrow=new MagicArrow(tip,pose.Facing,screen,petHeight);magicArrow.Update(Math.Max(0,archery.Age-ArcheryMotion.ReleaseAt));}
   else if(magicArrow!=null)magicArrow.Update(dt);
   if(magicArrow!=null && magicArrow.Done)magicArrow=null;
   if(wasActive && !archery.Active){motion.FollowPush(0,0,archery.Facing,petHeight);stateUntil=DateTime.UtcNow.AddSeconds(6);}
  }
  Bitmap RenderMagic(Bitmap character){
   bool hasBow=archery.Active && ArcheryArt.HasBowEffect(archery.Pose);
   if(!hasBow && magicArrow==null){if(magicOverlay!=null)magicOverlay.Hide();return character;}
   bool behind=(hasBow && archery.Facing<0) || (magicArrow!=null && magicArrow.Direction.X<0);
   if(magicOverlay==null || magicOverlay.IsDisposed)magicOverlay=new MagicOverlay();
   Bitmap composed=magicOverlay.PaintEffect(Location,Width,petHeight,hasBow?(ArcheryPose?)archery.Pose:null,magicArrow,TopMost,behind?character:null);
   return behind?composed:character;
  }
 }
}