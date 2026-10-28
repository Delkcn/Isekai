using System;
using System.Windows.Forms;
namespace JouchoPet {
 sealed partial class Pet:IDailyActions {
  readonly DailyMotion daily=new DailyMotion();int lookoutFacing;
  bool LookoutBusy{get{return daily.Busy&&daily.Kind==DailyAction.Lookout;}}
  int DailyDesired(){return LookoutBusy&&motion.Facing!=lookoutFacing?lookoutFacing:0;}
  public bool Wink(){return RequestDaily(DailyAction.Wink);}
  public bool Think(double holdSeconds=1.5){return RequestDaily(DailyAction.Thinking,holdSeconds);}
  public bool ScratchHead(){return RequestDaily(DailyAction.Hair);}
  public bool AdjustHair(){return ScratchHead();}
  bool RequestDaily(DailyAction kind,double hold=1.5){
   if(InvokeRequired)throw new InvalidOperationException("Daily actions must run on the desktop UI thread.");
   if(UtilityBusy||dragging||sleeping||rest.BlocksWalking||greeting.Active||CarryActive||leap.Busy||archery.Busy||sword.Busy||CastBusy)return false;
   if(!daily.Request(kind,hold))return false;
   if(kind==DailyAction.Lookout)lookoutFacing=motion.Facing==0?direction:-motion.Facing;
   walking=running=false;happyUntil=DateTime.MinValue;lastInput=DateTime.UtcNow;stateUntil=lastInput.AddSeconds(hold+4);
   return true;
  }
  public void CancelDailyAction(){
   if(InvokeRequired)throw new InvalidOperationException("Daily actions must run on the desktop UI thread.");
   CancelUtilityAction();daily.Cancel();
  }
  void UpdateDaily(double dt){
   GuardSigil();
   if(UtilityBusy||dragging||sleeping||rest.BlocksWalking||greeting.Active||CarryActive||leap.Busy||archery.Busy||sword.Busy||CastBusy||walking){daily.Cancel();CancelEmergency();}
   bool was=daily.Busy,wasWink=daily.Kind==DailyAction.Wink;
   bool ready=LookoutBusy?motion.Facing==lookoutFacing&&(motion.State=="hold"||(motion.State=="walk"&&Math.Abs(motion.Speed)<.6&&motion.Settle>.99)):motion.State=="idle"&&motion.Facing==0&&(!wasWink||blink<.01);
   SelectCrossFiles(dt);EndHungAtSigil(dt);daily.Update(menuOpen?0:dt,ready);
   if(was&&!daily.Busy){if(reachOverlay!=null)reachOverlay.Hide();stateUntil=DateTime.UtcNow.AddSeconds(4);if(wasWink){blinkStart=-10;nextBlink=elapsed+2.6;}}
  }
  void AddDailyMenu(){
   var item=new ToolStripMenuItem("日常动作");
   item.DropDownItems.Add("单眼眨眼 · 左眼",null,delegate{Wink();});
   item.DropDownItems.Add("安静思考",null,delegate{Think();});
   item.DropDownItems.Add("轻轻挠头",null,delegate{ScratchHead();});
   item.DropDownItems.Add("搭额张望",null,delegate{LookOut();});
   item.DropDownItems.Add("结束日常动作",null,delegate{CancelDailyAction();});menu.Items.Add(item);
  }
 }
}
