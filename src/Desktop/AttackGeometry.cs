using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace JouchoPet {
 static class AttackGeometry {
  internal static List<DesktopAttackFile> CrossFiles(List<DesktopAttackFile> files,PointF center,double span,double size){
   var selected=new List<DesktopAttackFile>();
   using(var region=new Region()){
    region.MakeEmpty();
    for(int ray=0;ray<4;ray++)using(var blade=MiSigilArt.Blade((float)(span*.5),(float)(MiSigilArt.ArmWidth(size)*2)))using(var matrix=new Matrix()){
     matrix.Rotate(ray*90);blade.Transform(matrix);using(var move=new Matrix()){move.Translate(center.X,center.Y);blade.Transform(move);}region.Union(blade);
    }
    foreach(var file in files)if(region.IsVisible(file.Bounds))selected.Add(file);
   }return selected;
  }
  // Filled blade polygons plus the strips joining each edge prevent a fast swing
  // from tunnelling through an icon between frames. No target-point radius test.
  internal static DesktopAttackFile SwordFile(List<DesktopAttackFile> files,Func<double,PointF[]> blade,double from,double to){
   double start=Math.Max(SwordMotion.StrikeStart,from),end=Math.Min(SwordMotion.StrikeEnd,to);
   if(end<start||to<SwordMotion.StrikeStart||from>=SwordMotion.StrikeEnd)return null;
   var previous=blade(start);int steps=Math.Max(1,(int)Math.Ceiling((end-start)*480));
   for(int step=1;step<=steps;step++){
    var current=blade(start+(end-start)*step/steps);
    using(var region=new Region()){
     region.MakeEmpty();using(var p=new GraphicsPath()){p.AddPolygon(previous);region.Union(p);p.Reset();p.AddPolygon(current);region.Union(p);}
     for(int i=0;i<current.Length;i++)using(var strip=new GraphicsPath()){int j=(i+1)%current.Length;strip.AddPolygon(new[]{previous[i],previous[j],current[j],current[i]});region.Union(strip);}
     DesktopAttackFile hit=null;double nearest=Double.MaxValue;
     foreach(var file in files)if(region.IsVisible(file.Bounds)){
      var center=DesktopFileTarget.Center(file.Bounds);double distance=RigParts.Length(center,current[2]);
      if(distance<nearest||(distance==nearest&&hit!=null&&String.CompareOrdinal(file.Path,hit.Path)<0)){hit=file;nearest=distance;}
     }
     if(hit!=null)return hit;
    }previous=current;
   }return null;
  }
 }
 sealed class SwordFileStrike {
  bool consumed=true;List<DesktopAttackFile> files;
  internal void Begin(){consumed=false;files=null;}
  internal void Cancel(){consumed=true;files=null;}
  internal ProgramLaunchResult Update(IDesktopAttackFiles desktop,Func<double,PointF[]> blade,double from,double to){
   if(consumed||to<SwordMotion.StrikeStart||from>=SwordMotion.StrikeEnd||to<=from)return null;
   try{
    if(files==null)files=desktop.Read();var file=AttackGeometry.SwordFile(files,blade,from,to);
    if(file==null){if(to>=SwordMotion.StrikeEnd)Cancel();return null;}
    Cancel();return desktop.Open(file);
   }catch(Exception ex){Cancel();return new ProgramLaunchResult(false,ex.Message);}
  }
 }
}
