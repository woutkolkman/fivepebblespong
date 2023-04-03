using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace CaptureAPI
{
    public class Program
    {
//        public static TimeSpan interval = new TimeSpan(0, 0, 0, 0, 50); //20 fps
//        public static TimeSpan interval = new TimeSpan(333333); //30 fps
        public static TimeSpan interval = new TimeSpan(222222); //45 fps
//        public static TimeSpan interval = new TimeSpan(166666); //60 fps
        public static string captureWindow = "Command Prompt";

        public static string openProgram = ""; //if window is not found, program will be opened
        public static Process? openedProgram;
        public static bool triedOpeningProgram = false; //tried starting other program
        public static volatile bool closeOperation = false; //close this program if true


        public static void Main(string[] args)
        {
            Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Startup");

            if (args.Length > 0)
                captureWindow = args[0];
            if (args.Length > 1)
                openProgram = args[1];

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(ProcessExit);
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ProcessExit);

            //read console input
            Task readInput = Task.Factory.StartNew(() => {
                try {
                    while (true) {
                        string? line = Console.ReadLine();
                        if (0 == String.Compare(line, "c"))
                            closeOperation = true;
                    }
                } catch (Exception ex) {
                    Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Console.ReadLine error: " + ex.ToString());
                }
            });

            HandleAPI();

            Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Done");
            Environment.Exit(0); //also closes blocking task readInput
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

            while (!closeOperation)
            {
                //update rate
                TimeSpan waitTime = (prevTime + interval) - DateTime.Now;
                if (waitTime > new TimeSpan(0))
                    Thread.Sleep(waitTime);
                prevTime = DateTime.Now;

                //search for window if handle is invalid
                if (windowHandle == WindowFinder.INVALID_HANDLE_VALUE)
                    windowHandle = WindowFinder.GetWindowHandle(captureWindow);

                //try again next loop
                if (windowHandle == WindowFinder.INVALID_HANDLE_VALUE) {
                    if (!triedOpeningProgram)
                        windowHandle = (int) StartProgram(openProgram);
                    triedOpeningProgram = true;

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
                    img = Screenshot.CaptureWindow((IntPtr) windowHandle);
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


        //try starting program indicated by path
        public static IntPtr StartProgram(string path)
        {
            IntPtr handle = (IntPtr) WindowFinder.INVALID_HANDLE_VALUE;
            if (string.IsNullOrEmpty(path)) {
                Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] StartProgram, No program path parameter provided");
                return handle;
            }
            Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] StartProgram, Starting program \"" + path + "\"");
            ProcessStartInfo info = new ProcessStartInfo(path);
            info.UseShellExecute = true;
            try {
                openedProgram = Process.Start(info);
                if (openedProgram != null && !openedProgram.WaitForInputIdle(5000))
                    Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] StartProgram, Idle state not reached");
            } catch (Exception ex) {
                Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] StartProgram, Exception starting program \"" + openProgram + "\": " + ex.ToString());
                Thread.Sleep(200); //last resort wait for program startup
            }
            if (openedProgram != null) {
                handle = openedProgram.MainWindowHandle;
                if (!String.IsNullOrEmpty(openedProgram.MainWindowTitle))
                    captureWindow = openedProgram.MainWindowTitle;
            }
            Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] StartProgram, Handle: " + handle + ", title: \"" + openedProgram?.MainWindowTitle + "\"");
            return handle;
        }


        //called when this program exits
        static void ProcessExit(object? sender, EventArgs e)
        {
            Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Received exit event: " + e.ToString());

            if (openedProgram != null && !openedProgram.HasExited) {
                openedProgram?.CloseMainWindow();
                openedProgram?.Close();
                Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Closed running program");
            }
            openedProgram = null;
        }


        //unhandled exception handler
        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine(e.Message, args.ExceptionObject.GetType().ToString());
        }
    }
}
