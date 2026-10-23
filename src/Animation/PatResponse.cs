using System;

namespace JouchoPet
{
    static class PatResponse
    {
        static double Smooth(double x)
        {
            x = Math.Max(0, Math.Min(1, x));
            return x * x * (3 - 2 * x);
        }

        public static double EyeClosure(double age)
        {
            if (age < 0 || age >= AnimationTuning.Pat.Duration) return 0;
            return Smooth((age - AnimationTuning.Pat.CloseDelay) / AnimationTuning.Pat.CloseDuration)
                * (1 - Smooth((age - AnimationTuning.Pat.ReopenAt) / AnimationTuning.Pat.ReopenDuration));
        }

        public static double Strength(double age)
        {
            if (age < 0 || age >= AnimationTuning.Pat.Duration) return 0;
            return Smooth(age / AnimationTuning.Pat.ResponseRise)
                * (1 - Smooth((age - AnimationTuning.Pat.ResponseFallAt) / AnimationTuning.Pat.ResponseFallDuration));
        }

        public static double Nod(double age)
        {
            return age > AnimationTuning.Pat.NodStart && age < AnimationTuning.Pat.NodEnd
                ? Math.Sin((age - AnimationTuning.Pat.NodStart) / AnimationTuning.Pat.NodDuration * Math.PI)
                    * AnimationTuning.Pat.NodAmount
                : 0;
        }
    }
}
