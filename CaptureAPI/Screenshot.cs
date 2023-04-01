using System.Runtime.InteropServices;
using System.Drawing;

namespace CaptureAPI
{
    //from https://stackoverflow.com/questions/1163761/capture-screenshot-of-active-window/24879511#24879511
    //and http://web.archive.org/web/20161116203653/http://www.snippetsource.net/Snippet/158/capture-screenshot-in-c
    public class Screenshot
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetDesktopWindow();

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);


        public static Bitmap CaptureDesktop()
        {
            return CaptureWindow(GetDesktopWindow());
        }


        public static Bitmap CaptureActiveWindow()
        {
            return CaptureWindow(GetForegroundWindow());
        }


        public static Bitmap CaptureWindow(IntPtr handle)
        {
            if (!OperatingSystem.IsWindows())
                throw new System.NotSupportedException();

            var rect = new Rect();
            GetWindowRect(handle, ref rect);
            Rectangle bounds = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            var result = new Bitmap(bounds.Width, bounds.Height); //TODO fix bitmap support

            using (var graphics = System.Drawing.Graphics.FromImage(result))
            {
                graphics.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
            }

            return result;
        }
    }
}
