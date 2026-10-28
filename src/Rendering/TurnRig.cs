using System;
using System.Drawing;
namespace JouchoPet {
 sealed class TurnRig : IDisposable {
  readonly CharacterForm form; readonly int width,height,size;SkeletalTurn model;
  public double LastMinimumJacobian=1;
  public int MaterialCount {get{return model==null?0:model.MaterialCount;}}
  public TurnRig(int w,int h,int petHeight,CharacterForm form=CharacterForm.AnemoneI){this.form=form;width=w;height=h;size=petHeight;}
  public void Prepare(Bitmap front,SideWalk sides) {
   if(model!=null)return;
   var views=new Bitmap[5];
   try {
    views[0]=(Bitmap)sides.Draw(0,0,0,-1,1).Clone();views[2]=(Bitmap)front.Clone();views[4]=(Bitmap)sides.Draw(0,0,0,1,1).Clone();
    using(Bitmap left=AssetStore.TurnView("left",form))views[1]=DrawnFrames.Normalize(left,width,height,size,-1);
    using(Bitmap right=AssetStore.TurnView("right",form))views[3]=DrawnFrames.Normalize(right,width,height,size,-1);
    model=new SkeletalTurn(views,width,height,size,form);
   }finally{foreach(Bitmap view in views)if(view!=null)view.Dispose();}
  }
  public Bitmap Draw(double angle,Bitmap front,SideWalk sides,double time) {
   LastMinimumJacobian=1;
   if(Math.Abs(angle)<.0000001)return front;
   if(angle<=-1)return sides.Draw(time,0,0,-1,1);
   if(angle>=1)return sides.Draw(time,0,0,1,1);
   Prepare(front,sides);Bitmap result=model.Draw(angle);LastMinimumJacobian=model.MinimumArea;return result;
  }
  public void DrawSkeleton(Graphics g,double angle){if(model!=null)model.DrawSkeleton(g,angle);}
  public Bitmap DrawLight(double angle,Bitmap front,SideWalk sides,double time,double progress,double fromAngle,bool supportNear) {
   if(progress<=0 || progress>=1)return Draw(angle,front,sides,time);
   Prepare(front,sides);Bitmap result=model.Draw(angle,progress,fromAngle,supportNear);LastMinimumJacobian=model.MinimumArea;return result;
  }
  public void Dispose(){if(model!=null)model.Dispose();}
 }
}
