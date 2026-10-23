using System;
using System.Drawing;
namespace JouchoPet {
 // Bone-driven skins: thigh, shin and boot have separate bind transforms;
 // ankles/feet remain fixed while the pelvis lowers. Direction-specific skins
 // come from the existing leg registration, never a mirrored whole character.
 sealed class SlashBodyRig:IDisposable {
  readonly RigPatch[,] legs=new RigPatch[2,3];
  readonly SingleSkin skin;
  readonly int[] torso;
  readonly SlashSkeleton skeleton;
  internal SlashBodyRig(int w,int h,double size,int facing,Bitmap backplate,SlashSkeleton bones){
   skeleton=bones;skin=new SingleSkin(w,h);
   using(var layers=new ArcheryStance(w,h,size,facing,backplate)){
    torso=PoseMesh.Read(layers.ForegroundLayer);
    for(int part=0;part<2;part++){
     int[] source=PoseMesh.Read(layers.DiagnosticLeg(part));var split=new[]{new int[source.Length],new int[source.Length],new int[source.Length]};
     double knee=bones.BindKnee(part).Y,ankle=bones.BindAnkle(part).Y;
     for(int y=0;y<h;y++)for(int x=0;x<w;x++){int k=y*w+x;if(y<=knee+size*.008)split[0][k]=source[k];if(y>=knee-size*.008&&y<=ankle+size*.004)split[1][k]=source[k];if(y>=ankle-size*.004)split[2][k]=source[k];}
     for(int j=0;j<3;j++)legs[part,j]=new RigPatch(split[j],w,h);
    }
   }
  }
  void Leg(Graphics g,int part,SlashSkeleton.Leg p){
   RigParts.Segment(g,legs[part,0],skeleton.BindHip(part),skeleton.BindKnee(part),p.Hip,p.Knee);
   RigParts.Segment(g,legs[part,1],skeleton.BindKnee(part),skeleton.BindAnkle(part),p.Knee,p.Ankle);
   RigParts.Segment(g,legs[part,2],skeleton.BindAnkle(part),skeleton.BindFoot(part),p.Ankle,p.Foot);
  }
  internal void Draw(Graphics g,SwordPose p,SlashSkeleton.Pose bones){Leg(g,0,bones.Far);Leg(g,1,bones.Near);g.DrawImageUnscaled(skin.Draw(torso,v=>skeleton.SkinBody(v,p)),0,0);}
  public void Dispose(){skin.Dispose();foreach(var patch in legs)patch.Dispose();}
 }
}
