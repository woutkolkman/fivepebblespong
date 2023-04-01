using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace CaptureOBS
{
    public class Program
    {
        public static int frame = 0;
        public static DateTime measureFps = DateTime.Now;

//        public static TimeSpan interval = new TimeSpan(0, 0, 0, 0, 50); //20 fps
        public static TimeSpan interval = new TimeSpan(333333); //30 fps
//        public static TimeSpan interval = new TimeSpan(166666); //60 fps


        public static void Main(string[] args)
        {
            Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Startup");

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(ProcessExit);
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ProcessExit);

            int windowHandle = WindowFinder.INVALID_HANDLE_VALUE;
            int notFoundCounter = 0;
            while (true)
            {
                if (windowHandle == WindowFinder.INVALID_HANDLE_VALUE) {
                    windowHandle = WindowFinder.GetWindowHandle("Command Prompt");
                    Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] WindowHandle: " + windowHandle);
                }

                if (windowHandle == WindowFinder.INVALID_HANDLE_VALUE) {
                    notFoundCounter++;
                    if (notFoundCounter > 10) {
                        Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Window not found");
                        break;
                    }
                    Thread.Sleep(500);
                    continue;
                }
                Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] WindowHandle valid");
                break;
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
