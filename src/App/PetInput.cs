using System;
using System.Drawing;
using System.Windows.Forms;
namespace JouchoPet
{
    sealed partial class Pet
    {
        void StartWalk(int facing,bool run=false)
        {CancelUtilityAction();
            CancelCarry();
            SetSleep(false);
            direction=facing;
            running=run;
            walking=true;
            wandering=true;
            wanderItem.Checked=true;
            stateUntil=DateTime.UtcNow.AddSeconds(12);
        }
        void HeadPat()
        {
            CancelDailyAction();CancelCast();CancelSword();CancelArchery();
            CancelLeap();
            greeting.Cancel();
            CancelCarry();
            sleeping=false;
            sleepItem.Checked=false;
            walking=false;
            rest.RequestPat(Cursor.Position.X<Left+Width/2?-1:1);
            Say("……嗯。是你的话，可以再待一会儿。");
            happyUntil=DateTime.UtcNow.AddSeconds(6);
            lastInput=DateTime.UtcNow;
            Render();
        }
        void SetSleep(bool value)
        {
            CancelDailyAction();CancelCast();CancelSword();CancelArchery();
            CancelLeap();
            bool changed=sleeping!=value;
            greeting.Cancel();
            CancelCarry();
            rest.RequestSleep(value);
            sleeping=value;
            if(changed)Say(value?"我稍微休息一下。你也别太勉强自己。":"醒了。刚才的话，我们接着说吧。");
            sleepItem.Checked=value;
            walking=false;
            happyUntil=DateTime.MinValue;
            lastInput=DateTime.UtcNow;
            Render();
        }
        void Greet(bool wave=false) {
            CancelDailyAction();CancelCast();CancelSword();CancelArchery();
            CancelLeap();
            greetingWaves=wave;
            CancelCarry();rest.RequestSleep(false);sleeping=false;sleepItem.Checked=false;walking=false;
            greeting.Request(wave);lastInput=DateTime.UtcNow;stateUntil=lastInput.AddSeconds(6);
            Say("嗨。看见你了。\n……今天也请多关照。");
        }
        void OnDown(object sender, MouseEventArgs e)
        {
            lastInput=DateTime.UtcNow;
            if(e.Button==MouseButtons.Right)
            {
                if((Control.ModifierKeys&Keys.Control)!=0){SelectForm(CurrentForm==CharacterForm.AnemoneI?CharacterForm.AnemoneII:CharacterForm.AnemoneI);return;}
                menu.Show(Cursor.Position);
                return;
            }
            if(e.Button!=MouseButtons.Left) return;
            CancelDailyAction();CancelCast();CancelSword();CancelArchery();
            CancelLeap();
            CancelCarry();
            greeting.Cancel();
            dragging=true;
            moved=false;
            previousCursor=Cursor.Position;
            dragStart=Cursor.Position;
            formStart=Location;
            Capture=true;
            walking=false;
        }
        void OnMove(object sender, MouseEventArgs e)
        {
            if(!dragging) return;
            Point p=Cursor.Position;
            int dx=p.X-dragStart.X,dy=p.Y-dragStart.Y;
            if(Math.Abs(dx)+Math.Abs(dy)>5) moved=true;
            if(moved)
            {
                Location=new Point(formStart.X+dx,formStart.Y+dy);
                dragLean=Math.Max(-1,Math.Min(1,(p.X-previousCursor.X)/25.0));
                previousCursor=p;
            }
        }
        void OnUp(object sender, MouseEventArgs e)
        {
            if(e.Button!=MouseButtons.Left || !dragging) return;
            dragging=false;
            Capture=false;
            if(moved)
            {
                Clamp();
                stateUntil=DateTime.UtcNow.AddSeconds(4);
            }
            else HeadPat();
        }
    }
}
