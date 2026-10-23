using System;
using System.Drawing;

namespace JouchoPet
{
    // A frame is a snapshot of animation inputs, independent of the window API.
    struct RenderFrame
    {
        public double Time, Gait, Speed, LookX, LookY, Lean, Blink, PatSide, RestAmount, Affection, ClosedEyes, Nod, Carry;
        public string MotionState;
        public int Facing;
        public double Angle, Settle;
        public double CarryEffort, CarryVertical;
        public double Greeting, WavePhase, WaveBlend, RunBlend;
        public bool Dragging;
        public bool Leaping;
        public bool RunningCarry;
        public LeapPose Leap;
        public bool LightTurn,SupportNear;
        public double TurnProgress,TurnFrom;
        public bool Archery;
        public ArcheryPose BowPose;
    }

    // Owns sprite and rig lifetimes; the window only chooses when/where to draw.
    sealed class PetRenderer : IDisposable
    {
        readonly Bitmap[] sprites = SpriteAssets.LoadFront();
        static readonly double[] NeutralPose = { 1, 0, 0 };
        SoftRig front;
        SideWalk side;
        TurnRig turn;
        RestRig rest;
        ArcheryRig archer;
        SwordRig swordsman;
        internal int SwordPadding {get{return swordsman.Padding;}}
        internal Bitmap DrawSword(SwordPose pose,bool effects=true){return swordsman.Draw(pose,effects);}
        public double LastArcheryJacobian {get{return archer==null?1:archer.MinimumArea;}}

        public double LastRestJacobian { get { return rest==null?1:rest.LastMinimumJacobian; } }

        public bool Ready { get { return front != null; } }

        public static Size CanvasSize(int petHeight)
        {
            return new Size(
                (int)(petHeight * AnimationTuning.CanvasWidthRatio) + AnimationTuning.CanvasExtraWidth,
                petHeight + AnimationTuning.CanvasExtraHeight);
        }

        public void Resize(int petHeight)
        {
            Size canvas = CanvasSize(petHeight);
            SoftRig newFront = null;
            SideWalk newSide = null;
            TurnRig newTurn = null;
            RestRig newRest = null;
            ArcheryRig newArcher = null;
            SwordRig newSword = null;
            try
            {
                newFront = new SoftRig(sprites, canvas.Width, canvas.Height, petHeight);
                newSide = new SideWalk(canvas.Width, canvas.Height, petHeight);
                newRest = new RestRig(sprites, canvas.Width, canvas.Height, petHeight, newFront.Draw(0, 0, 0, 0, 0, 0, 0, NeutralPose));
                newTurn = new TurnRig(canvas.Width, canvas.Height, petHeight);
                newTurn.Prepare(newFront.Draw(0, 0, 0, 0, 0, 0, 0, NeutralPose), newSide);
                newArcher = new ArcheryRig(canvas.Width,canvas.Height,petHeight,newSide);
                newSword = new SwordRig(canvas.Width,canvas.Height,petHeight,newSide);
            }
            catch
            {
                if (newFront != null) newFront.Dispose();
                if (newSide != null) newSide.Dispose();
                if (newTurn != null) newTurn.Dispose();
                if (newRest != null) newRest.Dispose();
                if (newArcher != null) newArcher.Dispose();
                if (newSword != null) newSword.Dispose();
                throw;
            }
            DisposeRigs();
            swordsman=newSword;
            front = newFront;
            side = newSide;
            turn = newTurn;
            rest = newRest;
            archer = newArcher;
        }

        // Returned bitmap is borrowed and reused on the next frame. Do not dispose it.
        public Bitmap Draw(RenderFrame frame)
        {
            if (!Ready) throw new InvalidOperationException("Renderer has not been sized.");
            double affection = Math.Max(frame.Affection,frame.Greeting);
            double closedEyes = Math.Max(frame.ClosedEyes,Math.Min(1,frame.Greeting*1.2));
            double nod = frame.Nod;
            double quietBlink = Math.Max(frame.Blink, closedEyes);
            if(frame.Archery)return archer.Draw(frame.BowPose);

            if (frame.MotionState == "turn")
            {
                Bitmap neutral = front.Draw(0, 0, 0, 0, 0, 0, 0, NeutralPose);
                return frame.LightTurn?turn.DrawLight(frame.Angle,neutral,side,frame.Time,frame.TurnProgress,frame.TurnFrom,frame.SupportNear):turn.Draw(frame.Angle, neutral, side, frame.Time);
            }
            if (frame.Facing != 0)
            {
                if(frame.Leaping)return side.DrawLeap(frame.Facing,frame.Leap,frame.Carry,frame.CarryEffort,frame.CarryVertical);
                return side.Draw(frame.Time, frame.Gait, Math.Min(1, Math.Abs(frame.Speed) / 36),
                    frame.Facing, frame.Settle, frame.Carry, frame.CarryEffort, frame.CarryVertical, frame.RunBlend,frame.RunningCarry);
            }
            double awake = 1 - frame.RestAmount;
            double waveBlend=Math.Max(0,Math.Min(1,frame.WaveBlend));
            Bitmap posed = front.Draw(frame.Time, frame.Gait, 0, frame.LookX * awake,
                frame.LookY * awake + nod,
                frame.Lean * awake, quietBlink, NeutralPose,
                affection, frame.Greeting>frame.Affection?0:frame.PatSide, closedEyes,
                frame.Greeting*awake*(1-waveBlend),frame.WavePhase,frame.Greeting*awake*waveBlend);
            return rest.Draw(posed, frame.RestAmount);
        }

        void DisposeRigs()
        {
            if (front != null) front.Dispose();
            if (side != null) side.Dispose();
            if (turn != null) turn.Dispose();
            if (rest != null) rest.Dispose();
            if (archer != null) archer.Dispose();
            if (swordsman != null){swordsman.Dispose();swordsman=null;}
            front = null;
            side = null;
            turn = null;
            rest = null;
            archer = null;
        }

        public void Dispose()
        {
            DisposeRigs();
            foreach (Bitmap sprite in sprites) sprite.Dispose();
        }
    }
}
