using System;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
namespace JouchoPet
{
    sealed partial class Pet : Form
    {
        readonly PetRenderer renderer;
        readonly FrameClock timer;
        readonly FrameStatistics frameStatistics = new FrameStatistics();
        readonly Random random = new Random();
        readonly ContextMenuStrip menu = new ContextMenuStrip();
        readonly NotifyIcon tray;
        readonly string settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IsekaijouchoPet", "settings.txt");
        ToolStripMenuItem wanderItem, sleepItem, topItem;
        int petHeight = AnimationTuning.DefaultHeight, direction = 1;
        readonly Stopwatch clock = Stopwatch.StartNew();
        double previousTime, elapsed, gait, walkBlend, lookX, lookY, lean, dragLean, speed, moveRemainder;
        double nextBlink=2.4, blinkStart=-10, blink;
        readonly Motion motion=new Motion();
        Point previousCursor;
        bool wandering = true, sleeping, dragging, moved, menuOpen, walking, running;
        Point dragStart, formStart;
        DateTime stateUntil = DateTime.UtcNow.AddSeconds(6), happyUntil = DateTime.MinValue;
        DateTime lastInput = DateTime.UtcNow;
        readonly RestMotion rest = new RestMotion();
        readonly GreetingMotion greeting = new GreetingMotion();
        bool greetingWaves;
        double greetingWaveBlend;
        Icon petIcon;
        ChatWindow chat;
        void OpenChat()
        {
            if(chat==null || chat.IsDisposed){chat=new ChatWindow();chat.AssistantSpoke+=delegate(string text){Say(text,true);};}
            chat.Show();
            chat.Activate();
        }
        public Pet()
        {
            AnimationParameters.UseFixedValues();
            motion.SelectLightTurn=delegate{return random.NextDouble()*100<AnimationParameters.Get("turnChance");};
            timer = new FrameClock(this);
            Text = "Isekaijoucho - Anemone I";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            renderer = new PetRenderer();
            LoadSettings();
            ResizePet();
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(area.Right - Width - 35, area.Bottom - Height);
            wanderItem = new ToolStripMenuItem("Walk around", null, delegate
            {
                CancelCast();CancelSword();CancelLeap();wandering = !wandering; walking = false; wanderItem.Checked = wandering; SaveSettings();
            });
            wanderItem.Checked = wandering;
            sleepItem = new ToolStripMenuItem("Sleep / wake", null, delegate
            {
                SetSleep(!sleeping);
            });
            topItem = new ToolStripMenuItem("Always on top", null, delegate
            {
                TopMost = !TopMost; topItem.Checked = TopMost; SaveSettings();
            });
            topItem.Checked = TopMost;
            menu.Items.Add("Isekaijoucho / Anemone I").Enabled = false;
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("对话 / 文件助手...", null, delegate
            {
                OpenChat();
            });
            menu.Items.Add("Head pat", null, delegate
            {
                HeadPat();
            });
            menu.Items.Add("偏头问好",null,delegate{Greet();});
            menu.Items.Add("挥手问好",null,delegate{Greet(true);});
            AddSpeechMenu();
            menu.Items.Add("Walk left (preview)", null, delegate
            {
                StartWalk(-1);
            });
            menu.Items.Add("Walk right (preview)", null, delegate
            {
                StartWalk(1);
            });
            menu.Items.Add("Turn around (preview)", null, delegate
            {
                StartWalk(motion.Facing==0?-1:-motion.Facing);
            });
            menu.Items.Add("向左小跑",null,delegate{StartWalk(-1,true);});
            menu.Items.Add("向右小跑",null,delegate{StartWalk(1,true);});
            menu.Items.Add("向上轻跃",null,delegate{StartLeap(-1);});
            menu.Items.Add("向下轻落",null,delegate{StartLeap(1);});
            AddArcheryMenu();AddSwordMenu();AddCastingMenu();
            AddCarryMenu();
            menu.Items.Add(wanderItem);
            menu.Items.Add(sleepItem);
            menu.Items.Add(topItem);
            ToolStripMenuItem size = new ToolStripMenuItem("Size");
            foreach(int value in new int[]
            {
                220,300,400
            })
            {
                int selected = value;
                size.DropDownItems.Add(value + " px", null, delegate
                {
                    CancelCarry(); int bottom = Bottom; petHeight = selected; ResizePet(); Top = bottom-Height; Clamp(); SaveSettings(); Render();
                });
            }
            menu.Items.Add(size);
            menu.Items.Add("Bring back to main screen", null, delegate
            {
                RestorePet();
            });
            menu.Items.Add("动画性能记录...", null, delegate
            {
                try { string path=frameStatistics.Save(); MessageBox.Show(frameStatistics.Report()+"\r\n"+path,"Animation performance"); }
                catch(Exception ex) { MessageBox.Show(ex.Message,"Animation performance"); }
            });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, delegate
            {
                Close();
            });
            menu.Opening += delegate
            {
                menuOpen = true;
            };
            menu.Closed += delegate
            {
                menuOpen = false;
            };
            using (Bitmap small = new Bitmap(32,32))
            {
                using(Graphics g = Graphics.FromImage(small))
                {
                    g.Clear(Color.Transparent);
                    g.FillEllipse(Brushes.Firebrick, 2,2,28,28);
                    g.FillEllipse(Brushes.White, 9,10,15,11);
                    g.FillEllipse(Brushes.Black, 14,11,7,9);
                }
                IntPtr handle=small.GetHicon();
                using(Icon temporary=Icon.FromHandle(handle))
                {
                    petIcon=(Icon)temporary.Clone();
                }
                Native.DestroyIcon(handle);
            }
            Icon=petIcon;
            tray = new NotifyIcon
            {
                Icon=petIcon, Text="Isekaijoucho Pet - double-click to find me", ContextMenuStrip=menu, Visible=true
            };
            tray.DoubleClick += delegate
            {
                RestorePet();
            };
            timer.Tick += Animate;
            Shown += delegate
            {
                Render();
                timer.Start();
                Say("你来了。\n今天也在这里陪你一会儿。\n想聊天的话，双击我就好。");
            };
            MouseDoubleClick += delegate
            {
                OpenChat();
            };
            MouseDown += OnDown;
            MouseMove += OnMove;
            MouseUp += OnUp;
            KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if(e.KeyCode==Keys.Escape) Close();
            };
            FormClosed += delegate
            {
                CancelCast();CancelSword();CancelArchery();if(magicOverlay!=null)magicOverlay.Dispose();if(swordOverlay!=null)swordOverlay.Dispose();if(castBodyOverlay!=null)castBodyOverlay.Dispose();if(castFlightOverlay!=null)castFlightOverlay.Dispose();
                CancelCarry();
                if(chat!=null)chat.Close();
                if(bubble!=null)bubble.Dispose();
                timer.Stop();
                timer.Dispose();
                SaveSettings();
                tray.Visible=false;
                tray.Dispose();
                menu.Dispose();
                renderer.Dispose();
                petIcon.Dispose();
            };
        }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams p=base.CreateParams;
                p.ExStyle |= 0x00080000 | 0x00000080;
                return p;
            }
        }
        protected override bool ShowWithoutActivation
        {
            get
            {
                return true;
            }
        }
        void ResizePet()
        {
            CancelCast();CancelSword();CancelArchery();
            CancelLeap();
            ClientSize=PetRenderer.CanvasSize(petHeight);
            renderer.Resize(petHeight);
        }
        void RestorePet()
        {
            CancelCast();CancelSword();CancelArchery();
            CancelLeap();
            CancelCarry();
            Show();
            Rectangle a=Screen.PrimaryScreen.WorkingArea;
            Location=new Point(a.Right-Width-30,a.Bottom-Height);
            Render();
        }
        void Clamp()
        {
            Rectangle a=Screen.FromPoint(new Point(Left+Width/2,Top+Height/2)).WorkingArea;
            Left=Math.Max(a.Left, Math.Min(Left,a.Right-Width));
            Top=Math.Max(a.Top, Math.Min(Top,a.Bottom-Height));
        }
    }
}
