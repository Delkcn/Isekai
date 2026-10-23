using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using JouchoPet;
class ArcheryStanceCheck {
 static int count;static string folder=Path.Combine("qa","v0643");
 static void Check(bool yes,string message){if(!yes)throw new Exception(message);count++;Console.WriteLine("PASS "+message);}
 static void Require(bool yes,string message){if(!yes)throw new Exception(message);}
 static int[] Read(Bitmap bitmap){var p=new int[bitmap.Width*bitmap.Height];var bits=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);try{for(int y=0;y<bitmap.Height;y++)Marshal.Copy(IntPtr.Add(bits.Scan0,y*bits.Stride),p,y*bitmap.Width,bitmap.Width);}finally{bitmap.UnlockBits(bits);}return p;}
 static double Difference(int[] a,int[] b){double sum=0;for(int i=0;i<a.Length;i++)for(int c=0;c<32;c+=8)sum+=Math.Abs(((uint)a[i]>>c&255)-((long)((uint)b[i]>>c&255)));return sum/(a.Length*4);}
 static int Lowest(int[] p,int width){for(int i=p.Length-1;i>=0;i--)if(((uint)p[i]>>24)>40)return i/width;return -1;}
 static void Main(){try {
  Directory.CreateDirectory(folder);AnimationParameters.UseFixedValues();
  foreach(int size in new[]{220,300,400})foreach(int direction in new[]{-1,1}) {
   Size c=PetRenderer.CanvasSize(size);
   using(var sides=new SideWalk(c.Width,c.Height,size))using(var neutral=(Bitmap)sides.Draw(0,0,0,direction,1).Clone())using(var arms=new CarryArmRig(direction<0?0:1,neutral,size))using(var plate=RigParts.BitmapOf(arms.DiagnosticBackplate,c.Width,c.Height))using(var stance=new ArcheryStance(c.Width,c.Height,size,direction,plate)) {
    int[] before=Read(plate),zero=Read(stance.Draw(0));int hip=(int)Math.Ceiling(28+size*(.98-SideWalk.LegHeightRatio));
    Check(Difference(before,zero)==0,"stance begins on the exact turn-end backplate "+size+" / "+direction);
    double last=-1,maxStep=0;int[] previous=before;
    for(int i=0;i<=120;i++) {
     double amount=i/120.0;int[] current=Read(stance.Draw(amount));
     for(int k=0;k<hip*c.Width;k++)Require(current[k]==before[k],"upper body or hair changed during stance");
     Require(Lowest(current,c.Width)==Lowest(before,c.Width),"planted foot moved vertically");
     double spread=Math.Abs(stance.DiagnosticFootShift(1,amount)-stance.DiagnosticFootShift(0,amount));
     Require(spread>=last,"stance expansion reversed");last=spread;
     maxStep=Math.Max(maxStep,Difference(previous,current));previous=current;
    }
    Check(Math.Abs(last-size*.09)<1e-8,"complete near/far feet gain nine percent height in separation "+size+" / "+direction);
    Check(maxStep<.22,"continuous stance and ground-locked feet; max mean channel step "+maxStep.ToString("F6")+" / "+size+" / "+direction);
    Check(Difference(Read(stance.DiagnosticLeg(0)),Read(stance.DiagnosticLeg(1)))>.15,"independent asymmetric leg textures remain distinct "+size+" / "+direction);
    Check(Difference(before,Read(stance.Draw(1e-8)))<.03,"no material pop at the exact neutral endpoint "+size+" / "+direction);
    Check(Difference(before,Read(stance.Draw(0)))==0,"return to neutral is exact after full expansion "+size+" / "+direction);
   }
  }
  ContactSheet();Console.WriteLine("All "+count+" archery stance checks passed.");
 }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
 static void ContactSheet(){int size=300;Size c=PetRenderer.CanvasSize(size);using(var sheet=new Bitmap(c.Width*5,c.Height*2+40))using(var g=Graphics.FromImage(sheet))using(var font=new Font("Segoe UI",10)) {
  g.Clear(Color.FromArgb(37,42,52));
  foreach(int direction in new[]{-1,1})using(var sides=new SideWalk(c.Width,c.Height,size))using(var neutral=(Bitmap)sides.Draw(0,0,0,direction,1).Clone())using(var arms=new CarryArmRig(direction<0?0:1,neutral,size))using(var plate=RigParts.BitmapOf(arms.DiagnosticBackplate,c.Width,c.Height))using(var stance=new ArcheryStance(c.Width,c.Height,size,direction,plate)) {
   int row=direction<0?0:1;for(int i=0;i<5;i++){g.DrawImageUnscaled(stance.Draw(i/4.0),i*c.Width,row*(c.Height+20));g.DrawString((direction<0?"Left":"Right")+" / "+(i/4.0).ToString("F2"),font,Brushes.White,i*c.Width+8,row*(c.Height+20)+c.Height);}
  }
  sheet.Save(Path.Combine(folder,"archery-stance-contact.png"),ImageFormat.Png);
 }}
}
