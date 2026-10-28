using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
namespace JouchoPet
{
    sealed partial class Pet
    {
        ToolStripMenuItem carryItem,carryStatus;
        bool carryEnabled;
        string carryPhase="";
        DesktopIcons carryShell;
        DesktopIcons.Item carryIcon;
        IconPositions iconPositions;
        Point iconStart,iconLast,iconEnd;
        int carryFacing,approachLeft,approachTop,carryStartLeft,carryStartTop;
        Point requestedPush,carryDirection;
        int runPushCells=1;
        bool requestedRun;
        double carryVertical,pushDistance;
        double approachY;
        double carryAge,carryAmount,nextCarry=15;
        double carryEffort;
        double carryGripEffort;
        CarryStyle carryStyle;
        CarryTravel carryTravel;
        RunningPickup carryPickup;
        double pickupDistance,pickupInitialSettle;
        bool carryRunLinedUp;
        ApproachLeap carryApproachLeap;
        bool CarryApproachAir {get{return carryPhase=="heightjump" && carryApproachLeap!=null;}}
        int carryRunStage;
        bool CarryRunApproach {get{return carryPhase=="runapproach" || carryPhase=="heightapproach" && carryStyle==CarryStyle.Run;}}
        bool CarryLeapActive {get{return carryPhase=="push" && carryTravel!=null && carryTravel.Style==CarryStyle.Leap;}}
        bool CarryRunActive {get{return carryPhase=="runpickup" || (carryPhase=="push" && carryTravel!=null && carryTravel.Style==CarryStyle.Run);}}
        string carryLayout;
        List<DesktopIcons.Item> carryObstacles;
        double obstacleAge;
        string lastCarryResult="尚未执行搬运";
        bool CarryActive { get { return carryPhase.Length!=0; } }
        void AddCarryMenu() {
            carryItem=new ToolStripMenuItem("允许搬动桌面图标（本次运行）",null,delegate { ToggleCarry(); });
            carryStatus=new ToolStripMenuItem("图标搬运：已关闭"){Enabled=false};
            menu.Items.Add(carryItem);
            menu.Items.Add("现在搬动附近图标",null,delegate { RequestPush(Point.Empty); });
            var directions=new ToolStripMenuItem("按方向推动附近图标");
            string[] labels={"向左推动","向右推动","向上推动","向下推动"};
            Point[] axes={new Point(-1,0),new Point(1,0),new Point(0,-1),new Point(0,1)};
            for(int i=0;i<axes.Length;i++){Point axis=axes[i];directions.DropDownItems.Add(labels[i],null,delegate{RequestPush(axis);});}
            menu.Items.Add(directions);
            var distanceMenu=new ToolStripMenuItem("跑动推动距离：1 格（本次运行）");
            for(int n=1;n<=6;n++) {
                int cells=n;var option=new ToolStripMenuItem(n+" 格"){Checked=n==runPushCells};
                option.Click+=delegate {
                    runPushCells=cells;
                    foreach(ToolStripMenuItem item in distanceMenu.DropDownItems)item.Checked=item==option;
                    distanceMenu.Text="跑动推动距离："+cells+" 格（本次运行）";
                };distanceMenu.DropDownItems.Add(option);
            }
            menu.Items.Add(distanceMenu);
            var running=new ToolStripMenuItem("按所选格数小跑推动");
            running.DropDownItems.Add("向左",null,delegate{RequestPush(new Point(-1,0),true);});
            running.DropDownItems.Add("向右",null,delegate{RequestPush(new Point(1,0),true);});
            menu.Items.Add(running);
            menu.Items.Add("检查桌面图标识别",null,delegate {
                try {using(var probe=new DesktopIcons()) {
                    var items=probe.Read();int nearby=0,visible=0;
                    foreach(var item in items){Point p=probe.ScreenPoint(item.Position);if(Math.Abs(p.X-(Left+Width/2))<400 && Math.Abs(p.Y-(Top+Height/2))<petHeight)nearby++;if(DesktopIcons.Unobscured(p,Handle))visible++;}
                    MessageBox.Show("读取到 "+items.Count+" 个图标；附近 "+nearby+" 个；未被窗口遮挡 "+visible+" 个。\n定位方式："+probe.MovementBackend+"\n"+carryStatus.Text+"\n"+lastCarryResult,"桌面图标识别（只读）");
                }}catch(Exception ex){MessageBox.Show(ex.Message,"桌面图标识别");}
            });
            menu.Items.Add("恢复搬动前的图标位置",null,delegate {
                CancelUtilityAction();CancelCarry();carryEnabled=false;carryItem.Checked=false;
                try { if(iconPositions==null)iconPositions=new IconPositions();using(var shell=new DesktopIcons())MessageBox.Show(iconPositions.Restore(shell),"图标位置恢复"); }
                catch(Exception ex){MessageBox.Show(ex.Message,"图标位置恢复");}
                carryStatus.Text="图标搬运：已关闭";
            });
            menu.Items.Add(carryStatus);menu.Items.Add(new ToolStripSeparator());
        }
        void RequestPush(Point axis,bool running=false) {
            CancelUtilityAction();
            CancelCast();CancelSword();CancelArchery();
            CancelLeap();
            if(!carryEnabled){MessageBox.Show("请先开启“允许搬动桌面图标”。");return;}
            if(CarryActive)CancelCarry("切换推动方向");
            requestedPush=axis;requestedRun=running;walking=false;stateUntil=DateTime.UtcNow.AddSeconds(8);nextCarry=elapsed+.3;
        }
        void ToggleCarry() {
            if(carryEnabled){carryEnabled=false;CancelCarry();carryItem.Checked=false;carryStatus.Text="图标搬运：已关闭";return;}
            try {using(var probe=new DesktopIcons()){probe.Read();probe.EnsureManual();}if(iconPositions==null)iconPositions=new IconPositions();carryEnabled=true;carryItem.Checked=true;nextCarry=elapsed+1;carryStatus.Text="图标搬运：等待附近空闲图标";}
            catch(Exception ex){MessageBox.Show(ex.Message,"无法开启图标搬运");}
        }
        void TryCarry() {
            nextCarry=elapsed+45;
            DesktopIcons shell=new DesktopIcons();bool keep=false;
            try {
                var items=shell.Read();bool gridAligned=shell.GridAligned;Rectangle area=Screen.FromControl(this).WorkingArea;
                // Choose the gait BEFORE checking the complete route. An active
                // trip captures its endpoint; changing the menu affects the next trip.
                CarryStyle horizontalStyle=requestedRun || random.Next(2)!=0?CarryStyle.Run:CarryStyle.Walk;
                double best=double.MaxValue;DesktopIcons.Item selected=null;int facing=1,startLeft=0,startTop=Top;Point end=Point.Empty,chosenDirection=Point.Empty;
                int far=0,covered=0,blocked=0;
                foreach(var item in items) {
                    Point p=shell.ScreenPoint(item.Position);
                    int half=shell.IconSize/2;
                    int targetTop=IconCarryPlan.PetTop(p.Y+half,petHeight);
                    double dy=Math.Abs(targetTop-Top);
                    if(dy>petHeight*.70 || Math.Abs(p.X+half-(Left+Width/2))>400){far++;continue;}
                    if(!DesktopIcons.Unobscured(new Point(p.X+half,p.Y+half),Handle)){covered++;continue;}
                    bool reachable=false;
                    Point[] axes=requestedPush==Point.Empty?new[]{new Point(-1,0),new Point(1,0),new Point(0,-1),new Point(0,1)}:new[]{requestedPush};
                    foreach(Point axis in axes)foreach(int sign in axis.X==0?new[]{-1,1}:new[]{axis.X}) {
                    int approach=p.X+half-Width/2-(int)(sign*petHeight*.20);
                    int spacing=axis.X==0?shell.Spacing.Y:shell.Spacing.X;
                    int distance=IconCarryPlan.PushDistance(spacing,gridAligned,axis.Y==0 && horizontalStyle==CarryStyle.Run,runPushCells);
                    Point destination=new Point(item.Position.X+axis.X*distance,item.Position.Y+axis.Y*distance);
                    Point screenEnd=shell.ScreenPoint(destination);
                    if(!area.Contains(new Rectangle(approach,targetTop,Width,Height)) || !area.Contains(new Rectangle(approach+axis.X*distance,targetTop+axis.Y*distance,Width,Height)))continue;
                    if(!area.Contains(new Rectangle(p.X,p.Y,shell.IconSize,shell.IconSize)) || !area.Contains(new Rectangle(screenEnd.X,screenEnd.Y,shell.IconSize,shell.IconSize)))continue;
                    if(!IconCarryPlan.Clear(item.Position,destination,item.Id,items,shell.Spacing))continue;
                    double score=Math.Abs(approach-Left)+dy*2;
                    reachable=true;
                    if(score<best){best=score;selected=item;facing=sign;startLeft=approach;startTop=targetTop;end=destination;chosenDirection=axis;}
                    }
                    if(!reachable)blocked++;
                }
                if(selected==null){carryStatus.Text="已识别 "+items.Count+" 个：距离过远 "+far+"，窗口遮挡 "+covered+"，边缘/路径受阻 "+blocked+"；小跑距离 "+runPushCells+" 格，空间不足时不会缩短";nextCarry=elapsed+5;return;}
                carryShell=shell;keep=true;carryObstacles=items;obstacleAge=0;carryIcon=selected;iconStart=iconLast=selected.Position;iconEnd=end;
                carryFacing=facing;approachLeft=startLeft;approachTop=startTop;approachY=Top;carryLayout=IconPositions.Layout();
                carryDirection=chosenDirection;carryVertical=chosenDirection.Y;requestedPush=Point.Empty;
                carryStyle=chosenDirection.Y!=0?CarryStyle.Leap:horizontalStyle;requestedRun=false;
                carryTravel=null;
                carryPhase="approach";carryAge=0;walking=false;carryStatus.Text="图标搬运：正在靠近";
                carryPickup=null;
                if(carryStyle==CarryStyle.Run) {
                    int lead=(int)Math.Ceiling(Math.Max(petHeight*.18,RunGait.SpeedAt300*petHeight/300*.42));
                    carryRunStage=Math.Max(area.Left,Math.Min(area.Right-Width,approachLeft-carryFacing*lead));
                    carryRunLinedUp=(approachLeft-Left)*carryFacing>=lead*.5;
                    carryPhase="runapproach";carryStatus.Text="图标搬运：跑着接近";
                }
                carryApproachLeap=ApproachLeap.Plan(Location,new Point(approachLeft,approachTop),area,Size,petHeight,carryFacing,carryStyle==CarryStyle.Run);
                if(carryApproachLeap!=null){carryPhase="heightapproach";carryStatus.Text=carryApproachLeap.Vertical<0?"图标搬运：准备向上跃近":"图标搬运：准备向下落近";}
            }finally {if(!keep)shell.Dispose();}
        }
        // Runs before the normal movement controller. No position teleport at arrival.
        int CarryDesired(double dt) {
            double effortTarget=CarryActive?carryAmount*(carryPhase=="push"?1:.65):0;
            if(CarryLeapActive || CarryRunActive)effortTarget=carryGripEffort;
            if(!menuOpen)carryEffort=Ease(carryEffort,effortTarget,8,dt);
            if(!CarryActive && carryEnabled && elapsed>=nextCarry && !sleeping && !rest.BlocksWalking && !greeting.Active && !dragging && !menuOpen && motion.State=="idle")TryCarry();
            if(!CarryActive)return 0;
            if(IconPositions.Layout()!=carryLayout){CancelCarry("屏幕布局改变");return 0;}
            if(dragging || sleeping || rest.BlocksWalking){CancelCarry("拖动、睡眠或抚摸动作打断");return 0;}
            // Pause before visibility checks or phase timers. Opening our menu
            // previously hit a different HWND and falsely reported completion.
            if(menuOpen)return 0;
            carryAge+=dt;
            Point screenIcon=carryShell.ScreenPoint(iconLast);
            if(!DesktopIcons.Unobscured(new Point(screenIcon.X+carryShell.IconSize/2,screenIcon.Y+carryShell.IconSize/2),Handle)){CancelCarry("图标被其他窗口遮挡");return 0;}
            if(carryPhase=="heightapproach") {
                int dx=carryApproachLeap.Launch.X-Left;
                if(Math.Abs(dx)>Math.Max(5,Math.Abs(motion.Speed)*dt))return Math.Sign(dx);
                carryPhase="heightalign";carryAge=0;
            }
            if(carryPhase=="heightalign") {
                if(motion.Facing!=carryApproachLeap.Facing || motion.State=="turn")return carryApproachLeap.Facing;
                if(motion.State!="hold" || motion.Speed!=0)return 0;
                carryApproachLeap=carryApproachLeap.FromActual(Location,Screen.FromControl(this).WorkingArea,Size,petHeight);
                carryPhase="heightjump";carryAge=0;moveRemainder=0;return 0;
            }
            if(CarryApproachAir)return 0;
            if(carryPhase=="runapproach") {
                if(!carryRunLinedUp) {
                    if((carryRunStage-Left)*carryFacing < -Math.Max(6,Math.Abs(motion.Speed)*dt))return -carryFacing;
                    carryRunLinedUp=true;
                }
                if(motion.Facing!=carryFacing || motion.State!="walk")return carryFacing;
                carryPickup=new RunningPickup(Location,new Point(approachLeft,approachTop),iconStart,iconEnd,petHeight,Math.Abs(motion.Speed));
                pickupInitialSettle=motion.Settle;
                carryPhase="runpickup";carryAge=0;pickupDistance=0;carryStartLeft=approachLeft;carryStartTop=approachTop;carryGripEffort=.65;moveRemainder=0;
            }
            if(carryPhase=="runpickup") {
                PickupSample sample=carryPickup.At(carryAge);carryAmount=sample.Grip;carryEffort=.65*carryAmount;
                carryStatus.Text=sample.Distance<carryPickup.ApproachDistance?"图标搬运：跑动中伸手接取":"图标搬运：连贯跑动推动";
                if(iconLast==iconEnd && carryAge>=carryPickup.Duration){motion.FollowPush(0,0,carryFacing,petHeight);carryPhase="release";carryAge=0;}
                return 0;
            }
            if(carryPhase=="approach") {
                int remaining=approachLeft-Left;
                if(Math.Abs(remaining)>6)return Math.Sign(remaining);
                if(Math.Abs(Top-approachTop)>1)return 0;
                carryPhase="align";carryAge=0;
            }
            if(carryPhase=="align") {
                if(motion.Facing!=carryFacing || motion.State=="turn")return carryFacing;
                carryPhase="reach";carryAge=0;carryStatus.Text="图标搬运：伸手";
            }
            if(carryPhase=="reach") {
                if(motion.State!="hold") {carryAge=0;return 0;}
                carryAmount=IconCarryPlan.Smooth(carryAge/.65);
                if(carryAge>=.65){
                    carryTravel=new CarryTravel(iconStart,iconEnd,petHeight,carryStyle);carryGripEffort=carryEffort;
                    carryPhase="push";carryStartLeft=Left;carryStartTop=Top;pushDistance=0;moveRemainder=0;carryAge=0;
                    carryStatus.Text="图标搬运："+(carryDirection.Y<0?"抱住向上轻跃":carryDirection.Y>0?"抱住向下轻落":(carryDirection.X<0?"向左":"向右")+(carryStyle==CarryStyle.Run?"小跑推动":"走动推动"));
                }
                return 0;
            }
            if(carryPhase=="push") {
                // Progress is the last position actually accepted by Explorer,
                // never merely distance walked by the character.
                if(iconLast==iconEnd && carryAge>=carryTravel.Duration){motion.FollowPush(0,0,carryFacing,petHeight);carryPhase="release";carryAge=0;return 0;}
                return 0;
            }
            if(carryPhase=="release") {
                if(motion.State!="hold"){carryAge=0;return 0;}
                carryAmount=1-IconCarryPlan.Smooth(carryAge/.65);
                if(carryAge>=.65)FinishCarry();
            }
            return 0;
        }
        void CarryAfterMove(double dt) {
            if(CarryApproachAir && !menuOpen) {
                Point landing=Point.Round(carryApproachLeap.At(carryAge));Rectangle bounds=Screen.FromControl(this).WorkingArea;
                if(!bounds.Contains(new Rectangle(landing,Size))){CancelCarry("接近跳跃超出屏幕可用范围");return;}
                Location=landing;motion.FollowPush(0,0,carryApproachLeap.Facing,petHeight);
                if(carryAge>=carryApproachLeap.Duration){carryPhase=carryStyle==CarryStyle.Run?"runapproach":"approach";carryAge=0;approachY=Top;carryRunLinedUp=(approachLeft-Left)*carryFacing>=Math.Max(6,petHeight*.06);carryApproachLeap=null;}
                return;
            }
            if(!CarryActive || menuOpen || (carryPhase!="push" && carryPhase!="runpickup"))return;
            obstacleAge+=dt;
            bool pickup=carryPhase=="runpickup";
            PickupSample runSample=pickup?carryPickup.At(carryAge):new PickupSample();
            CarrySample sample=pickup?new CarrySample():carryTravel.At(carryAge);Point target=pickup?runSample.Icon:sample.Position;
            Point petTarget=pickup && runSample.Distance<carryPickup.ApproachDistance?Point.Round(runSample.Pet):new Point(carryStartLeft+target.X-iconStart.X,carryStartTop+target.Y-iconStart.Y);
            Rectangle area=Screen.FromControl(this).WorkingArea;
            if(!area.Contains(new Rectangle(petTarget,Size))){CancelCarry("推动超出屏幕可用范围");return;}
            if(target!=iconLast) {
                if(obstacleAge>=.35){carryObstacles=carryShell.Read();obstacleAge=0;}
                if(!IconCarryPlan.Clear(iconLast,target,carryIcon.Id,carryObstacles,carryShell.Spacing)) {CancelCarry("推动路径被其他图标占用");return;}
                iconPositions.BeforeMove(carryIcon.Id,iconLast,target);
                try {
                    if(!carryShell.Move(carryIcon,iconLast,target)){CancelCarry("图标位置或标识被外部修改");return;}
                } catch(DesktopIcons.PositionFailure ex) {
                    iconPositions.BeforeMove(carryIcon.Id,iconLast,ex.Actual);iconLast=ex.Actual;throw;
                }
                iconLast=target;
            }
            Location=petTarget;
            if(pickup){motion.FollowPush(runSample.Distance-pickupDistance,runSample.Velocity,carryFacing,petHeight,true);motion.Settle=Math.Max(motion.Settle,pickupInitialSettle*(1-LeapMotion.Ease(carryAge/.18)));pickupDistance=runSample.Distance;}
            else FollowCarrySample(sample);
        }
        void FollowCarrySample(CarrySample sample) {
            if(CarryLeapActive)motion.FollowPush(0,0,carryFacing,petHeight);
            else motion.FollowPush(sample.Distance-pushDistance,sample.Velocity,carryFacing,petHeight,CarryRunActive);
            pushDistance=sample.Distance;
        }
        void FinishCarry() {
            int moved=(int)Math.Round(IconCarryPlan.Distance(iconStart,iconLast));
            if(iconLast!=iconEnd){CancelCarry("未达到目标位置");return;}
            lastCarryResult="搬运完成：Explorer 已确认移动 "+moved+" 像素";
            EndCarry();carryStatus.Text=lastCarryResult;
        }
        void EndCarry() {
            if((carryPhase=="push" || carryPhase=="runpickup") && motion!=null)motion.FollowPush(0,0,carryFacing,petHeight);
            carryPhase="";nextCarry=elapsed+45;stateUntil=DateTime.UtcNow.AddSeconds(5);
            carryTravel=null;
            carryPickup=null;
            carryApproachLeap=null;
            DesktopIcons shell=carryShell;carryShell=null;
            try {
                if(shell!=null && carryIcon!=null) {
                    Point? final=shell.FinishSmooth(carryIcon,iconLast);
                    if(final.HasValue && final.Value!=iconLast) {
                        if(iconPositions!=null)iconPositions.BeforeMove(carryIcon.Id,iconLast,final.Value);
                        iconLast=final.Value;lastCarryResult+="；已恢复网格吸附并记录吸附位置";
                    }
                }
            }catch(Exception ex){lastCarryResult+="；结束推动失败："+ex.Message;}
            finally {
                carryIcon=null;
                if(shell!=null)try{shell.Dispose();}catch(Exception ex){lastCarryResult+="；网格恢复失败，请重启桌宠："+ex.Message;}
            }
        }
        void CancelCarry(string reason="用户操作取消") {
            requestedRun=false;requestedPush=Point.Empty;
            // Stop at the last accepted location; restore remains an explicit menu action.
            if(CarryActive) {
                lastCarryResult="搬运中断（"+carryPhase+"）："+reason+"；实际移动 "+(int)Math.Round(IconCarryPlan.Distance(iconStart,iconLast))+" 像素";
                EndCarry();carryStatus.Text=lastCarryResult;
            }
            nextCarry=elapsed+45;
        }
        void CarryError(Exception ex) {
            CancelCarry(ex.Message);carryEnabled=false;if(carryItem!=null)carryItem.Checked=false;
            if(carryStatus!=null)carryStatus.Text="图标搬运：已暂停 — "+ex.Message;
            tray.ShowBalloonTip(6000,"图标搬运已暂停",ex.Message,ToolTipIcon.Info);
        }
    }
}
