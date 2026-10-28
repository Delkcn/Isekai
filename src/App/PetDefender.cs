using System;
using System.Windows.Forms;
namespace JouchoPet {
 sealed partial class Pet {
  bool defenderEnabled=true;IDefenderThreatSource defenderSource;
  readonly object defenderEventLock=new object();Action<DefenderThreat> defenderReceiver;
  readonly DefenderArcheryQueue defenderShots=new DefenderArcheryQueue();
  ToolStripMenuItem defenderToggle,defenderStatus,defenderLast;
  void AddDefenderMenu(ToolStripMenuItem bow){
   bow.DropDownItems.Add(new ToolStripSeparator());
   defenderToggle=new ToolStripMenuItem("Defender 威胁联动"){Checked=defenderEnabled};
   defenderToggle.Click+=delegate{defenderEnabled=!defenderEnabled;defenderToggle.Checked=defenderEnabled;if(defenderEnabled)StartDefender();else StopDefender();SaveSettings();RefreshDefenderStatus();};
   bow.DropDownItems.Add(defenderToggle);
   bow.DropDownItems.Add(new ToolStripMenuItem("低 / 中等 → 左；高 / 严重 → 右"){Enabled=false});
   defenderStatus=new ToolStripMenuItem("Defender：尚未连接"){Enabled=false};bow.DropDownItems.Add(defenderStatus);
   defenderLast=new ToolStripMenuItem("尚无新威胁触发射箭"){Enabled=false};bow.DropDownItems.Add(defenderLast);
   bow.DropDownItems.Add("重新连接 Defender",null,delegate{if(defenderEnabled){StopDefender();StartDefender();RefreshDefenderStatus();}});
  }
  void StartDefender(){
   if(!defenderEnabled||defenderSource!=null||IsDisposed||Disposing)return;
   ConnectDefender(new DefenderThreatMonitor());
  }
  internal void ConnectDefender(IDefenderThreatSource source){
   StopDefender();if(!defenderEnabled||IsDisposed||Disposing){source.Dispose();return;}
   lock(defenderEventLock){defenderSource=source;defenderReceiver=threat=>{lock(defenderEventLock){if(defenderSource==source&&defenderEnabled)defenderShots.Add(threat,DateTime.UtcNow);}};source.Detected+=defenderReceiver;}
   source.Start();RefreshDefenderStatus();
  }
  void StopDefender(){IDefenderThreatSource old;Action<DefenderThreat> receiver;lock(defenderEventLock){old=defenderSource;defenderSource=null;receiver=defenderReceiver;defenderReceiver=null;defenderShots.Clear();}if(old!=null){old.Detected-=receiver;old.Dispose();}}
  void RefreshDefenderStatus(){if(defenderStatus!=null)defenderStatus.Text="Defender："+(!defenderEnabled?"联动已关闭":defenderSource==null?"尚未连接":defenderSource.Status);}
  internal void UpdateDefender(DateTime now){
   if(!defenderEnabled||defenderSource==null||IsDisposed||Disposing)return;
   // Do not cancel user actions, wake sleep, interrupt icon carry, or fire through a menu.
   // Appearance switching is now allowed during every action. Keep automatic
   // archery's original idle guard independent of that UI capability.
   bool ready=!menuOpen&&!menu.Visible&&!choosingUtility&&!walking&&!running&&now>=happyUntil&&
    !dragging&&!sleeping&&!rest.BlocksWalking&&!greeting.Active&&!CarryActive&&carryAmount<.001&&
    !leap.Busy&&!archery.Busy&&magicArrow==null&&!sword.Busy&&!CastBusy&&!daily.Busy&&!UtilityBusy&&
    (motion.State=="idle"||motion.State=="hold")&&Math.Abs(motion.Speed)<.001;
   var threat=defenderShots.Take(now,ready);if(threat==null)return;
   StartArchery(threat.Facing);
   if(defenderLast!=null)defenderLast.Text="最近触发："+(threat.Facing<0?"低 / 中等 · 向左":"高 / 严重 · 向右")+" "+now.ToLocalTime().ToString("HH:mm:ss");
  }
  protected override void Dispose(bool disposing){if(disposing)StopDefender();base.Dispose(disposing);}
 }
}
