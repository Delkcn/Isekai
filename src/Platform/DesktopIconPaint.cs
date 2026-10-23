using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
namespace JouchoPet {
 // Shell position acknowledgement is not a paint acknowledgement. Flush each
 // small move to the desktop view before the pet advances to that location.
 static class DesktopIconPaint {
  [StructLayout(LayoutKind.Sequential)]
  struct RECT {
   public int Left,Top,Right,Bottom;
   public RECT(Rectangle r){Left=r.Left;Top=r.Top;Right=r.Right;Bottom=r.Bottom;}
  }
  internal static Rectangle Region(Point from,Point to,Point spacing,int iconSize) {
   int side=Math.Max(iconSize,spacing.X)/2+8;
   int below=Math.Max(iconSize,spacing.Y)+8;
   return Rectangle.FromLTRB(Math.Min(from.X,to.X)-side,Math.Min(from.Y,to.Y)-8,
    Math.Max(from.X,to.X)+iconSize+side,Math.Max(from.Y,to.Y)+below);
  }
  public static void Present(IntPtr window,Point from,Point to,Point spacing,int iconSize) {
   if(window==IntPtr.Zero)throw new InvalidOperationException("桌面视图已关闭，推动已暂停。");
   RECT area=new RECT(Region(from,to,spacing,iconSize));
   // RDW_INVALIDATE | RDW_ALLCHILDREN | RDW_UPDATENOW.
   // Target only the old/new icon and label area, not the whole desktop.
   // Do not erase the background separately, toggle redraw, or refresh Shell.
   if(!RedrawWindow(window,ref area,IntPtr.Zero,0x001|0x080|0x100))
    throw new Win32Exception(Marshal.GetLastWin32Error(),"无法刷新桌面图标的中间位置，推动已暂停。");
  }
  [DllImport("user32.dll",SetLastError=true)]
  static extern bool RedrawWindow(IntPtr hwnd,ref RECT rect,IntPtr region,uint flags);
 }
}
