using System.Drawing;
using System.Drawing.Imaging;

namespace CaptureAPI
{
    public class Program
    {
//        public static TimeSpan interval = new TimeSpan(0, 0, 0, 0, 50); //20 fps
        public static TimeSpan interval = new TimeSpan(333333); //30 fps
//        public static TimeSpan interval = new TimeSpan(166666); //60 fps


        public static void Main(string[] args)
        {
            Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Startup");

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(ProcessExit);
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ProcessExit);

            HandleAPI();

            Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Done");
        }


        public static void HandleAPI()
        {
            if (!OperatingSystem.IsWindows())
                throw new System.NotSupportedException();

            int frame = 0;
            DateTime measureFps = DateTime.Now;
            DateTime prevTime = DateTime.Now;
            int windowHandle = WindowFinder.INVALID_HANDLE_VALUE;
            int notFoundCounter = 0;

            while (true)
            {
                //update rate
                TimeSpan waitTime = (prevTime + interval) - DateTime.Now;
                if (waitTime > new TimeSpan(0))
                    Thread.Sleep(waitTime);
                prevTime = DateTime.Now;

                //search for window if handle is invalid
                if (windowHandle == WindowFinder.INVALID_HANDLE_VALUE) {
                    windowHandle = WindowFinder.GetWindowHandle("Command Prompt");
                    Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] WindowHandle: " + windowHandle);
                }

                //try again next loop
                if (windowHandle == WindowFinder.INVALID_HANDLE_VALUE) {
                    notFoundCounter++;
                    if (notFoundCounter > 10) {
                        Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Window not found");
                        break;
                    }
                    Thread.Sleep(500);
                    continue;
                }
                notFoundCounter = 0;

                //capture screenshot and write to console
                Bitmap img;
                try {
                    img = Screenshot.CaptureWindow((IntPtr)windowHandle);
                } catch (System.ArgumentException ex) {
                    Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Error creating screenshot: " + ex.ToString());
                    windowHandle = WindowFinder.INVALID_HANDLE_VALUE;
                    continue;
                }
                System.IO.MemoryStream ms = new MemoryStream();
                img.Save(ms, ImageFormat.Png);
                byte[] bytes = ms.ToArray();
                if (bytes == null || bytes.Length <= 0) {
                    Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Error saving screenshot");
                    windowHandle = WindowFinder.INVALID_HANDLE_VALUE;
                    continue;
                }
                Console.WriteLine(Convert.ToBase64String(bytes));

                //measure FPS every 100th frame
                frame++;
                if (frame % 100 == 0) {
                    TimeSpan diff = DateTime.Now - measureFps;
                    Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Average FPS: " + (frame / diff.TotalSeconds).ToString());
                    frame = 0;
                    measureFps = DateTime.Now;
                }
            }
        }


        static void ProcessExit(object? sender, EventArgs e)
        {
            Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Received exit event: " + e.ToString());
        }


        //from obsclient
        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine(e.Message, args.ExceptionObject.GetType().ToString());
        }
    }
}
