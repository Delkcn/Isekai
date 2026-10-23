using System;
namespace JouchoPet {
 // Phase-domain following response: forearm and wrist trail the driving upper
 // arm, then catch up after reversal. Stateless sampling preserves loop seams
 // and frame-rate independence; this is an artistic inertia model, not rigid-body physics.
 struct RunArmPose {
  public double Upper,Lower,Wrist;
  static double Drive(double phase){return AnimationParameters.Get("armSwing")*RunGait.ArmSwing(phase)-4;}
  public static RunArmPose At(double phase) {
   double delay=AnimationParameters.Get("armDelay"),limit=AnimationParameters.Get("wristLimit");
   double now=Drive(phase),recent=Drive(phase-.045*delay),late=Drive(phase-.085*delay),older=Drive(phase-.14*delay);
   return new RunArmPose {
    Upper=now,
    Lower=-AnimationParameters.Get("elbowBend")+AnimationParameters.Get("forearmFollow")*(recent-now),
    Wrist=Math.Max(-limit,Math.Min(limit,AnimationParameters.Get("wristFollow")*(.55*(late-now)+.15*(older-late))))
   };
  }
 }
}
