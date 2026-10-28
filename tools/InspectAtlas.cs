using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
class InspectAtlas {
 static void Main(string[] args) {
  using(var b=new Bitmap(args[0])) {
   int w=b.Width,h=b.Height;var p=new int[w*h];var seen=new bool[p.Length];var queue=new int[p.Length];
   var d=b.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);Marshal.Copy(d.Scan0,p,0,p.Length);b.UnlockBits(d);
   Console.WriteLine(w+" x "+h);
   for(int seed=0;seed<p.Length;seed++) {
    if(seen[seed] || ((uint)p[seed]>>24)<80)continue;
    int read=0,n=1,l=w,r=0,t=h,bt=0;queue[0]=seed;seen[seed]=true;
    while(read<n){int k=queue[read++],x=k%w,y=k/w;l=Math.Min(l,x);r=Math.Max(r,x);t=Math.Min(t,y);bt=Math.Max(bt,y);
     foreach(int next in new[]{k-w,k+w,x>0?k-1:-1,x<w-1?k+1:-1})if(next>=0 && next<p.Length && !seen[next] && ((uint)p[next]>>24)>=80){seen[next]=true;queue[n++]=next;}}
    if(n>500)Console.WriteLine(n+" pixels "+l+","+t+" -> "+r+","+bt);
   }
  }
 }
}
