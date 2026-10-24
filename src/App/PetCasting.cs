using System;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
namespace JouchoPet {
 sealed partial class Pet:IFileCastAction {
  readonly CastingMotion casting=new CastingMotion();
  readonly CastingApproach castApproach=new CastingApproach();
  bool castPlanReady;int castPlanAttempts,castRequestTop;double castPendingAge;string castRequestLayout;
  bool CastRunApproach{get{return casting.Pending&&castApproach.Moving;}}
  MagicBolt castBolt;SwordOverlay castBodyOverlay;CastingOverlay castFlightOverlay;
  Point castLocation;Rectangle castDesktop;string castDisplayLayout;bool castAnchored,castEffects=true;
  DesktopFileTarget castFileTarget;string selectedCastFile;bool choosingCastFile;double castTargetPoll;
  ToolStripMenuItem castFileStatus,castAgain;
  bool CastBusy{get{return choosingCastFile||casting.Busy||castBolt!=null;}}
  public event EventHandler<ProjectileHitEventArgs> ProjectileHit;
  internal ProjectileHitEventArgs LastProjectileHit{get;private set;}
  void AddCastingMenu(){
   var item=new ToolStripMenuItem("双手施法 · 魔法弹");
   item.DropDownItems.Add("选择桌面文件并施法…",null,delegate{PickCastFile();});
   castAgain=new ToolStripMenuItem("向所选文件施法",null,delegate{TryCastSelected();}){Enabled=false};item.DropDownItems.Add(castAgain);
   castFileStatus=new ToolStripMenuItem("尚未选择目标文件"){Enabled=false};item.DropDownItems.Add(castFileStatus);
   var preview=new ToolStripMenuItem("动作预览（不锁文件）");preview.DropDownItems.Add("向左施法",null,delegate{DemoCast(-1);});preview.DropDownItems.Add("向右施法",null,delegate{DemoCast(1);});item.DropDownItems.Add(preview);
   item.DropDownItems.Add("取消施法",null,delegate{CancelCast();});
   var effects=new ToolStripMenuItem("显示魔法特效"){Checked=true,CheckOnClick=true};
   effects.CheckedChanged+=delegate{castEffects=effects.Checked;Render();};item.DropDownItems.Add(effects);menu.Items.Add(item);
   // Pending locomotion may move normally; once planted, a moved HWND cancels.
   LocationChanged+=delegate{if((castAnchored&&Location!=castLocation)||(casting.Pending&&Top!=castRequestTop))CancelCast();};
   VisibleChanged+=delegate{if(!Visible)CancelCast();};
  }
  void PickCastFile(){
   CancelCast();CancelCarry();walking=running=false;choosingCastFile=true;
   try{using(var picker=new CastFilePicker())if(picker.ShowDialog(this)==DialogResult.OK){selectedCastFile=picker.SelectedPath;castAgain.Enabled=true;TryCastSelected();}}
   finally{choosingCastFile=false;}
  }
  void TryCastSelected(){try{CastFile(selectedCastFile);}catch(Exception ex){castFileStatus.Text="未施法："+ex.Message;tray.ShowBalloonTip(5000,"施法目标",ex.Message,ToolTipIcon.Info);}}
  public Guid CastFile(string path){
   if(InvokeRequired)throw new InvalidOperationException("CastFile must run on the desktop UI thread.");
   DesktopFileTarget target=DesktopFileTarget.Open(path);
   try{Guid id=Cast(target.Position);castFileTarget=target;castTargetPoll=0;selectedCastFile=path;if(castFileStatus!=null)castFileStatus.Text="锁定："+Path.GetFileName(path);return id;}
   catch{target.Dispose();throw;}
  }
  bool ValidateCastFile(){
   if(castFileTarget==null)return true;
   try{if(castFileTarget.Valid())return true;}catch(Exception ex){Trace.WriteLine("Cast target: "+ex.Message);}
   if(castFileStatus!=null)castFileStatus.Text="施法取消：目标文件已消失或图标位置改变";CancelCast();return false;
  }
  void ReleaseCastFile(){var target=castFileTarget;castFileTarget=null;if(target!=null)try{target.Dispose();}catch(Exception ex){Trace.WriteLine("Cast target cleanup: "+ex.Message);}}
  void DemoCast(int facing){Rectangle r=Screen.FromControl(this).WorkingArea;Cast(new PointF(Math.Max(r.Left+12,Math.Min(r.Right-12,Left+Width*.5f+facing*petHeight*1.6f)),Top+petHeight*.45f));}
  public Guid Cast(PointF targetPosition){
   if(InvokeRequired)throw new InvalidOperationException("Cast must be dispatched on the desktop UI thread.");
   CastingMotion.Validate(targetPosition);Rectangle desktop=SystemInformation.VirtualScreen;
   if(targetPosition.X<desktop.Left||targetPosition.X>=desktop.Right||targetPosition.Y<desktop.Top||targetPosition.Y>=desktop.Bottom)throw new ArgumentException("Target must be inside the virtual desktop.");
   CancelCast();CancelSword();CancelArchery();CancelLeap();CancelCarry();greeting.Cancel();rest.RequestSleep(false);
   sleeping=false;sleepItem.Checked=false;walking=running=false;happyUntil=DateTime.MinValue;lastInput=DateTime.UtcNow;
   if(bubble!=null)bubble.Dismiss();
   castRequestTop=Top;castRequestLayout=DisplayLayout();
   return casting.Request(targetPosition,targetPosition.X<Left+Width*.5?-1:1);
  }
  public void CancelCast(){
   if(InvokeRequired)throw new InvalidOperationException("CancelCast must be dispatched on the desktop UI thread.");
   if(casting!=null)casting.Cancel();if(castBolt!=null){castBolt.Cancel();castBolt=null;}castAnchored=false;
   castApproach.Cancel();castPlanReady=false;castPlanAttempts=0;castPendingAge=0;
   ReleaseCastFile();
   if(castBodyOverlay!=null)castBodyOverlay.Hide();if(castFlightOverlay!=null)castFlightOverlay.Hide();
  }
  bool CastAimAt(double left,int facing,double limit,out double aim){
   PointF target=RigParts.Add(casting.Target,-left,-Top);
   if(!renderer.ResolveCastAim(target,facing,out aim))return false;
   return Math.Abs(aim)<=limit;
  }
  void CastPositionFailed(){
   const string message="当前屏幕没有合适的施法站位，请移动角色或选择其他文件后重试";
   if(castFileStatus!=null)castFileStatus.Text=message;
   CancelCast();tray.ShowBalloonTip(5000,"施法位置",message,ToolTipIcon.Info);
  }
  int CastDesired(bool ready){
   if(casting.Pending&&castApproach.Moving)return castApproach.Desired(motion,Left,ready);
   return casting.ApproachDirection(motion,ready);
  }
  void BeginCastIfReady(){
   if(!casting.Pending||menuOpen||dragging)return;
   if(rest.BlocksWalking||greeting.Active)return;
   double aim;
   if(!castPlanReady){
    if(CastAimAt(Left,casting.Facing,CastingMotion.MaxAim,out aim))castPlanReady=true;
    else {
     Rectangle area=Screen.FromControl(this).WorkingArea;
     if(++castPlanAttempts>3){CastPositionFailed();return;}
     bool found=castApproach.Plan(Left,area.Left,area.Right-Width,petHeight,casting.Facing,
        delegate(double x,int facing){double angle;return CastAimAt(x,facing,50,out angle);});
     if(!found)found=castApproach.Plan(Left,area.Left,area.Right-Width,petHeight,casting.Facing,
        delegate(double x,int facing){double angle;return CastAimAt(x,facing,60,out angle);});
     if(!found){CastPositionFailed();return;}
     casting.Facing=castApproach.Facing;castPlanReady=true;
     if(castFileStatus!=null)castFileStatus.Text="正在跑向合适的施法位置";
    }
   }
   if(castApproach.Moving)return;
   if(!CastingApproach.Stopped(motion)||motion.Facing!=casting.Facing)return;
   if(!ValidateCastFile())return;
   // Recheck after natural braking, without dropping the user's request.
   if(!CastAimAt(Left,casting.Facing,CastingMotion.MaxAim,out aim)){castPlanReady=false;return;}
   var shoulder=renderer.CastBones(CastingMotion.At(0,casting.Facing)).Shoulder;
   if(casting.Begin(motion,!rest.BlocksWalking&&!greeting.Active,RigParts.Add(shoulder,Left,Top))){
    PointF localTarget=RigParts.Add(casting.Target,-Left,-Top);
    casting.Aim=aim;casting.HeadAim=renderer.CastLookAim(localTarget,casting.Facing);
    if(castFileTarget!=null&&!DesktopIcons.Unobscured(Point.Round(castFileTarget.Position),Handle)){castFileStatus.Text="目标图标被窗口遮挡，请显示桌面后再次施法";tray.ShowBalloonTip(5000,"施法暂停",castFileStatus.Text,ToolTipIcon.Info);CancelCast();return;}
    castLocation=Location;castDesktop=SystemInformation.VirtualScreen;castDisplayLayout=DisplayLayout();castAnchored=true;moveRemainder=0;
    if(castFileStatus!=null&&castFileTarget!=null)castFileStatus.Text="已就位，正在施法："+Path.GetFileName(castFileTarget.Path);
   }
  }
  static string DisplayLayout(){string result="";foreach(var screen in Screen.AllScreens)result+=screen.DeviceName+screen.Bounds.ToString();return result;}
  void RecordProjectileHit(object sender,ProjectileHitEventArgs e){
   if(!ValidateCastFile())return;
   if(castFileTarget!=null){ReleaseCastFile();if(castFileStatus!=null)castFileStatus.Text="已命中："+Path.GetFileName(e.TargetFile);}
   LastProjectileHit=e;
   string line=DateTime.UtcNow.ToString("o")+" ProjectileHit "+e.RequestId+" target="+e.TargetPosition+" file="+e.TargetFile;
   Trace.WriteLine(line);
   try{string folder=Path.GetDirectoryName(settings);Directory.CreateDirectory(folder);File.AppendAllText(Path.Combine(folder,"projectile-hits.log"),line+Environment.NewLine);}catch(Exception ex){Trace.WriteLine("Projectile log: "+ex.Message);}
   var callbacks=ProjectileHit;if(callbacks!=null)foreach(EventHandler<ProjectileHitEventArgs> callback in callbacks.GetInvocationList())try{callback(this,e);}catch(Exception ex){Trace.WriteLine("ProjectileHit subscriber: "+ex.Message);}
  }
  void UpdateCast(double dt){
   if(castAnchored&&(Location!=castLocation||SystemInformation.VirtualScreen!=castDesktop||DisplayLayout()!=castDisplayLayout||motion.Facing!=casting.Facing)){CancelCast();return;}
   if(menuOpen||dragging)return;BeginCastIfReady();
   if(casting.Pending){
    castPendingAge+=dt;
    if(DisplayLayout()!=castRequestLayout){CancelCast();return;}
    if(castPendingAge>45){CastPositionFailed();return;}
   }
   castTargetPoll+=dt;if(castFileTarget!=null&&castTargetPoll>=.10){castTargetPoll=0;if(!ValidateCastFile())return;}
   bool active=casting.Active;double previous=casting.Age;Guid request=casting.Id;
   bool release=casting.Update(dt);
   if(release){
    if(!ValidateCastFile())return;
    PointF palm=renderer.CastCore(CastingMotion.At(CastingMotion.ReleaseAt,casting.Facing,casting.Aim));
    castBolt=new MagicBolt(casting.Id,RigParts.Add(palm,Left,Top),casting.Target,petHeight,castFileTarget==null?null:castFileTarget.Path);castBolt.ProjectileHit+=RecordProjectileHit;
    castBolt.Update(Math.Max(0,previous+dt-CastingMotion.ReleaseAt));
   }else if(castBolt!=null)castBolt.Update(dt);
   if(request!=casting.Id)return; // A hit subscriber may enqueue the next request.
   if(castBolt!=null&&castBolt.Done){castBolt=null;if(castFlightOverlay!=null)castFlightOverlay.Hide();}
   if(active&&!casting.Active){if(castBodyOverlay!=null)castBodyOverlay.Hide();motion.FollowPush(0,0,casting.Facing,petHeight);stateUntil=DateTime.UtcNow.AddSeconds(6);}
   if(!CastBusy)castAnchored=false;
  }
  void RenderCast(){
   if(castBodyOverlay==null||castBodyOverlay.IsDisposed)castBodyOverlay=new SwordOverlay();
   Bitmap full=renderer.DrawCast(casting.Pose,castEffects);
   Native.Present(Handle,castBodyOverlay.Present(full,Location,ClientSize,renderer.CastPadding,TopMost),Left,Top);
   RenderCastFlight();FollowSpeech();
  }
  void RenderCastFlight(){
   if(!castEffects||castBolt==null){if(castFlightOverlay!=null)castFlightOverlay.Hide();return;}
   if(castFlightOverlay==null||castFlightOverlay.IsDisposed)castFlightOverlay=new CastingOverlay();
   castFlightOverlay.PresentBolt(castBolt,TopMost);
  }
 }
}
