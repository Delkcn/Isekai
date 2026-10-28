using System;
using System.Drawing;
using System.Windows.Forms;
namespace JouchoPet
{
    sealed partial class Pet
    {
        static double Ease(double current,double target,double rate,double dt)
        {
            return current+(target-current)*(1-Math.Exp(-rate*dt));
        }
        void Animate(object sender, EventArgs e)
        {
            double nowSeconds=clock.Elapsed.TotalSeconds;
            UpdateFormTransition(nowSeconds);
            double dt=Math.Min(0.05,Math.Max(0,nowSeconds-previousTime));
            previousTime=nowSeconds;
            elapsed=nowSeconds;
            DateTime now=DateTime.UtcNow;
            UpdateDefender(now);
            bool happy=now<happyUntil;
            UpdateUtility(dt);
            UpdateDaily(dt);
            if(!CarryActive)carryAmount=Ease(carryAmount,0,10,dt);
            int carryDesired=0;
            try { if(!UtilityBusy && !daily.Busy && !leap.Busy && !archery.Busy && !sword.Busy && !CastBusy)carryDesired=CarryDesired(dt); } catch(Exception ex) { CarryError(ex); }
            if(leap.Busy)carryEffort=Ease(carryEffort,0,8,dt);
            if(!CarryActive) {
                if(carryAmount<.001)carryAmount=0;
                if(carryEffort<.001)carryEffort=0;
            }
            if(!UtilityBusy && !daily.Busy && !CarryActive && !leap.Busy && !archery.Busy && !sword.Busy && !CastBusy && !dragging && !menuOpen)
            {
                if(!sleeping && (now-lastInput).TotalMinutes>=5) SetSleep(true);
                if(!sleeping && !happy && !rest.BlocksWalking && wandering && now>=stateUntil)
                {
                    walking=!walking;
                    running=false;
                    direction=random.Next(2)==0?-1:1;
                    stateUntil=now.AddSeconds(walking?random.Next(4,9):random.Next(5,11));
                }
            }
            // Claim an already planted pose before ordinary locomotion can add
            // a needless brake/settle cycle to the sword request.
            if(!utility.Active && !daily.Active){BeginSwordIfReady();BeginCastIfReady();}
            int desired=walking && wandering && !sleeping && !happy && !rest.BlocksWalking && !greeting.Active && !dragging && !menuOpen?direction:0;
            if(CarryActive)desired=menuOpen?0:carryDesired;
            if(archery.Busy)desired=archery.ApproachDirection(motion,!menuOpen && !rest.BlocksWalking && !greeting.Active);
            if(sword.Busy)desired=sword.ApproachDirection(motion,!menuOpen && !rest.BlocksWalking && !greeting.Active);
            if(CastBusy)desired=CastDesired(!menuOpen && !rest.BlocksWalking && !greeting.Active && !dragging);
            if(leap.Pending)desired=leap.ApproachDirection(motion,!menuOpen && !rest.BlocksWalking && !greeting.Active);
            if(daily.Busy)desired=DailyDesired();
            if(choosingUtility)desired=0;
            if(utility.Busy)desired=UtilityDesired();
            double travel=utility.Active || choosingUtility || daily.Active || casting.Active || (CastBusy && menuOpen) || sword.Active || (sword.Busy && menuOpen) || archery.Active || (archery.Busy && menuOpen) || leap.Active || CarryApproachAir || (CarryActive && (menuOpen || carryPhase=="push" || carryPhase=="runpickup"))?0:
                motion.Update(dt,desired,petHeight,(CarryActive && !CarryRunApproach) || leap.Pending || archery.Pending || sword.Pending || CastBusy || (utility.Busy&&utility.Facing!=0) || LookoutBusy,CastRunApproach || CarryRunApproach || (running && !CarryActive && !leap.Busy));
            rest.Update(daily.Active||UtilityBusy?0:dt, motion.State=="idle" && motion.Facing==0);
            greeting.Update(menuOpen||UtilityBusy||daily.Active?0:dt,motion.State=="idle" && motion.Facing==0 && rest.Sleep<.025);
            greetingWaveBlend=Ease(greetingWaveBlend,greetingWaves?1:0,9,menuOpen?0:dt);
            speed=motion.Speed;
            gait=motion.Phase;
            walkBlend=Ease(walkBlend,Math.Min(1,Math.Abs(speed)/36),5,dt);
            if(!dragging && Math.Abs(travel)>0)
            {
                moveRemainder+=travel;
                int delta=(int)moveRemainder;
                moveRemainder-=delta;
                Rectangle a=Screen.FromControl(this).WorkingArea;
                int next=Left+delta;
                if(next<a.Left || next+Width>a.Right)
                {
                    direction=-direction;
                    motion.Boundary();
                    moveRemainder=0;
                }
                Left=Math.Max(a.Left,Math.Min(next,a.Right-Width));
            }
            else if(motion.State=="turn" || motion.State=="idle") moveRemainder=0;
            if(!UtilityBusy&&!daily.Active){
            UpdateLeap(dt);
            UpdateArchery(dt);
            UpdateSword(dt);
            UpdateCast(dt);
            }
            try { if(!UtilityBusy&&!daily.Active)CarryAfterMove(dt); } catch(Exception ex) { CarryError(ex); }
            // CarryAfterMove synchronizes the gait with the accepted icon path.
            speed=motion.Speed;gait=motion.Phase;
            Point cursor=Cursor.Position;
            double tx=Math.Max(-1,Math.Min(1,(cursor.X-(Left+Width/2))/350.0));
            double ty=Math.Max(-1,Math.Min(1,(cursor.Y-(Top+Height/3))/350.0));
            lookX=Ease(lookX,sleeping?0:tx*0.30,2,dt);
            lookY=Ease(lookY,sleeping?0:ty*0.25,2,dt);
            if(!dragging) dragLean=Ease(dragLean,0,6,dt);
            lean=Ease(lean,dragging?dragLean:walkBlend*direction*0.35,5,dt);
            if(elapsed>=nextBlink && !(daily.Active&&daily.Kind==DailyAction.Wink))
            {
                blinkStart=elapsed;
                nextBlink=elapsed+2.6+random.NextDouble()*3.6;
            }
            double blinkAge=elapsed-blinkStart;
            blink=blinkAge>=0 && blinkAge<0.19 ? Math.Sin(Math.PI*blinkAge/0.19):0;
            if(daily.Active&&daily.Kind==DailyAction.Wink)blink=0;
            // Rendering uses registered anatomical meshes, not optical flow.
            double renderStart=clock.Elapsed.TotalSeconds;
            Render();
            frameStatistics.Record(nowSeconds,(clock.Elapsed.TotalSeconds-renderStart)*1000,timer.Turning);
        }
        void Render()
        {
            if(!IsHandleCreated || IsDisposed || !renderer.Ready) return;
            UpdateFormTransition(clock.Elapsed.TotalSeconds);
            timer.Turning=motion.State=="turn" && !utility.Active && !(daily.Active&&LookoutBusy) && !sword.Active && !casting.Active && !archery.Active;
            if(!SigilVisible&&sigilOverlay!=null)sigilOverlay.Hide();
            if(RenderUtility())return;
            if(daily.Active&&LookoutBusy){var rig=renderer.Reach;if(reachOverlay==null)reachOverlay=new SwordOverlay();Native.Present(Handle,reachOverlay.Present(DrawForm(r=>r.Reach.DrawLookout(lookoutFacing,daily.Pose.Scout)),Location,ClientSize,rig.Padding,TopMost),Left,Top);return;}
            if(reachOverlay!=null&&reachOverlay.Visible)reachOverlay.Hide();
            if(sword.Active){RenderSword();return;}
            if(casting.Active){RenderCast();return;}
            RenderFrame frame=new RenderFrame
            {
                Time=elapsed, Gait=gait, Speed=speed, MotionState=motion.State, Facing=motion.Facing, Angle=motion.Angle, Settle=motion.Settle, Dragging=dragging,
                LookX=lookX, LookY=lookY, Lean=lean, Blink=blink,
                RestAmount=rest.Sleep, Affection=rest.Affection, ClosedEyes=rest.Eyes,
                Nod=rest.Nod, PatSide=rest.Side, Carry=carryAmount, CarryEffort=carryEffort, CarryVertical=carryVertical,
                Greeting=greeting.Amount,WavePhase=greeting.Phase,WaveBlend=greetingWaveBlend,RunBlend=motion.RunBlend,
                Leaping=leap.Active || CarryLeapActive || CarryApproachAir,Leap=CarryApproachAir?carryApproachLeap.Pose(carryAge):CarryLeapActive?carryTravel.At(carryAge).Leap:leap.Pose,RunningCarry=CarryRunActive,
                LightTurn=motion.LightTurn,TurnProgress=motion.TurnProgress,TurnFrom=motion.TurnFrom,SupportNear=motion.SupportNear,
                Archery=archery.Active,BowPose=archery.Pose,Daily=daily.Active?daily.Pose:new DailyPose()
            };
            if(daily.Active){frame.RestAmount=frame.Affection=frame.ClosedEyes=frame.Nod=frame.Greeting=frame.Carry=0;frame.LookX=lookX*(1-frame.Daily.Think)-.2*frame.Daily.Think;frame.LookY=lookY*(1-frame.Daily.Think)+.18*frame.Daily.Think;frame.Lean=0;}
            Native.Present(Handle,RenderMagic(DrawForm(r=>r.Draw(frame))),Left,Top);
            RenderSigil();
            RenderCastFlight();

            FollowSpeech();
        }
    }
}
