using System;using System.Drawing;using System.Drawing.Imaging;using System.Globalization;using System.IO;using System.Text;using JouchoPet;
class BubbleCheck {
 static int checks;
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
 static SpeechContent Make(string text){return new SpeechContent(text,s=>StringInfo.ParseCombiningCharacters(s).Length<=14,5);}
 [STAThread]static void Main(string[] args){try {
  var simple=Make("你来了。今天也在这里陪你一会儿。");
  Check(simple.VisibleText=="","new text starts with empty reveal");simple.Update(.1,false);
  Check(simple.VisibleText.Length>0 && !simple.Complete,"text reveals gradually");simple.Advance();
  Check(simple.Complete && !simple.Closed,"first click reveals current page");
  simple.Update(100,true);Check(!simple.Closed,"hover keeps completed bubble visible");simple.Update(30,false);Check(simple.Closed,"short message expires after reading time");
  string text=new string('绪',1300)+"🙂e\u0301终点";var longText=Make(text);
  Check(longText.Pages.Count>10,"long reply is paginated");
  var joined=new StringBuilder();int pages=0;
  while(!longText.Closed){longText.Advance();joined.Append(longText.FullPage.Replace("\n",""));longText.Update(1000,false);Check(!longText.Closed,"long reply page remains available "+pages);longText.Advance();pages++;}
  Check(joined.ToString()==text,"pagination keeps every text element without truncation");
  var unicode=Make("🙂e\u0301🦋你好");
  for(int i=0;i<20;i++){unicode.Update(.02,false);string part=unicode.VisibleText;if(part.Length>0 && char.IsHighSurrogate(part[part.Length-1]))throw new Exception("split surrogate");}
  Check(unicode.Complete,"surrogates and combining characters reveal safely");
  var paragraphs=Make("第一句\r\n\r\n第二句");paragraphs.Advance();Check(paragraphs.VisibleText=="第一句\n\n第二句","paragraph breaks preserved");
  var punctuation=Make(new string('情',14)+"。");punctuation.Advance();Check(!punctuation.VisibleText.Contains("\n。"),"closing punctuation stays with preceding character");
  foreach(var work in new[]{new Rectangle(0,0,1920,1080),new Rectangle(-1920,-200,1920,1080),new Rectangle(0,0,800,600)})foreach(bool right in new[]{false,true})foreach(bool bottom in new[]{false,true}) {
   bool tail;var pet=new Rectangle(right?work.Right-347:work.Left,bottom?work.Bottom-348:work.Top,347,348);
   var placed=SpeechContent.Place(pet,work,new Size(304,222),out tail);
   Check(work.Contains(placed),"bubble remains inside work area "+work+" "+right+" "+bottom);
   Check(tail!=right,"bubble chooses free side of character "+right+" "+bottom);
  }
  string folder=args.Length>0?args[0]:"qa/v0618/preview";Directory.CreateDirectory(folder);
  using(var bubble=new SpeechBubble())using(var font=new Font("Microsoft YaHei UI",15,FontStyle.Regular,GraphicsUnit.Pixel))using(var caption=new Font("Microsoft YaHei UI",12,FontStyle.Regular,GraphicsUnit.Pixel)) {
   var state=bubble.PrepareText("看见你回来了。\n……嗯，有一点高兴。\n\n想聊聊今天发生的事吗？");state.Advance();
   using(var card=SpeechBubble.RenderCard(state,true,font,caption))using(var pet=new PetRenderer())using(var stage=new Bitmap(800,410))using(var g=Graphics.FromImage(stage)) {
    pet.Resize(300);g.Clear(Color.FromArgb(235,228,220));g.DrawImageUnscaled(pet.Draw(new RenderFrame{MotionState="idle",Greeting=1,WavePhase=.8}),95,32);g.DrawImageUnscaled(card,374,58);stage.Save(Path.Combine(folder,"greeting-and-bubble.png"),ImageFormat.Png);
    Check(card.GetPixel(0,0).A==0,"card corners remain transparent");
   }
   var reply=bubble.PrepareText("这一段比较长，我会分成几页慢慢说。\n"+new string('想',140)+"\n最后一句也会留在这里。🙂");
   Check(reply.Pages.Count>1,"actual font layout makes multiple pages");
   using(var image=new Bitmap(1,1))using(var g=Graphics.FromImage(image))using(var format=(StringFormat)StringFormat.GenericTypographic.Clone())foreach(var page in reply.Pages) {
    Check(page.Split('\n').Length<=5,"rendered page has at most five lines");
    foreach(string line in page.Split('\n'))if(g.MeasureString(line,font,int.MaxValue,format).Width>248.01)throw new Exception("text exceeds card width");
   }
   Check(true,"all rendered lines fit content bounds");reply.Advance();using(var image=SpeechBubble.RenderCard(reply,false,font,caption))image.Save(Path.Combine(folder,"bubble-long-reply.png"));
  }
  Console.WriteLine("All "+checks+" bubble checks passed.");
 }catch(Exception ex){Console.WriteLine(ex);Environment.ExitCode=1;}}
}
