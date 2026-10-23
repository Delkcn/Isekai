using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace JouchoPet {
 // Shared rest-length binding for the existing painted hair. Body/face pixels
 // stay in their original coordinates; only the separately masked rear hair
 // receives this load-time bone transform, before any action's animation.
 static class HairLength {
  internal static bool Enabled=true; // diagnostic baseline switch, not a user setting
  internal const double LowerLength=.52;
  // Source-space body silhouettes: y, left, right. These are independent
  // registrations, never a mirrored mask for the asymmetric costume.
  static double[] Body(string name){switch(name){
   case "profile-left":return new double[]{161,148,181,190,138,190,230,134,194,270,124,201,290,120,205,310,113,211,330,101,220,345,94,230,356,105,234,368,121,217,378,136,199,386,137,190,455,102,214};
   case "left":return new double[]{161,478,520,190,471,526,230,465,534,270,456,541,290,449,544,310,442,554,330,432,562,350,423,569,358,433,572,368,455,553,378,474,534,385,474,522,460,431,554};
   case "front":return new double[]{159,861,914,190,850,923,210,838,927,230,833,932,250,826,938,270,815,946,290,821,950,310,824,949,330,818,956,350,809,965,362,807,968,372,824,954,382,847,939,390,850,922,465,842,954};
   case "right":return new double[]{162,1245,1292,190,1238,1295,210,1232,1298,230,1223,1299,250,1221,1301,270,1218,1308,290,1215,1314,310,1215,1321,330,1207,1326,350,1199,1332,362,1195,1334,375,1192,1313,385,1208,1298,395,1232,1290,465,1207,1320};
   case "profile-right":return new double[]{161,1609,1651,190,1595,1650,230,1588,1653,250,1586,1655,270,1579,1663,290,1568,1668,310,1564,1673,330,1556,1679,343,1551,1688,350,1548,1681,370,1543,1657,378,1541,1643,385,1550,1639,393,1575,1636,400,1586,1640,413,1588,1648,455,1575,1674};
   case "push-left":return new double[]{610,35,196,630,33,199,640,90,201,650,162,202,660,165,209,680,158,230,690,148,239,710,124,259,730,96,281,741,87,291,750,100,300,764,120,310,771,133,306,780,145,283,800,140,290,820,100,308,875,90,316};
   case "push-right":return new double[]{616,545,735,640,548,736,650,545,603,665,536,598,685,512,598,705,499,604,715,492,619,725,484,632,735,475,637,745,465,637,765,447,630,775,449,628,785,462,626,800,477,625,820,454,640,840,440,650,865,440,638};
   case "crouch":return new double[]{662,899,951,680,875,943,700,867,950,720,850,958,740,853,963,760,855,970,780,845,980,800,820,989,810,815,985,825,815,1001,842,827,977,855,835,980};
   case "sleep":return new double[]{693,1194,1294,710,1188,1302,730,1187,1303,740,1191,1300,760,1191,1319,780,1176,1330,800,1160,1343,809,1152,1356,815,1167,1324,825,1159,1345,833,1158,1320,840,1158,1316,846,1160,1303,850,1185,1240,855,1200,1230,861,1210,1220};
   case "pat":return new double[]{606,1582,1635,620,1575,1634,640,1570,1638,660,1560,1643,680,1552,1650,700,1549,1657,720,1541,1662,740,1545,1656,760,1536,1660,775,1529,1667,785,1528,1655,797,1547,1637,810,1570,1634,875,1550,1680};
   default:throw new ArgumentException("Missing hair registration: "+name);
  }}
  static double[] SleepBody(int i){switch(i){
   case 1:return new double[]{214,649,705,230,636,716,250,630,719,270,624,727,290,619,736,310,622,742,330,610,749,345,607,756,355,606,754,367,609,760,385,602,765,397,600,770,414,595,730,421,612,721,435,631,727,442,640,727,450,646,730,487,637,730};
   case 2:return new double[]{251,1085,1131,270,1073,1147,290,1062,1157,310,1059,1163,330,1060,1168,350,1051,1175,370,1048,1182,390,1042,1188,410,1032,1195,426,1027,1201,432,1038,1184,442,1060,1156,451,1068,1157,487,1060,1159};
   case 3:return new double[]{303,1512,1592,320,1502,1603,340,1493,1610,360,1480,1616,380,1468,1627,400,1453,1636,420,1440,1647,435,1430,1632,445,1426,1620,455,1421,1605,465,1416,1596,473,1432,1594,476,1455,1594,480,1528,1594,487,1528,1594};
   case 4:return new double[]{699,197,303,715,190,311,735,176,317,755,174,319,775,170,319,795,150,332,815,140,340,835,140,340,850,162,339,860,158,338,869,166,338,875,225,305};
   case 5:return new double[]{704,633,746,720,625,750,740,612,753,760,611,748,780,602,767,800,583,781,815,580,789,825,580,785,840,596,791,855,580,780,864,581,773,870,655,716,887,655,716};
   case 6:return new double[]{711,1043,1151,725,1033,1160,740,1035,1169,755,1042,1170,775,1036,1180,795,1029,1190,815,1021,1205,825,1018,1214,835,1015,1175,845,1006,1188,856,1015,1172,861,1028,1160,875,1055,1125};
   default:return new double[]{737,1483,1600,780,1464,1635,830,1420,1660,887,1410,1670};
  }}

  static double[][] Holes(string name){switch(name){
   case "sleep":return new[]{new double[]{1130,809,1154,809,1181,800,1178,813,1168,819,1161,836,1145,840},new double[]{1301,815,1319,820,1338,817,1347,826,1328,832,1318,829,1320,839,1315,845,1300,844,1300,831}};
   case "crouch":return new[]{new double[]{810,801,836,783,851,763,853,769,848,790,838,806,822,815,814,820},new double[]{939,820,958,820,963,827,955,831,941,834},new double[]{976,806,1001,809,1004,819,986,815,978,812},new double[]{847,827,856,837,870,839,877,835,875,847,853,850}};
   case "front":return new[]{new double[]{881,381,890,385,895,408,885,416,881,402}};
   case "left":return new[]{new double[]{497,389,506,382,510,407,504,422,497,425}};
   case "right":return new[]{new double[]{1262,391,1270,393,1275,427,1268,431}};
   case "pat":return new[]{new double[]{1604,800,1612,799,1617,834,1607,842}};
   case "push-left":return new[]{new double[]{173,775,209,784,227,780,248,796,253,812,237,824,203,809,179,805}};
   case "push-right":return new[]{new double[]{529,779,568,768,591,755,602,783,602,819,569,835,520,833,505,801}};
   case "sleep1":return new[]{new double[]{675,378,681,386,686,412,693,434,687,451,681,439},new double[]{624,429,639,440,646,444,646,452,625,453}};
   case "sleep2":return new[]{new double[]{1107,405,1113,417,1116,434,1123,451,1117,459,1110,448}};
   case "sleep3":return new[]{new double[]{1430,426,1453,398,1473,368,1470,385,1458,411,1450,435,1430,452,1424,448},new double[]{1480,460,1494,465,1508,460,1528,461,1528,482,1492,482}};
   case "sleep4":return new[]{new double[]{140,801,166,780,181,771,179,786,167,802,149,822,140,827},new double[]{140,844,159,852,160,867,140,869},new double[]{288,834,305,838,318,846,312,855,286,855},new double[]{197,851,204,861,222,863,225,874,191,874,191,865}};
   case "sleep5":return new[]{new double[]{579,810,597,792,615,778,611,797,594,813,580,823},new double[]{719,836,738,837,756,845,746,854,719,855},new double[]{618,847,627,859,638,865,653,865,653,875,612,875}};
   case "sleep6":return new[]{new double[]{1164,833,1186,840,1211,835,1216,843,1198,852,1207,858,1192,867,1175,867,1180,851,1165,845}};
   default:return new double[0][];
  }}
  static bool Inside(double[] polygon,double x,double y){bool inside=false;for(int i=0,j=polygon.Length-2;i<polygon.Length;j=i,i+=2){double ax=polygon[i],ay=polygon[i+1],bx=polygon[j],by=polygon[j+1];if((ay>y)!=(by>y)&&x<(bx-ax)*(y-ay)/(by-ay)+ax)inside=!inside;}return inside;}
  static bool Foreground(double[] body,double[][] holes,double x,double y){double inset=Math.Max(0,Math.Min(1.5,(y-body[0]-55)/15));if(x<Edge(body,y,1)+inset||x>Edge(body,y,2)-inset)return false;foreach(var hole in holes)if(Inside(hole,x,y))return false;return true;}
  static int Coverage(int pixel,double amount){uint v=(uint)pixel;return (int)(((uint)Math.Round((v>>24)*amount)<<24)|((uint)Math.Round(((v>>16)&255)*amount)<<16)|((uint)Math.Round(((v>>8)&255)*amount)<<8)|(uint)Math.Round((v&255)*amount));}
  static double Edge(double[] bands,double y,int side){int i=0;while(i<bands.Length-6&&y>bands[i+3])i+=3;double t=Math.Max(0,Math.Min(1,(y-bands[i])/(bands[i+3]-bands[i])));return bands[i+side]+(bands[i+3+side]-bands[i+side])*t;}
  internal static void Apply(Bitmap picture,CharacterArt.Pose pose,double size){
   double scale=CharacterArt.Scale(pose,size),neck=CharacterArt.Top(pose)+CharacterArt.HeadToNeck;
   Apply(picture,size,28+size*neck,Body(pose.Name),Holes(pose.Name),
    x=>pose.Anchor+(x-picture.Width*.5)/scale,
    y=>pose.Neck+(y-28-size*neck)*(pose.Sole-pose.Neck)/(size*(.98-neck)));
  }
  internal static void ApplySleep(Bitmap picture,int index,double size,double top,double neckSource,double crown,double sole,double anchor){
   double neck=top+CharacterArt.HeadToNeck,scale=size*CharacterArt.HeadToNeck/(neckSource-crown);
   Apply(picture,size,28+size*neck,SleepBody(index),Holes("sleep"+index),x=>anchor+(x-picture.Width*.5)/scale,y=>neckSource+(y-28-size*neck)*(sole-neckSource)/(size*(.98-neck)));
  }
  internal static int[] ReadRaw(Bitmap picture){
   int w=picture.Width,h=picture.Height;var pixels=new int[w*h];
   var data=picture.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);
   try{for(int y=0;y<h;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),pixels,y*w,w);}finally{picture.UnlockBits(data);}
   return pixels;
  }
  static void Apply(Bitmap picture,double size,double neck,double[] body,double[][] holes,Func<double,double> sourceX,Func<double,double> sourceY){
   if(!Enabled)return;
   int w=picture.Width,h=picture.Height;var original=ReadRaw(picture);var hair=new int[original.Length];var foreground=(int[])original.Clone();
   var protectedPixel=new bool[original.Length];
   for(int y=(int)Math.Floor(neck);y<h;y++)for(int x=0;x<w;x++){
    int k=y*w+x;if(original[k]==0)continue;
    int covered=0;for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++)
     if(Foreground(body,holes,sourceX(x+(dx-.5)*.5),sourceY(y+(dy-.5)*.5)))covered++;
    double amount=covered/4.0;protectedPixel[k]=covered==4;
    foreground[k]=Coverage(original[k],amount);hair[k]=Coverage(original[k],1-amount);
   }
   using(var skin=new SingleSkin(w,h))using(var front=RigParts.BitmapOf(foreground,w,h))using(var g=Graphics.FromImage(picture)){
    g.Clear(Color.Transparent);
    g.DrawImageUnscaled(skin.Draw(hair,p=>{
     double d=Math.Max(0,p.Y-neck),blend=LeapMotion.Ease(d/(size*.045));
     return new PointF((float)(w*.5+(p.X-w*.5)*(1-.16*blend)),(float)(p.Y-d*(1-LowerLength)*blend));
    }),0,0);
    g.DrawImageUnscaled(front,0,0);
   }
   // Restore protected painted surfaces verbatim after compositing. This also
   // prevents resampling a semi-transparent neck edge into the face region.
   var data=picture.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadWrite,PixelFormat.Format32bppPArgb);
   try{var row=new int[w];for(int y=0;y<h;y++){
    Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),row,0,w);
    for(int x=0;x<w;x++)if(y<Math.Floor(neck)||protectedPixel[y*w+x])row[x]=original[y*w+x];
    Marshal.Copy(row,0,IntPtr.Add(data.Scan0,y*data.Stride),w);
   }}finally{picture.UnlockBits(data);}
  }
 }
}
