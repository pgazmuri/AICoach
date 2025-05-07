using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

namespace AICoach.Services
{
    public class ScreenshotService
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        private const int MaxHistorySize = 5;
        private readonly Queue<ScreenshotRecord> _screenshotHistory = new Queue<ScreenshotRecord>();

        public class ScreenshotRecord
        {
            public Bitmap Screenshot { get; set; }
            public string WindowTitle { get; set; }
            public DateTime Timestamp { get; set; }

            public ScreenshotRecord(Bitmap screenshot, string windowTitle)
            {
                Screenshot = screenshot;
                WindowTitle = windowTitle;
                Timestamp = DateTime.Now;
            }
        }

        public Bitmap CaptureScreenshot()
        {
            // Get the screen where the cursor is currently located
            Screen cursorScreen = GetScreenWithCursor();
            
            Rectangle bounds = cursorScreen.Bounds;
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            }
            
            Logger.Instance.Log($"Screenshot captured from screen at {bounds.Location}, size: {bounds.Size}");
            return bitmap;
        }
        
        private Screen GetScreenWithCursor()
        {
            if (GetCursorPos(out POINT cursorPosition))
            {
                Point cursorPoint = new Point(cursorPosition.X, cursorPosition.Y);
                
                // Find which screen contains the cursor
                foreach (Screen screen in Screen.AllScreens)
                {
                    if (screen.Bounds.Contains(cursorPoint))
                    {
                        Logger.Instance.Log($"Cursor found on screen: {screen.DeviceName} at position {cursorPoint}");
                        return screen;
                    }
                }
            }
            
            // Fallback to primary screen if cursor position can't be determined
            Logger.Instance.Log("Could not determine cursor screen, falling back to primary screen");
            Screen? primaryScreen = Screen.PrimaryScreen;
            if (primaryScreen == null)
            {
                throw new InvalidOperationException("No primary screen detected.");
            }
            return primaryScreen;
        }

        public ScreenshotRecord CaptureAndStoreScreenshot()
        {
            var bitmap = CaptureScreenshot();
            var windowTitle = GetActiveWindowTitle();
            var record = new ScreenshotRecord(bitmap, windowTitle);
            
            AddToHistory(record);
            
            return record;
        }

        private void AddToHistory(ScreenshotRecord record)
        {
            _screenshotHistory.Enqueue(record);
            
            // Maintain history size
            while (_screenshotHistory.Count > MaxHistorySize)
            {
                var oldRecord = _screenshotHistory.Dequeue();
                // Dispose the bitmap to free resources
                oldRecord.Screenshot.Dispose();
            }
            
            Logger.Instance.Log($"Screenshot added to history. Current history size: {_screenshotHistory.Count}");
        }

        public IReadOnlyList<ScreenshotRecord> GetScreenshotHistory()
        {
            return _screenshotHistory.ToList();
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