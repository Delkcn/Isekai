using System;using System.Linq;using System.Drawing;using System.Drawing.Imaging;using System.Windows.Forms;
namespace JouchoPet {
 sealed partial class Pet:IUtilityActions {
  readonly UtilityMotion utility=new UtilityMotion();bool choosingUtility;
  bool UtilityBusy{get{return choosingUtility||utility.Busy;}}
  PointF utilityTarget;Point utilityStart,teleportEnd;DesktopFileTarget beamFile;double liftIconHeight;string utilityLayout;
  DesktopIcons liftShell;DesktopIcons.Item liftIcon;Point liftStart,liftEnd,liftLast;PointF liftCenter;System.Collections.Generic.List<DesktopIcons.Item> liftObstacles;double liftHeight,utilityValidateAge;
  SwordOverlay reachOverlay;UtilityOverlay beamOverlay;Bitmap teleportCanvas;
  ToolStripMenuItem utilityStatus;
  readonly HitFileOpen beamFileOpen=new HitFileOpen(new ShellFileOpener(true));string beamOpenStatus;
  public bool LookOut(double holdSeconds=1.8){return magicArrow==null&&RequestDaily(DailyAction.Lookout,holdSeconds);}
  bool UtilityAvailable(){if(InvokeRequired)throw new InvalidOperationException("Utility actions must run on the desktop UI thread.");return !UtilityBusy&&!daily.Busy&&!dragging&&!sleeping&&!rest.BlocksWalking&&!greeting.Active&&!CarryActive&&!leap.Busy&&!archery.Busy&&magicArrow==null&&!sword.Busy&&!CastBusy;}
  static bool OnScreen(Point p){return Screen.AllScreens.Any(s=>s.Bounds.Contains(p));}
  void StartUtility(UtilityAction kind,PointF target,int facing){beamFileOpen.Cancel();beamOpenStatus=null;utilityTarget=target;utilityStart=Location;utilityLayout=DisplayLayout();utilityValidateAge=0;utility.Request(kind,facing);walking=running=false;happyUntil=DateTime.MinValue;lastInput=DateTime.UtcNow;stateUntil=lastInput.AddSeconds(10);if(bubble!=null)bubble.Dismiss();}
  public bool Teleport(Point screenFootPoint){if(!UtilityAvailable()||!OnScreen(screenFootPoint))return false;Rectangle a=Screen.FromPoint(screenFootPoint).WorkingArea;
   var dest=new Point(screenFootPoint.X-Width/2,screenFootPoint.Y-(int)(28+petHeight*.98));dest.X=Math.Max(a.Left,Math.Min(a.Right-Width,dest.X));dest.Y=Math.Max(a.Top,Math.Min(a.Bottom-Height,dest.Y));if(dest==Location)return false;
   StartUtility(UtilityAction.Teleport,screenFootPoint,0);teleportEnd=dest;return true;}
  internal bool LightBeam(Point screenTarget){if(!UtilityAvailable()||!OnScreen(screenTarget))return false;int f=screenTarget.X<Left+Width/2?-1:1;if(RigParts.Length(screenTarget,new Point(Left+Width/2,Top+Height/2))<petHeight*.25)return false;StartUtility(UtilityAction.Beam,screenTarget,f);return true;}
  public bool LightBeamFile(string desktopFilePath){if(!UtilityAvailable())return false;
   try{beamFile=DesktopFileTarget.Open(desktopFilePath);StartUtility(UtilityAction.Beam,beamFile.Position,beamFile.Position.X<Left+Width/2?-1:1);beamFileOpen.Arm(beamFile);beamOpenStatus=null;return true;}
   catch(Exception ex){UtilityMessage(ex.Message);if(beamFile!=null){beamFile.Dispose();beamFile=null;}return false;}}
  public bool LevitateIcon(string iconId,Point screenDestination){if(!UtilityAvailable()||!OnScreen(screenDestination)||String.IsNullOrEmpty(iconId))return false;
   try{liftShell=new DesktopIcons();liftShell.EnsureManual();var items=liftShell.Read();liftIcon=items.FirstOrDefault(i=>i.Id==iconId);if(liftIcon==null)throw new InvalidOperationException("所选图标已经消失。");
    Rectangle rect=liftShell.IconScreenRectangle(liftIcon);PointF center=DesktopFileTarget.Center(rect);liftIconHeight=rect.Height;liftCenter=center;liftObstacles=items;liftStart=liftLast=liftIcon.Position;liftEnd=Point.Round(new PointF(liftStart.X+screenDestination.X-center.X,liftStart.Y+screenDestination.Y-center.Y));liftHeight=Math.Max(18,petHeight*.10);double transit=Math.Max(1.05,RigParts.Length(liftStart,liftEnd)/160.0);
    Point last=liftStart;for(int k=0;k<=70;k++){Point next=Point.Round(UtilityMotion.IconAt(liftStart,liftEnd,k*(1.65+transit)/70,liftHeight,transit));Rectangle moved=new Rectangle(rect.Left+next.X-liftStart.X,rect.Top+next.Y-liftStart.Y,rect.Width,rect.Height);if(!Screen.AllScreens.Any(s=>s.WorkingArea.Contains(moved))||!IconCarryPlan.Clear(last,next,iconId,items,liftShell.Spacing))throw new InvalidOperationException("悬浮路径经过其他图标或屏幕边缘，请选择更近的空白落点。");last=next;}
    if(iconPositions==null)iconPositions=new IconPositions();StartUtility(UtilityAction.Levitate,center,center.X<Left+Width/2?-1:1);utility.Transit=transit;return true;
   }catch(Exception ex){UtilityMessage(ex.Message);FinishLift();return false;}}
  public void CancelUtilityAction(){if(InvokeRequired)throw new InvalidOperationException("Utility actions must run on the desktop UI thread.");CancelEmergency();if(daily.Kind==DailyAction.Lookout||daily.Kind==DailyAction.Sigil)daily.Cancel();if(sigilOverlay!=null)sigilOverlay.Hide();EndUtility(false);}
  void UtilityMessage(string message){if(utilityStatus!=null)utilityStatus.Text=message;}
  void FinishLift(){var shell=liftShell;liftShell=null;if(shell==null)return;try{if(liftIcon!=null){Point? final=shell.FinishSmooth(liftIcon,liftLast);if(final.HasValue&&final.Value!=liftLast&&iconPositions!=null)iconPositions.BeforeMove(liftIcon.Id,liftLast,final.Value);}}catch(Exception e){UtilityMessage("结束悬浮："+e.Message);}finally{liftIcon=null;try{shell.Dispose();}catch(Exception e){UtilityMessage("恢复图标网格失败："+e.Message);}}}
  void EndUtility(bool completed){beamFileOpen.Cancel();if(!utility.Busy&&liftShell==null)return;var kind=utility.Kind;int facing=utility.Facing;UtilityMessage(completed?(beamOpenStatus??"动作完成"):"动作已停止；图标保留最后确认位置，可从恢复菜单撤销");FinishLift();if(beamFile!=null){beamFile.Dispose();beamFile=null;}utility.Reset();if(reachOverlay!=null)reachOverlay.Hide();if(beamOverlay!=null)beamOverlay.Hide();if(kind!=UtilityAction.Teleport)motion.FollowPush(0,0,facing,petHeight);stateUntil=DateTime.UtcNow.AddSeconds(5);nextCarry=elapsed+45;
  }
  int UtilityDesired(){if(!utility.Pending||utility.Facing==0)return 0;return motion.Facing==utility.Facing&&motion.State!="turn"?0:utility.Facing;}
  void UpdateUtility(double dt){if(!utility.Busy)return;
   if(dragging||sleeping||rest.BlocksWalking||greeting.Active||CarryActive||leap.Busy||archery.Busy||sword.Busy||CastBusy||walking||DisplayLayout()!=utilityLayout||(!utility.Pending&&Location!=utilityStart)){EndUtility(false);return;}
   if(menuOpen||!CastingMotion.ValidDelta(dt))return;
   if(utility.Pending){utilityStart=Location;bool ready=utility.Facing==0?motion.State=="idle"&&motion.Facing==0:motion.Facing==utility.Facing&&(motion.State=="hold"||(motion.State=="walk"&&Math.Abs(motion.Speed)<.6&&motion.Settle>.99));if(ready){utility.Pending=false;if(utility.Facing!=0)motion.FollowPush(0,0,utility.Facing,petHeight);}return;}
   double previousAge=utility.Age;utility.Age+=dt;
   if(beamFile!=null){utilityValidateAge+=dt;if(utilityValidateAge>=.10||(previousAge<.50&&utility.Age>=.50)){utilityValidateAge=0;try{if(!beamFile.Valid()){EndUtility(false);UtilityMessage("光束已取消：所选文件消失或图标位置改变");return;}}catch(Exception ex){EndUtility(false);UtilityMessage(ex.Message);return;}}}
   if(utility.Kind==UtilityAction.Beam&&previousAge<.50&&utility.Age>=.50){
    var result=beamFileOpen.Hit();if(beamFile!=null){beamFile.Dispose();beamFile=null;}if(result!=null){beamOpenStatus=result.Success?(result.OrdinaryOpenFallback?"已命中，目标不支持管理员运行，已普通打开":"已命中并请求以管理员权限运行"):"已命中，管理员运行未完成："+result.Error;UtilityMessage(beamOpenStatus);if(!result.Success)tray.ShowBalloonTip(5000,"管理员运行",beamOpenStatus,ToolTipIcon.Info);}
   }
   if(utility.Kind==UtilityAction.Teleport&&utility.Age>=.65&&!utility.Relocated){Location=teleportEnd;utilityStart=Location;utility.Relocated=true;}
   if(utility.Kind==UtilityAction.Levitate){try{
    Point target=Point.Round(UtilityMotion.IconAt(liftStart,liftEnd,utility.Age,liftHeight,utility.Transit));utilityValidateAge+=dt;
    if(utilityValidateAge>=.10){var items=liftShell.Read();liftObstacles=items;var current=items.FirstOrDefault(i=>i.Id==liftIcon.Id);if(current==null||current.Position!=liftLast){EndUtility(false);return;}utilityValidateAge=0;}
    if(target!=liftLast){if(!IconCarryPlan.Clear(liftLast,target,liftIcon.Id,liftObstacles,liftShell.Spacing)){EndUtility(false);return;}iconPositions.BeforeMove(liftIcon.Id,liftLast,target);try{if(!liftShell.Move(liftIcon,liftLast,target)){EndUtility(false);return;}}catch(DesktopIcons.PositionFailure e){iconPositions.BeforeMove(liftIcon.Id,liftLast,e.Actual);liftLast=e.Actual;throw;}liftLast=target;}
    utilityTarget=new PointF(liftCenter.X+liftLast.X-liftStart.X,liftCenter.Y+liftLast.Y-liftStart.Y);
   }catch(Exception ex){EndUtility(false);UtilityMessage("悬浮已停止："+ex.Message);return;}}
   if(utility.Age>=utility.Duration)EndUtility(true);
  }
  bool RenderUtility(){if(!utility.Active)return false;
   if(utility.Kind==UtilityAction.Teleport){var source=DrawForm(r=>r.Draw(new RenderFrame{MotionState="idle",Time=elapsed,Blink=blink}));if(teleportCanvas==null||teleportCanvas.Size!=source.Size){if(teleportCanvas!=null)teleportCanvas.Dispose();teleportCanvas=new Bitmap(source.Width,source.Height,PixelFormat.Format32bppPArgb);}using(var g=Graphics.FromImage(teleportCanvas)){g.Clear(Color.Transparent);UtilityArt.Teleport(g,source,utility.Age,petHeight);}Native.Present(Handle,teleportCanvas,Left,Top);return true;}
   PointF target=new PointF(utilityTarget.X-Left,utilityTarget.Y-Top);var rig=renderer.Reach;var full=DrawForm(r=>r.Reach.Draw(utility.Facing,utility.Amount,target));if(reachOverlay==null)reachOverlay=new SwordOverlay();Native.Present(Handle,reachOverlay.Present(full,Location,ClientSize,rig.Padding,TopMost),Left,Top);
   if(beamOverlay==null)beamOverlay=new UtilityOverlay();var finger=rig.Sample(utility.Facing,utility.Amount,target).Tip;bool tether=utility.Kind==UtilityAction.Levitate;double alpha=tether?(utility.Age<.45?0:utility.Amount):UtilityMotion.BeamAlpha(utility.Age);beamOverlay.PresentBeam(RigParts.Add(finger,Left,Top),utilityTarget,alpha,petHeight,TopMost,utility.Age,tether,liftIconHeight);return true;
  }
  void PickUtility(UtilityAction kind){if(!UtilityAvailable()){UtilityMessage("请等待当前动作结束。");return;}choosingUtility=true;walking=running=false;string id=null;Point destination=Point.Empty;bool selected=false;
   try{if(kind==UtilityAction.Beam){using(var picker=new CastFilePicker(null,"选择管理员运行目标","选择桌面程序或快捷方式，光束命中后请求管理员权限。Windows 可能弹出授权窗口；不支持管理员运行的目标会直接普通打开。","发射并请求授权")){selected=picker.ShowDialog()==DialogResult.OK;id=picker.SelectedPath;}}
    else{if(kind==UtilityAction.Levitate){using(var shell=new DesktopIcons())using(var picker=new UtilityIconPicker(shell.Read().ToArray())){if(picker.ShowDialog()!=DialogResult.OK)return;id=picker.SelectedId;}}
     using(var picker=new UtilityPointPicker(kind==UtilityAction.Teleport?"点击人物重新出现的脚下位置":"点击图标落点中心")){selected=picker.ShowDialog()==DialogResult.OK;destination=picker.Selected;}}
   }catch(Exception ex){UtilityMessage(ex.Message);}finally{choosingUtility=false;}
   if(selected){bool started=kind==UtilityAction.Teleport?Teleport(destination):kind==UtilityAction.Beam?LightBeamFile(id):LevitateIcon(id,destination);if(started)UtilityMessage("动作执行中…");else MessageBox.Show(utilityStatus.Text,"暂时无法执行动作");}
  }
  void AddUtilityMenu(){var root=new ToolStripMenuItem("轻魔法");root.DropDownItems.Add("光粒子移形…",null,delegate{PickUtility(UtilityAction.Teleport);});root.DropDownItems.Add("悬浮搬运图标…",null,delegate{PickUtility(UtilityAction.Levitate);});root.DropDownItems.Add("浅蓝光束 · 管理员运行…",null,delegate{PickUtility(UtilityAction.Beam);});root.DropDownItems.Add("停止新动作",null,delegate{CancelDailyAction();});utilityStatus=new ToolStripMenuItem("先选目标，再执行动作"){Enabled=false};root.DropDownItems.Add(utilityStatus);menu.Items.Add(root);AddEmergencySigilMenu();menu.Items.Add("十字法阵",null,delegate{CrossSigil();});}
  void DisposeUtility(){EndUtility(false);if(sigilOverlay!=null)sigilOverlay.Dispose();if(reachOverlay!=null)reachOverlay.Dispose();if(beamOverlay!=null)beamOverlay.Dispose();if(teleportCanvas!=null)teleportCanvas.Dispose();}
 }
}
