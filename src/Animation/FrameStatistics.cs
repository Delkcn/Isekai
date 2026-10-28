using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
namespace JouchoPet {
 sealed class FrameStatistics {
  readonly List<double> gaps=new List<double>();
  readonly List<double> turnGaps=new List<double>();
  bool previousTurning;
  double previous=-1,totalRender;
  int frames;
  public void Record(double now,double renderMilliseconds,bool turning=false) {
   if(previous>=0 && now-previous<.5){gaps.Add((now-previous)*1000);if(turning&&previousTurning)turnGaps.Add((now-previous)*1000);}
   previousTurning=turning;
   previous=now;totalRender+=renderMilliseconds;frames++;
   if(gaps.Count>18000)gaps.RemoveRange(0,9000);
   if(turnGaps.Count>18000)turnGaps.RemoveRange(0,9000);
  }
  public string Report() {
   if(gaps.Count==0)return "尚未收集到帧间隔。";
   double[] sorted=gaps.ToArray();Array.Sort(sorted);double sum=0;int slow=0;
   foreach(double n in sorted){sum+=n;if(n>25)slow++;}
   double turnSum=0;foreach(double gap in turnGaps)turnSum+=gap;
   string turnReport=turnGaps.Count==0?"Turn cadence: no samples yet.\r\n":string.Format(CultureInfo.InvariantCulture,"Turn cadence: {0:F1} fps ({1} intervals).\r\n",1000*turnGaps.Count/turnSum,turnGaps.Count);
   return string.Format(CultureInfo.InvariantCulture,
    "Samples: {0}\r\nAverage display cadence: {1:F1} fps\r\nFrame interval p50 / p95: {2:F2} / {3:F2} ms\r\nIntervals above 25ms: {4}\r\nMean rendering + present: {5:F2} ms\r\n",
    sorted.Length,1000*sorted.Length/sum,sorted[sorted.Length/2],sorted[(int)((sorted.Length-1)*.95)],slow,totalRender/Math.Max(1,frames))+turnReport+"Targets: turns 120 fps; other actions 60 fps. Actual cadence depends on rendering and display refresh. Menu/drag/stall time may be included.\r\n";
  }
  public string Save() {
   string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"IsekaijouchoPet");
   Directory.CreateDirectory(dir);string path=Path.Combine(dir,"animation-performance.txt");File.WriteAllText(path,Report());return path;
  }
 }
}
