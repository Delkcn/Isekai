using System;
using System.Drawing;
using System.Runtime.InteropServices;
namespace JouchoPet
{
    static class Native
    {
        [StructLayout(LayoutKind.Sequential)] struct POINT
        {
            public int x,y;
            public POINT(int a,int b)
            {
                x=a;
                y=b;
            }
        }
        [StructLayout(LayoutKind.Sequential)] struct SIZE
        {
            public int cx,cy;
            public SIZE(int a,int b)
            {
                cx=a;
                cy=b;
            }
        }
        [StructLayout(LayoutKind.Sequential,Pack=1)] struct BLEND
        {
            public byte op,flags,alpha,format;
        }
        [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr h);
        [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr h,IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
        [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);
        [DllImport("user32.dll",SetLastError=true)] static extern bool UpdateLayeredWindow(IntPtr hwnd,IntPtr dc,ref POINT pos,ref SIZE size,IntPtr src,ref POINT origin,int key,ref BLEND blend,int flags);
        public static void Present(IntPtr hwnd,Bitmap bitmap,int x,int y)
        {
            IntPtr dc=GetDC(IntPtr.Zero),mem=IntPtr.Zero,hbitmap=IntPtr.Zero,old=IntPtr.Zero;
            try
            {
                mem=CreateCompatibleDC(dc);
                hbitmap=bitmap.GetHbitmap(Color.FromArgb(0));
                old=SelectObject(mem,hbitmap);
                POINT pos=new POINT(x,y),origin=new POINT(0,0);
                SIZE size=new SIZE(bitmap.Width,bitmap.Height);
                BLEND blend=new BLEND
                {
                    op=0,flags=0,alpha=255,format=1
                };
                if(!UpdateLayeredWindow(hwnd,dc,ref pos,ref size,mem,ref origin,0,ref blend,2)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if(old!=IntPtr.Zero) SelectObject(mem,old);
                if(hbitmap!=IntPtr.Zero) DeleteObject(hbitmap);
                if(mem!=IntPtr.Zero) DeleteDC(mem);
                if(dc!=IntPtr.Zero) ReleaseDC(IntPtr.Zero,dc);
            }
        }
    }
}
