using System;
using System.Collections.Generic;
using System.Drawing;
namespace JouchoPet
{
    // Axis-aligned push planning in Explorer's desktop coordinates.
    static class IconCarryPlan
    {
        public static int PushDistance(int spacing,bool aligned,bool running,int cells) {
            if(spacing<=0)throw new ArgumentOutOfRangeException("spacing");
            if(cells<1 || cells>6)throw new ArgumentOutOfRangeException("cells");
            // A cell always means Explorer's actual spacing, even with snap off.
            return running?checked(spacing*cells):aligned?spacing:Math.Max(72,Math.Min(140,spacing));
        }
        // Lower the pet by 20% of character height relative to the old 60% anchor.
        public static int PetTop(int iconCenterY,int petHeight) {
            return iconCenterY-(int)(28+petHeight*.60)+(int)Math.Round(petHeight*.20);
        }
        public static bool Clear(Point from,Point to,string id,List<DesktopIcons.Item> items,Point spacing) {
            int left=Math.Min(from.X,to.X),right=Math.Max(from.X,to.X),top=Math.Min(from.Y,to.Y),bottom=Math.Max(from.Y,to.Y);
            foreach(var other in items)if(other.Id!=id && other.Position.X>left-spacing.X*.8 && other.Position.X<right+spacing.X*.8 && other.Position.Y>top-spacing.Y*.8 && other.Position.Y<bottom+spacing.Y*.8)return false;
            return true;
        }
        public static double Distance(Point from,Point to) {double x=to.X-from.X,y=to.Y-from.Y;return Math.Sqrt(x*x+y*y);}
        // Peak velocity of the quintic curve is 1.875 * average velocity.
        public static double Duration(Point from,Point to) {return Math.Max(1,Distance(from,to)/24);}
        public static Point Position(Point from,Point to,double seconds) {
            double t=Smooth(seconds/Duration(from,to));
            return new Point(from.X+(int)Math.Round((to.X-from.X)*t),from.Y+(int)Math.Round((to.Y-from.Y)*t));
        }
        public static double Smooth(double x) {x=Math.Max(0,Math.Min(1,x));return x*x*x*(x*(x*6-15)+10);}
    }
}
