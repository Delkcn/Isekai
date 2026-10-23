using System;
using System.Drawing;
using System.Windows.Forms;
namespace JouchoPet {
 sealed partial class Pet:ISwordSlashAction {
  readonly SwordMotion sword=new SwordMotion();
  SwordOverlay swordOverlay;Rectangle swordScreen;bool swordEffects=true;
  public Guid RequestId{get{return sword.RequestId;}}
  public event EventHandler<SwordHitEventArgs> HitFrame{add{sword.HitFrame+=value;}remove{sword.HitFrame-=value;}}
  void AddSwordMenu(){
   var item=new ToolStripMenuItem("单手挥剑");
   item.DropDownItems.Add("向左挥剑",null,delegate{StartSword(-1);});
   item.DropDownItems.Add("向右挥剑",null,delegate{StartSword(1);});
   var effects=new ToolStripMenuItem("显示星芒与斩击轨迹"){Checked=swordEffects,CheckOnClick=true};
   effects.CheckedChanged+=delegate{swordEffects=effects.Checked;Render();};
   item.DropDownItems.Add(effects);menu.Items.Add(item);
  }
  void StartSword(int facing){SwordSlash(new PointF(Left+Width*.5f+facing*petHeight*.55f,Top+petHeight*.5f),facing);}
  public void SwordSlash(PointF targetPosition,int facingDirection){
   if(InvokeRequired)throw new InvalidOperationException("SwordSlash must be dispatched on the desktop UI thread.");
   if(float.IsNaN(targetPosition.X)||float.IsInfinity(targetPosition.X)||float.IsNaN(targetPosition.Y)||float.IsInfinity(targetPosition.Y))throw new ArgumentException("Target must be finite.");
   CancelSword();CancelArchery();CancelLeap();CancelCarry();greeting.Cancel();rest.RequestSleep(false);sleeping=false;sleepItem.Checked=false;walking=running=false;happyUntil=DateTime.MinValue;lastInput=DateTime.UtcNow;
   sword.SwordSlash(targetPosition,facingDirection);
  }
  void CancelSword(){if(sword!=null)sword.Cancel();if(swordOverlay!=null)swordOverlay.Hide();}
  void BeginSwordIfReady(){if(!menuOpen&&!dragging&&sword.Begin(motion,!rest.BlocksWalking&&!greeting.Active))swordScreen=Screen.FromControl(this).Bounds;}
  void UpdateSword(double dt){if(menuOpen||dragging)return;var screen=Screen.FromControl(this).Bounds;if(sword.Active&&screen!=swordScreen){CancelSword();return;}if(sword.Begin(motion,!rest.BlocksWalking&&!greeting.Active)){swordScreen=screen;return;}bool active=sword.Active;sword.Update(dt);if(active&&!sword.Active){if(swordOverlay!=null)swordOverlay.Hide();motion.FollowPush(0,0,sword.Facing,petHeight);stateUntil=DateTime.UtcNow.AddSeconds(6);}}
  void RenderSword(){if(swordOverlay==null||swordOverlay.IsDisposed)swordOverlay=new SwordOverlay();Bitmap full=renderer.DrawSword(sword.Pose,swordEffects);Native.Present(Handle,swordOverlay.Present(full,Location,ClientSize,renderer.SwordPadding,TopMost),Left,Top);FollowSpeech();}
 }
}
