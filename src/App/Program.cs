using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
namespace JouchoPet
{
    static class Program
    {
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
        [STAThread] static void Main()
        {
            bool fresh;
            using (Mutex mutex = new Mutex(true, "JouchoPet.AnemoneI.v1", out fresh))
            {
                if (!fresh)
                {
                    MessageBox.Show("Isekaijoucho is already running. Look for the flower icon in the system tray.");
                    return;
                }
                try
                {
                    SetProcessDPIAware();
                    GridSnapLease.Recover();
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Pet());
                }
                catch(Exception ex)
                {
                    MessageBox.Show("The pet could not start. Keep the matching character.pack beside the app.\n\n" + ex.Message, "Isekaijoucho Pet");
                }
            }
        }
    }
}
