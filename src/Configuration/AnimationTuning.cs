namespace JouchoPet
{
    // Units are explicit. Rest transitions added in v0.5.16.
    // Approved gait geometry remains in SideWalk's existing public constants.
    static class AnimationTuning
    {
        public const int DefaultHeight = 300;
        public const double CanvasWidthRatio = 1.05; // room for approved long hair without horizontal squeezing
        public const int CanvasExtraWidth = 32;
        public const int CanvasExtraHeight = 48;

        public static class Pat
        {
            public const double Duration = 3.2;
            public const double CloseDelay = 0.15;
            public const double CloseDuration = 0.35;
            public const double ReopenAt = 2.05;
            public const double ReopenDuration = 0.8;
            public const double ResponseRise = 0.55;
            public const double ResponseFallAt = 1.65;
            public const double ResponseFallDuration = 1.55;
            public const double NodStart = 0.65;
            public const double NodEnd = 1.35;
            public const double NodDuration = 0.70;
            public const double NodAmount = 0.6;
        }

        public static class Rest
        {
            public const double SleepRate = 3.4; // restored v0.6.34 sleep/wake timing
            public const double ExpressionRate = 12;
        }

    }
}
