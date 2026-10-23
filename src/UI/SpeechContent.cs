using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
namespace JouchoPet {
 // Text-only state: no network requests, markup interpretation or file operations.
 sealed class SpeechContent {
  public readonly List<string> Pages;
  int page;double revealed,reading;
  int[] elements;
  public bool Closed {get;private set;}
  public int Page {get{return page;}}
  public bool Complete {get{return revealed>=elements.Length;}}
  public string FullPage {get{return Pages[page];}}
  public string VisibleText {get{int n=(int)revealed;return n>=elements.Length?FullPage:FullPage.Substring(0,elements[n]);}}
  public SpeechContent(string text,Func<string,bool> fits,int linesPerPage) {
   if(linesPerPage<1)throw new ArgumentOutOfRangeException("linesPerPage");
   Pages=new List<string>();var lines=new List<string>();var line=new StringBuilder();
   var e=StringInfo.GetTextElementEnumerator((text??"").Replace("\r\n","\n").Replace('\r','\n'));
   while(e.MoveNext()) {
    string token=e.GetTextElement();
    if(token=="\n"){lines.Add(line.ToString());line.Clear();continue;}
    if(line.Length>0 && !fits(line.ToString()+token)) {
     string current=line.ToString();int[] positions=StringInfo.ParseCombiningCharacters(current);
     // Keep closing punctuation with its preceding character, not alone on a line.
     if(positions.Length>1 && ",.!?:;，。！？：；、…）】」』％%”’".Contains(token)) {
      int cut=positions[positions.Length-1];lines.Add(current.Substring(0,cut));line.Clear();line.Append(current.Substring(cut));
     }else{lines.Add(current);line.Clear();}
    }
    line.Append(token);
   }
   lines.Add(line.ToString());
   for(int i=0;i<lines.Count;i+=linesPerPage)Pages.Add(string.Join("\n",lines.GetRange(i,Math.Min(linesPerPage,lines.Count-i)).ToArray()));
   LoadPage();
  }
  void LoadPage(){elements=StringInfo.ParseCombiningCharacters(FullPage);revealed=0;reading=0;}
  public void Update(double dt,bool hovered) {
   if(Closed || dt<=0)return;
   if(!Complete){revealed=Math.Min(elements.Length,revealed+dt*28);return;}
   // Long replies stay available until explicitly paged/dismissed.
   if(!hovered && Pages.Count==1){reading+=dt;if(reading>Math.Max(8,Math.Min(20,elements.Length*.13)))Closed=true;}
  }
  public void Advance(){if(!Complete){revealed=elements.Length;return;}if(page+1<Pages.Count){page++;LoadPage();}else Closed=true;}
  public static Rectangle Place(Rectangle pet,Rectangle work,Size size,out bool tailLeft) {
   int right=pet.Left+pet.Width/2+pet.Width/5;
   int left=pet.Left+pet.Width/2-pet.Width/5-size.Width;
   tailLeft=right+size.Width<=work.Right-8 || left<work.Left+8;
   int x=tailLeft?right:left;
   x=Math.Max(work.Left,Math.Min(x,work.Right-size.Width));
   int y=Math.Max(work.Top,Math.Min(pet.Top+20-size.Height/3,work.Bottom-size.Height));
   return new Rectangle(x,y,size.Width,size.Height);
  }
 }
}
