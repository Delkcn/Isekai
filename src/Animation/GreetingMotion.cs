using System;
namespace JouchoPet {
 sealed class GreetingMotion {
  bool pending;double age=100,returnRate=7;
  public double Amount {get;private set;}
  public double Phase {get;private set;}
  public bool Active {get{return pending || age<3.6 || Amount>.001;}}
  public void Request(bool wave=false){pending=true;returnRate=wave?7*.70:7;}
  public void Cancel(){pending=false;age=100;}
  public void Update(double dt,bool frontReady) {
   if(dt<=0)return;
   if(pending && frontReady){pending=false;age=0;}
   if(!pending)age+=dt;
   if(age<3.6)Phase+=dt*Math.PI*3.6;
   double target=frontReady && age<2.8?1:0;
   double rate=target<Amount?returnRate:7;
   Amount+=(target-Amount)*(1-Math.Exp(-dt*rate));
   if(Amount<.0001 && !pending && age>=3.6)Amount=0;
  }
 }
}
