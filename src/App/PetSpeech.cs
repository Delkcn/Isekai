using System;
using System.Windows.Forms;
namespace JouchoPet {
 sealed partial class Pet {
  SpeechBubble bubble;
  bool bubblesEnabled=true;
  void Say(string text,bool conversation=false) {
   if(!bubblesEnabled || IsDisposed)return;
   if(bubble!=null && bubble.Visible && bubble.Conversation && !conversation)return;
   if(bubble==null || bubble.IsDisposed){bubble=new SpeechBubble();bubble.OpenConversation+=OpenChat;}
   bubble.Speak(text,conversation,Bounds,Screen.FromControl(this).WorkingArea,TopMost);
  }
  void FollowSpeech(){if(bubble!=null && bubble.Visible)bubble.Follow(Bounds,Screen.FromControl(this).WorkingArea,TopMost);}
  void AddSpeechMenu() {
   var enabled=new ToolStripMenuItem("显示对话泡泡"){Checked=bubblesEnabled};
   enabled.Click+=delegate{bubblesEnabled=!bubblesEnabled;enabled.Checked=bubblesEnabled;if(!bubblesEnabled && bubble!=null)bubble.Dismiss();else Say("嗯，我在。想说什么的话，直接告诉我就好。");SaveSettings();};
   menu.Items.Add(enabled);
   menu.Items.Add("和我说句话",null,delegate {
    string[] words={"刚才在想，安静也可以是一种陪伴。你继续忙吧，我在这里。","今天有没有一件值得记住的小事？不必很特别。","看见你回来了。……嗯，有一点高兴。","如果思绪打了结，就先停一小会儿。我陪你。"};
    Say(words[random.Next(words.Length)]);
   });
   menu.Items.Add("收起对话泡泡",null,delegate{if(bubble!=null)bubble.Dismiss();});
  }
 }
}
