using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
namespace JouchoPet {
 sealed class AnimationDebugWindow : Form {
  readonly Dictionary<string,NumericUpDown> editors=new Dictionary<string,NumericUpDown>();
  readonly Dictionary<string,TrackBar> sliders=new Dictionary<string,TrackBar>();
  Dictionary<string,double> saved=AnimationParameters.Snapshot();
  readonly PetRenderer renderer=new PetRenderer();
  readonly FrameClock timer;
  readonly Stopwatch clock=Stopwatch.StartNew();
  readonly string settingsPath;
  readonly PictureBox preview=new PictureBox();
  readonly ComboBox action=new ComboBox(),rate=new ComboBox();
  readonly TrackBar timeline=new TrackBar();
  readonly Label status=new Label(),hint=new Label(),phaseLabel=new Label();
  readonly Bitmap leapStage=new Bitmap(900,650);
  readonly Button pause=new Button();
  bool syncing,paused,disposed;
  int facing=1;
  double elapsed,phase,previous;
  public AnimationDebugWindow(string savePath=null) {
   settingsPath=savePath ?? AnimationParameters.SettingsPath;
   Text="动画参数调试";StartPosition=FormStartPosition.CenterScreen;
   Font=new Font("Microsoft YaHei UI",9);AutoScaleMode=AutoScaleMode.Dpi;
   ClientSize=new Size(980,720);MinimumSize=new Size(900,680);
   BackColor=Color.FromArgb(246,242,237);
   var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(16),ColumnCount=1,RowCount=4};
   root.RowStyles.Add(new RowStyle(SizeType.Absolute,50));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));
   root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));root.RowStyles.Add(new RowStyle(SizeType.Absolute,46));Controls.Add(root);
   root.Controls.Add(new Label{Text="动画工作台\n修改即时生效 · 沿用已保存的调整 · 新增参数自动补齐",Dock=DockStyle.Fill,AutoSize=false},0,0);
   var main=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};main.RowStyles.Add(new RowStyle(SizeType.Percent,100));main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,56));main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,44));root.Controls.Add(main,0,1);
   var tabs=new TabControl{Dock=DockStyle.Fill};main.Controls.Add(tabs,0,0);
   foreach(string group in new[]{"小跑","摆臂与手腕","单脚转身","挥手问好","轻跃"}) {
    var tab=new TabPage(group){BackColor=BackColor,AutoScroll=true};tabs.TabPages.Add(tab);
    var rows=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,Padding=new Padding(8)};tab.Controls.Add(rows);
    foreach(var spec in AnimationParameters.Specs)if(spec.Group==group)AddEditor(rows,spec);
   }
   var right=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(12,0,0,0),RowCount=7,ColumnCount=1};main.Controls.Add(right,1,0);
   right.RowStyles.Add(new RowStyle(SizeType.Absolute,35));right.RowStyles.Add(new RowStyle(SizeType.Percent,100));
   right.RowStyles.Add(new RowStyle(SizeType.Absolute,32));right.RowStyles.Add(new RowStyle(SizeType.Absolute,28));
   right.RowStyles.Add(new RowStyle(SizeType.Absolute,40));right.RowStyles.Add(new RowStyle(SizeType.Absolute,35));right.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
   action.DropDownStyle=ComboBoxStyle.DropDownList;action.Dock=DockStyle.Fill;
   action.Items.AddRange(new object[]{"小跑（原地预览）","普通走路（对照）","单脚转身：左右","单脚转身：正面 ↔ 侧面","原有转身（对照）","挥手问好（循环）","向上轻跃（含位移）","向下轻落（对照）","跑步衔接：起跑 → 换向 → 停下","睡眠：0.6.34 入睡 → 起身","搬运：走动推动","搬运：跑动推动","搬运：抱住向上轻跃","搬运：抱住向下轻落","白弓：完整转身 → 抬臂 → 射箭","接近文件：向上跃近","接近文件：向下落近"});action.SelectedIndex=0;right.Controls.Add(action,0,0);
   preview.Dock=DockStyle.Fill;preview.SizeMode=PictureBoxSizeMode.Zoom;preview.BackColor=Color.FromArgb(232,226,218);right.Controls.Add(preview,0,1);
   timeline.Maximum=1000;timeline.TickStyle=TickStyle.None;timeline.Dock=DockStyle.Fill;right.Controls.Add(timeline,0,2);
   phaseLabel.Dock=DockStyle.Fill;phaseLabel.TextAlign=ContentAlignment.MiddleCenter;right.Controls.Add(phaseLabel,0,3);
   var playback=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};right.Controls.Add(playback,0,4);
   pause.Text="暂停";pause.AutoSize=true;pause.Click+=delegate{paused=!paused;pause.Text=paused?"继续":"暂停";};playback.Controls.Add(pause);
   AddButton(playback,"逐帧",delegate{paused=true;pause.Text="继续";Advance(1.0/60);});
   AddButton(playback,"换方向",delegate{facing=-facing;DrawPreview();});
   var speedRow=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};speedRow.Controls.Add(new Label{Text="预览速度",AutoSize=true,Margin=new Padding(3,6,6,0)});
   rate.DropDownStyle=ComboBoxStyle.DropDownList;rate.Width=105;rate.Items.AddRange(new object[]{"0.25 倍","0.5 倍","1 倍"});rate.SelectedIndex=2;speedRow.Controls.Add(rate);right.Controls.Add(speedRow,0,5);
   right.Controls.Add(new Label{Text="可拖动进度条检查姿态。慢放只影响本窗口。\n转身时长、随机概率、跳跃距离下次动作生效。",Dock=DockStyle.Fill},0,6);
   hint.Dock=DockStyle.Fill;hint.Text="选择数值框或滑块可查看说明。修改也会作用于桌面角色对应的动作。";root.Controls.Add(hint,0,2);
   var bottom=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};bottom.RowStyles.Add(new RowStyle(SizeType.Percent,100));bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,440));bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));root.Controls.Add(bottom,0,3);
   var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};bottom.Controls.Add(buttons,0,0);
   AddButton(buttons,"保存参数",delegate{SaveCurrent();});
   AddButton(buttons,"撤销未保存",delegate{AnimationParameters.Apply(saved);RefreshEditors();});
   AddButton(buttons,"恢复默认",delegate{AnimationParameters.Reset();RefreshEditors();});
   AddButton(buttons,"导入",delegate{Import();});AddButton(buttons,"导出",delegate{Export();});
   status.Dock=DockStyle.Fill;status.TextAlign=ContentAlignment.MiddleLeft;bottom.Controls.Add(status,1,0);
   renderer.Resize(300);timer=new FrameClock(this);
   timer.Tick+=delegate{double now=clock.Elapsed.TotalSeconds,dt=Math.Min(.08,Math.Max(0,now-previous));previous=now;if(!paused)Advance(dt*(rate.SelectedIndex==0?.25:rate.SelectedIndex==1?.5:1));};
   action.SelectedIndexChanged+=delegate{elapsed=phase=0;DrawPreview();};
   tabs.SelectedIndexChanged+=delegate{action.SelectedIndex=tabs.SelectedIndex==2?2:tabs.SelectedIndex==3?5:tabs.SelectedIndex==4?6:0;};
   timeline.Scroll+=delegate{if(syncing)return;paused=true;pause.Text="继续";phase=timeline.Value/1000.0;elapsed=phase*PreviewDuration();DrawPreview();};
   Shown+=delegate{previous=clock.Elapsed.TotalSeconds;timer.Start();};
   FormClosing+=OnClosing;RefreshEditors();
   if(AnimationParameters.LoadNotice.Length>0)hint.Text=AnimationParameters.LoadNotice;
  }
  static void AddButton(Control parent,string text,Action click){var b=new Button{Text=text,AutoSize=true,Margin=new Padding(0,2,6,2)};b.Click+=delegate{click();};parent.Controls.Add(b);}
  void AddEditor(TableLayoutPanel rows,AnimationParameter spec) {
   var row=new TableLayoutPanel{Width=430,Height=64,Dock=DockStyle.Top,ColumnCount=2,RowCount=2,Margin=new Padding(0,0,4,4)};
   row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,108));
   row.RowStyles.Add(new RowStyle(SizeType.Absolute,29));row.RowStyles.Add(new RowStyle(SizeType.Absolute,32));
   row.Controls.Add(new Label{Text=spec.Label+"（"+spec.Unit+"）",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,0);
   var number=new NumericUpDown{Minimum=(decimal)spec.Min,Maximum=(decimal)spec.Max,Increment=(decimal)spec.Step,DecimalPlaces=2,Dock=DockStyle.Fill,Value=(decimal)AnimationParameters.Get(spec.Key)};
   row.Controls.Add(number,1,0);editors[spec.Key]=number;
   var slider=new TrackBar{Minimum=0,Maximum=1000,TickStyle=TickStyle.None,Dock=DockStyle.Fill};row.Controls.Add(slider,0,1);row.SetColumnSpan(slider,2);sliders[spec.Key]=slider;
   Action explain=delegate{hint.Text=spec.Hint+"  默认："+spec.Default.ToString("0.##")+" "+spec.Unit;};number.Enter+=delegate{explain();};slider.Enter+=delegate{explain();};slider.MouseEnter+=delegate{explain();};
   number.ValueChanged+=delegate{if(syncing)return;AnimationParameters.Set(spec.Key,(double)number.Value);syncing=true;slider.Value=ToSlider(spec,(double)number.Value);syncing=false;Changed();};
   slider.ValueChanged+=delegate{if(syncing)return;number.Value=(decimal)Math.Round(spec.Min+(spec.Max-spec.Min)*slider.Value/1000,2);};
   rows.Controls.Add(row);
  }
  static int ToSlider(AnimationParameter s,double value){return Math.Max(0,Math.Min(1000,(int)Math.Round((value-s.Min)/(s.Max-s.Min)*1000)));}
  void RefreshEditors(){syncing=true;foreach(var s in AnimationParameters.Specs){editors[s.Key].Value=(decimal)AnimationParameters.Get(s.Key);sliders[s.Key].Value=ToSlider(s,AnimationParameters.Get(s.Key));}syncing=false;Changed();}
  void Changed(){bool dirty=!AnimationParameters.Matches(saved);Text="动画参数调试"+(dirty?" · 未保存":"");status.Text=dirty?"已实时应用 · 尚未保存":"已应用 · 无待保存修改";DrawPreview();}
  double TurnDuration(){return action.SelectedIndex==4?.92:AnimationParameters.Get(action.SelectedIndex==3?"frontTurnDuration":"turnDuration");}
  CarryTravel PreviewCarry(){int vertical=action.SelectedIndex==12?-1:action.SelectedIndex==13?1:0;return new CarryTravel(Point.Empty,new Point(vertical==0?facing*120:0,vertical*120),300,action.SelectedIndex==11?CarryStyle.Run:CarryStyle.Walk);}
  double PreviewDuration(){return action.SelectedIndex==14?7.8:action.SelectedIndex>=15?ApproachPreviewPlan(action.SelectedIndex==15?-1:1,facing).Duration+.45:action.SelectedIndex==11?PickupPreviewPlan(facing).Duration+1.05:action.SelectedIndex>=10?PreviewCarry().Duration+1.7:action.SelectedIndex==5?4.8:action.SelectedIndex==6?1.95:action.SelectedIndex==7?1.79:action.SelectedIndex>=8?6.5:TurnDuration();}
  internal static ApproachLeap ApproachPreviewPlan(int vertical,int facing){return ApproachLeap.Plan(new Point(facing>0?90:450,vertical<0?280:60),new Point(facing>0?340:210,vertical<0?60:280),new Rectangle(0,0,900,650),PetRenderer.CanvasSize(300),300,facing,false);}
  internal static RenderFrame BowPreview(double time,int facing,out double firedAge){
   var motion=new Motion();var shot=new ArcheryMotion();shot.Request(facing);double begin=-1;
   for(double t=0;t<time;){double dt=Math.Min(1.0/120,time-t);
    if(!shot.Active)motion.Update(dt,shot.ApproachDirection(motion,true),300,shot.Pending);
    if(shot.Begin(motion,true))begin=t+dt;
    else {bool wasActive=shot.Active;shot.Update(dt);if(wasActive&&!shot.Active)motion.FollowPush(0,0,facing,300);}
    t+=dt;
   }
   firedAge=begin<0?-1:time-begin-ArcheryMotion.ReleaseAt;
   return new RenderFrame{Time=time,MotionState=motion.State,Facing=motion.Facing,Angle=motion.Angle,Settle=motion.Settle,Speed=motion.Speed,Gait=motion.Phase,LightTurn=motion.LightTurn,TurnProgress=motion.TurnProgress,TurnFrom=motion.TurnFrom,SupportNear=motion.SupportNear,Archery=shot.Active,BowPose=shot.Active?shot.Pose:new ArcheryPose{Facing=facing}};
  }
  internal static void DrawBowPreview(Graphics g,PetRenderer renderer,double time,int facing){
   Point origin=new Point(275,260);double firedAge;RenderFrame frame=BowPreview(time,facing,out firedAge);ArcheryPose pose=frame.BowPose;
   g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
   if(facing<0)ArcheryArt.Bow(g,origin,PetRenderer.CanvasSize(300).Width,300,pose);
   if(facing>0)g.DrawImageUnscaled(renderer.Draw(frame),origin);
   if(facing>0)ArcheryArt.Bow(g,origin,PetRenderer.CanvasSize(300).Width,300,pose);
   if(firedAge>=0){ArcheryPose release=ArcheryMotion.At(ArcheryMotion.ReleaseAt,facing);PointF axis=ArcheryMotion.Axis(facing),nock=ArcheryMotion.Nock(PetRenderer.CanvasSize(300).Width,300,release);
    var arrow=new MagicArrow(RigParts.Add(nock,origin.X+axis.X*ArcheryMotion.ArrowLength(300),origin.Y+axis.Y*ArcheryMotion.ArrowLength(300)),facing,new Rectangle(0,0,900,650),300);arrow.Update(firedAge);ArcheryArt.Projectile(g,arrow);
   }
   if(facing<0)g.DrawImageUnscaled(renderer.Draw(frame),origin);
  }
  internal static RunningPickup PickupPreviewPlan(int facing) {
   Point start=new Point(facing>0?170:430,180),contact=new Point(facing>0?380:220,180);
   Point icon=new Point(contact.X+PetRenderer.CanvasSize(300).Width/2+facing*60-16,312);
   return new RunningPickup(start,contact,icon,new Point(icon.X+facing*120,icon.Y),300,0);
  }
  internal static RenderFrame PickupPreview(double time,int facing,out Point pet,out Point icon) {
   var plan=PickupPreviewPlan(facing);var sample=plan.At(time);double grip=time<=plan.Duration?sample.Grip:1-LeapMotion.Ease((time-plan.Duration)/.65);
   var m=new Motion();m.FollowPush(sample.Distance,sample.Velocity,facing,300,true);pet=Point.Round(sample.Pet);icon=sample.Icon;
   return new RenderFrame{MotionState="walk",Facing=facing,Carry=grip,CarryEffort=.65*grip,RunningCarry=true,RunBlend=1,Speed=m.Speed,Gait=m.Phase,Settle=m.Settle};
  }
  internal static RenderFrame WavePreview(double time) {
   var motion=new GreetingMotion();motion.Request(true);
   for(double t=0;t<time;){double dt=Math.Min(1.0/120,time-t);motion.Update(dt,true);t+=dt;}
   return new RenderFrame{Time=time,MotionState="idle",Greeting=motion.Amount,WavePhase=motion.Phase,WaveBlend=1};
  }
  internal static RenderFrame RunPreview(double time,int direction,out double distance) {
   var motion=new Motion{SelectLightTurn=delegate{return true;}};distance=0;
   for(double t=0;t<time;){double dt=Math.Min(1.0/120,time-t);int desired=t<2.2?direction:t<4.5?-direction:0;distance+=motion.Update(dt,desired,300,false,desired!=0);t+=dt;}
   return new RenderFrame{Time=time,MotionState=motion.State,Facing=motion.Facing,Speed=motion.Speed,Gait=motion.Phase,RunBlend=motion.RunBlend,Settle=motion.Settle,Angle=motion.Angle,LightTurn=motion.LightTurn,TurnProgress=motion.TurnProgress,TurnFrom=motion.TurnFrom,SupportNear=motion.SupportNear};
  }
  internal static RenderFrame SleepPreview(double time) {
   var motion=new RestMotion();
   for(double t=0;t<time;){double dt=Math.Min(1.0/120,time-t);motion.RequestSleep(t<3);motion.Update(dt,true);t+=dt;}
   return new RenderFrame{Time=time,MotionState="idle",RestAmount=motion.Sleep,ClosedEyes=motion.Eyes};
  }
  void Advance(double dt){elapsed+=dt;phase+=dt*(action.SelectedIndex==1?36*SideWalk.Stance/(300*SideWalk.StrideRatio):RunGait.SpeedAt300*RunGait.Contact(1)/RunGait.Stride(300,1));DrawPreview();}
  void DrawPreview() {
   if(!renderer.Ready || disposed)return;
   var frame=new RenderFrame{Time=elapsed,MotionState="walk",Facing=facing,Gait=phase,Speed=action.SelectedIndex==1?36:RunGait.SpeedAt300,RunBlend=action.SelectedIndex==0?1:0};
   double progress=phase-Math.Floor(phase);
   if(action.SelectedIndex>=2 && action.SelectedIndex<=4) {
    double duration=TurnDuration(),cycle=duration+.3,t=elapsed%cycle;int pass=(int)(elapsed/cycle);
    progress=Math.Min(1,t/duration);int from=action.SelectedIndex==3?0:-facing,to=facing;if(pass%2==1){int old=from;from=to;to=old;}
    frame.MotionState="turn";frame.LightTurn=action.SelectedIndex!=4;frame.TurnFrom=from;frame.SupportNear=to<from;frame.TurnProgress=progress;
    frame.Angle=from+(to-from)*(frame.LightTurn?PivotTurn.Progress(progress):LeapMotion.Ease(progress));
   }
   if(action.SelectedIndex==5){double t=elapsed%4.8;progress=t/4.8;frame=WavePreview(t);}
   if(action.SelectedIndex==9){double t=elapsed%6.5;progress=t/6.5;frame=SleepPreview(t);}
   if(action.SelectedIndex==14) {
    double t=elapsed%PreviewDuration();progress=t/PreviewDuration();
    using(var g=Graphics.FromImage(leapStage)){g.Clear(Color.FromArgb(65,72,82));DrawBowPreview(g,renderer,t,facing);}preview.Image=leapStage;
   }else if(action.SelectedIndex>=15) {
    var plan=ApproachPreviewPlan(action.SelectedIndex==15?-1:1,facing);double t=elapsed%PreviewDuration();progress=t/PreviewDuration();
    frame=new RenderFrame{MotionState="hold",Facing=plan.Facing,Leaping=true,Leap=plan.Pose(t)};Point pet=Point.Round(plan.At(t));
    using(var g=Graphics.FromImage(leapStage)){g.Clear(preview.BackColor);g.DrawImageUnscaled(renderer.Draw(frame),pet);
     int ix=plan.Landing.X+PetRenderer.CanvasSize(300).Width/2+plan.Facing*60-16,iy=plan.Landing.Y+132;
     using(var brush=new SolidBrush(Color.FromArgb(230,241,255)))using(var pen=new Pen(Color.FromArgb(105,143,185),2)){g.FillRectangle(brush,ix,iy,32,40);g.DrawRectangle(pen,ix,iy,32,40);g.DrawLine(pen,ix+7,iy+14,ix+25,iy+14);g.DrawLine(pen,ix+7,iy+23,ix+25,iy+23);}
    }preview.Image=leapStage;
   }else if(action.SelectedIndex>=10) {
    var travel=PreviewCarry();double cycle=PreviewDuration(),t=elapsed%cycle;progress=t/cycle;
    double amount=t<.65?IconCarryPlan.Smooth(t/.65):t<travel.Duration+.65?1:1-IconCarryPlan.Smooth((t-travel.Duration-.65)/.65);
    CarrySample sample=travel.At(t-.65);var motion=new Motion();motion.FollowPush(sample.Distance,sample.Velocity,facing,300,travel.Style==CarryStyle.Run);
    frame=new RenderFrame{MotionState="hold",Facing=facing,Carry=amount,CarryEffort=.65*amount,CarryVertical=travel.Vertical,RunningCarry=travel.Style==CarryStyle.Run,RunBlend=motion.RunBlend,Gait=motion.Phase,Speed=motion.Speed,Settle=motion.Settle,Leaping=travel.Style==CarryStyle.Leap && t>=.65 && t<=travel.Duration+.65,Leap=sample.Leap};
    Point pet=new Point(275+sample.Position.X,180+sample.Position.Y),icon=new Point(pet.X+PetRenderer.CanvasSize(300).Width/2+facing*60-16,pet.Y+132);
    if(action.SelectedIndex==11)frame=PickupPreview(t,facing,out pet,out icon);
    using(var g=Graphics.FromImage(leapStage)) {
     g.Clear(preview.BackColor);
     int x=pet.X,y=pet.Y;
     g.DrawImageUnscaled(renderer.Draw(frame),x,y);
     using(var brush=new SolidBrush(Color.FromArgb(230,241,255)))using(var pen=new Pen(Color.FromArgb(105,143,185),2)) {
      int ix=icon.X,iy=icon.Y;g.FillRectangle(brush,ix,iy,32,40);g.DrawRectangle(pen,ix,iy,32,40);g.DrawLine(pen,ix+7,iy+14,ix+25,iy+14);g.DrawLine(pen,ix+7,iy+23,ix+25,iy+23);
     }
    }
    preview.Image=leapStage;
   }else if(action.SelectedIndex==8) {
    double t=elapsed%6.5,distance;progress=t/6.5;frame=RunPreview(t,facing,out distance);
    using(var g=Graphics.FromImage(leapStage)) {
     g.Clear(preview.BackColor);
     using(var pen=new Pen(Color.FromArgb(150,135,120),2))g.DrawLine(pen,30,482,870,482);
     g.DrawImageUnscaled(renderer.Draw(frame),275+(int)Math.Round(distance),160);
    }
    preview.Image=leapStage;
   }else if(action.SelectedIndex==6 || action.SelectedIndex==7) {
    int vertical=action.SelectedIndex==6?-1:1;var leap=new LeapMotion();leap.Request(vertical,facing);
    leap.Begin(new Point(275,vertical<0?240:35),new Rectangle(0,0,900,650),PetRenderer.CanvasSize(300),300);
    double t=Math.Min(leap.Duration,elapsed%(leap.Duration+.45));leap.Update(t);progress=t/leap.Duration;
    frame=new RenderFrame{MotionState="hold",Facing=facing,Leaping=true,Leap=leap.Pose};
    using(var g=Graphics.FromImage(leapStage)) {
     g.Clear(preview.BackColor);
     using(var pen=new Pen(Color.FromArgb(150,135,120),2)){g.DrawLine(pen,leap.Start.X+125,leap.Start.Y+322,leap.Start.X+222,leap.Start.Y+322);g.DrawLine(pen,leap.Target.X+125,leap.Target.Y+322,leap.Target.X+222,leap.Target.Y+322);}
     g.DrawImageUnscaled(renderer.Draw(frame),(int)Math.Round(leap.Position.X),(int)Math.Round(leap.Position.Y));
    }
    preview.Image=leapStage;
   }else preview.Image=renderer.Draw(frame);
   preview.Invalidate();
   syncing=true;timeline.Value=(int)Math.Round(progress*1000);syncing=false;
   phaseLabel.Text="动作进度 "+(progress*100).ToString("0")+"%";
  }
  bool SaveCurrent(){try{AnimationParameters.Save(settingsPath);saved=AnimationParameters.Snapshot();Changed();status.Text="已保存 · 下次启动沿用";return true;}catch(Exception ex){MessageBox.Show(this,"保存失败，当前调整仍保留：\n"+ex.Message,"保存参数",MessageBoxButtons.OK,MessageBoxIcon.Warning);return false;}}
  void Import(){using(var dialog=new OpenFileDialog{Filter="动画参数 (*.json)|*.json",Title="导入动画参数"})if(dialog.ShowDialog(this)==DialogResult.OK)try{AnimationParameters.Load(dialog.FileName);RefreshEditors();hint.Text="已导入并实时应用；点击保存参数后下次启动沿用。";}catch(Exception ex){MessageBox.Show(this,"导入失败，原参数未改变：\n"+ex.Message,"导入参数");}}
  void Export(){using(var dialog=new SaveFileDialog{Filter="动画参数 (*.json)|*.json",FileName="情绪-动画参数.json",DefaultExt="json",Title="导出当前参数"})if(dialog.ShowDialog(this)==DialogResult.OK)try{AnimationParameters.Save(dialog.FileName);hint.Text="已导出当前参数，可保留多个方案或发来继续调整。";}catch(Exception ex){MessageBox.Show(this,ex.Message,"导出参数失败");}}
  void OnClosing(object sender,FormClosingEventArgs e){if(AnimationParameters.Matches(saved))return;var answer=MessageBox.Show(this,"保存这次调整吗？\n选“否”会恢复打开窗口时或上次保存的参数。","动画参数尚未保存",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question);if(answer==DialogResult.Cancel || (answer==DialogResult.Yes && !SaveCurrent()))e.Cancel=true;else if(answer==DialogResult.No)AnimationParameters.Apply(saved);}
  // Used by the UI integration check; edits still go through real controls/events.
  internal void SetEditor(string key,double value){editors[key].Value=(decimal)value;}
  internal void SelectPreview(int index){action.SelectedIndex=index;}
  internal void StepPreview(){Advance(1.0/60);}
  protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;if(timer!=null)timer.Dispose();preview.Image=null;renderer.Dispose();leapStage.Dispose();}base.Dispose(disposing);}
 }
}
