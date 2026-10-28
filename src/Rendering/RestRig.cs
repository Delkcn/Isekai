using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace JouchoPet {
 sealed class RestRig : IDisposable {
  readonly PoseMesh mesh;
  readonly int count;
  public double LastMinimumJacobian=1;
  internal PoseMesh.Pose[] DiagnosticKeys {get{return mesh.DiagnosticKeys;}}
  public RestRig(Bitmap[] original,int w,int h,int size,Bitmap standing,CharacterForm form=CharacterForm.AnemoneI) {
   count=9;var keys=new PoseMesh.Pose[count];keys[0]=CharacterArt.Register(standing,"front",size);
   using(Bitmap sheet=AssetStore.Image("v067/sleep-keys-eye.png",form))for(int i=1;i<8;i++)keys[i]=SleepArt.Key(sheet,i,w,h,size,form);
   using(Bitmap final=CharacterArt.Normalized("sleep",w,h,size,form))keys[8]=CharacterArt.Register(final,"sleep",size);
   // Finish with the same folded-arm drawing used at rest. The old sheet's
   // last drawing had a different forearm, causing a late arm replacement.
   keys[7]=keys[8];
   mesh=new PoseMesh(keys,w,h,true);
  }
  public Bitmap Draw(Bitmap standing,double amount) {
   amount=Math.Max(0,Math.Min(1,amount));if(amount<.00001)return standing;
   if(amount<1.0/(count-1))mesh.UpdateFirst(standing);
   Bitmap result=mesh.Draw(amount*(count-1));LastMinimumJacobian=mesh.MinimumArea;return result;
  }
  public void Dispose(){if(mesh!=null)mesh.Dispose();}
 }
}
