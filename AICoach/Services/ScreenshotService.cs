using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace AICoach.Services
{
    public class ScreenshotService
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        public Bitmap CaptureScreenshot()
        {
            Screen? primaryScreen = Screen.PrimaryScreen;
            if (primaryScreen == null)
            {
                throw new InvalidOperationException("No primary screen detected.");
            }
            
            Rectangle bounds = primaryScreen.Bounds;
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            }
            return bitmap;
        }

        public string GetActiveWindowTitle()
        {
            IntPtr handle = GetForegroundWindow();
            if (handle == IntPtr.Zero)
            {
                return string.Empty;
            }
            
            StringBuilder windowTitle = new StringBuilder(256);
            int length = GetWindowText(handle, windowTitle, windowTitle.Length);
            if (length == 0)
            {
                return string.Empty;
            }
            
            return windowTitle.ToString();
        }

        public byte[] ConvertScreenshotToBytes(Bitmap screenshot)
        {
            using (var ms = new MemoryStream())
            {
                screenshot.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }
    }
}