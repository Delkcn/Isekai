using System;

namespace JouchoPet
{
    // Targets may change at any point; position AND velocity survive interruption.
    sealed class RestMotion
    {
        sealed class Channel
        {
            public double Value, Velocity;
            public void Step(double target, double rate, double dt)
            {
                double offset = Value - target;
                double tangent = Velocity + rate * offset;
                double decay = Math.Exp(-rate * dt);
                Value = target + (offset + tangent * dt) * decay;
                Velocity = (Velocity - rate * tangent * dt) * decay;
            }
        }

        readonly Channel sleep = new Channel(), affection = new Channel(), eyes = new Channel(), nod = new Channel(), side = new Channel();
        bool sleepRequested, pendingPat;
        double patAge = 100, requestedSide = 1;
        public double Sleep { get { return sleep.Value; } }
        public double Affection { get { return affection.Value; } }
        public double Eyes { get { return eyes.Value; } }
        public double Nod { get { return nod.Value; } }
        public double Side { get { return side.Value; } }
        public bool BlocksWalking
        {
            get { return sleepRequested || pendingPat || patAge < AnimationTuning.Pat.Duration
                || Sleep > 0.001 || Affection > 0.001 || Eyes > 0.001 || Math.Abs(Nod) > 0.001; }
        }

        public void RequestSleep(bool value)
        {
            sleepRequested = value;
            pendingPat = false;
            patAge = 100;
        }

        public void RequestPat(double direction)
        {
            sleepRequested = false;
            pendingPat = true;
            requestedSide = direction < 0 ? -1 : 1;
            // Do not reset any visible channel, even for rapid repeated touches.
        }

        public void Update(double dt, bool frontReady)
        {
            if (dt <= 0) return;
            if (pendingPat && frontReady && Sleep < 0.025)
            {
                pendingPat = false;
                patAge = Affection > 0.15 ? AnimationTuning.Pat.ResponseRise : 0;
            }
            else patAge += dt;
            sleep.Step(sleepRequested && frontReady ? 1 : 0, AnimationTuning.Rest.SleepRate, dt);
            double strength = frontReady ? PatResponse.Strength(patAge) : 0;
            affection.Step(strength, AnimationTuning.Rest.ExpressionRate, dt);
            // Sleep-to-pat keeps the lids closed until the acknowledgement starts.
            double closed = Math.Max(Sleep, pendingPat && frontReady ? 1 : PatResponse.EyeClosure(patAge));
            eyes.Step(closed, AnimationTuning.Rest.ExpressionRate, dt);
            nod.Step(frontReady ? PatResponse.Nod(patAge) : 0, AnimationTuning.Rest.ExpressionRate, dt);
            side.Step(requestedSide, AnimationTuning.Rest.ExpressionRate, dt);
        }
    }
}
