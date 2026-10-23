using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace JouchoPet
{
    static class SpriteAssets
    {
        // Caller owns every returned bitmap. Keep atlas coordinates here so the
        // application and diagnostic renderer always load exactly the same art.
        public static Bitmap[] LoadFront()
        {
            Bitmap[] sprites = new Bitmap[4];
            try
            {
                string[] names={"front","pat","crouch","sleep"};
                for(int i=0;i<sprites.Length;i++)sprites[i]=CharacterArt.Load(names[i]);
                return sprites;
            }
            catch
            {
                foreach (Bitmap sprite in sprites) if (sprite != null) sprite.Dispose();
                throw;
            }
        }
        // The sleeping cell touches a neighboring pose's hair in the atlas.
        // Remove small disconnected fragments, preserving all large islands.
        static void RemoveDetachedAtlasFragments(Bitmap image)
        {
            int w=image.Width,h=image.Height;var bounds=new Rectangle(0,0,w,h);
            int[] pixels=new int[w*h],labels=new int[w*h],queue=new int[w*h];
            var data=image.LockBits(bounds,ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
            try {
                Marshal.Copy(data.Scan0,pixels,0,pixels.Length);
                var counts=new System.Collections.Generic.List<int>();counts.Add(0);int largest=0;
                for(int seed=0;seed<pixels.Length;seed++) {
                    if(labels[seed]!=0 || ((uint)pixels[seed]>>24)<16)continue;
                    int id=counts.Count,read=0,write=1;queue[0]=seed;labels[seed]=id;
                    while(read<write) {
                        int k=queue[read++],x=k%w;
                        for(int direction=0;direction<4;direction++) {
                            int next=direction==0?k-w:direction==1?k+w:direction==2?k-1:k+1;
                            if(next<0 || next>=pixels.Length || direction==2 && x==0 || direction==3 && x==w-1)continue;
                            if(labels[next]==0 && ((uint)pixels[next]>>24)>=16){labels[next]=id;queue[write++]=next;}
                        }
                    }
                    counts.Add(write);largest=Math.Max(largest,write);
                }
                for(int i=0;i<pixels.Length;i++)if(labels[i]==0 || counts[labels[i]]<largest*.02)pixels[i]=0;
                Marshal.Copy(pixels,0,data.Scan0,pixels.Length);
            }finally{image.UnlockBits(data);}
        }
    }
}
