using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using OBSStudioClient;
using OBSStudioClient.Exceptions;

namespace CaptureOBS
{
    public class Program
    {
#if DEBUG
        public static bool debug = true;
#else
        public static bool debug = false;
#endif
        public static TimeSpan interval = new TimeSpan(0, 0, 0, 0, 50); //20 fps
//        public static TimeSpan interval = new TimeSpan(333333); //30 fps
//        public static TimeSpan interval = new TimeSpan(166666); //60 fps


        public static void Main(string[] args)
        {
            if (debug) Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Startup");

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(ProcessExit);
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ProcessExit);

            var task = Task.Run(() =>
                HandleOBS()
            );
            task.Wait();

            if (debug) Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Done");
        }


        public static ObsClient client = new();
        public static async Task HandleOBS()
        {
            if (debug) Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Waiting on OBS Studio connection");
            bool isConnected = await client.ConnectAsync();
            if (debug) Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Connected: " + isConnected);
            if (!isConnected)
                return;


            //check if supported image format is available
            var getVersion = await client.GetVersion();
            bool supportsPNG = false;
            if (getVersion != null) {
                foreach (string format in getVersion.SupportedImageFormats) {
                    if (debug) Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Supported image format: " + format);
                    supportsPNG |= 0 == String.Compare(format, "png");
                }
            }
            if (debug) Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Contains \"png\": " + supportsPNG);
            if (!supportsPNG)
                goto SKIPLOOP;


            DateTime prevTime = DateTime.Now;
            while (isConnected)
            {
                //update rate
                TimeSpan waitTime = (prevTime + interval) - DateTime.Now;
                if (waitTime > new TimeSpan(0))
                    Thread.Sleep(waitTime);
                prevTime = DateTime.Now;


                //check if source is available
                try {
                    var sourceAvailable = await client.GetSourceActive("Window Capture");
                    if (!sourceAvailable.VideoActive) {
                        if (debug) Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Source \"Window Capture\" not available");
                        break;
                    }
                } catch (Exception e) {
                    if (debug) Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Source not available: " + e.ToString());
                    break;
                }


                //capture screenshot and write to console
                var data = await client.GetSourceScreenshot("Window Capture", "png");
                if (data != null)
                {
                    string[] png = data.Split(',');
                    if (data.Length <= 0 || png.Length < 2) {
                        if (debug) Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Invalid Base64-encoded screenshot: " + data);
                    } else {
                        if (debug) Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Received valid screenshot");
                        if (!debug) Console.WriteLine(png[1]);
                        //File.WriteAllBytes(@"C:\Users\Wout Kolkman\Downloads\test.png", Convert.FromBase64String(png[1]));
                    }
                }
                isConnected = client.ConnectionState != OBSStudioClient.Enums.ConnectionState.Disconnected && client.ConnectionState != OBSStudioClient.Enums.ConnectionState.Disconnecting;
            }


            SKIPLOOP:
            if (isConnected) {
                client.Disconnect();
                if (debug) Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Disconnected");
            } else {
                if (debug) Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Not connected or connection broken");
            }
        }


        static void ProcessExit(object? sender, EventArgs e)
        {
            if (debug) Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Received exit event: " + e.ToString());

            if (client.ConnectionState != OBSStudioClient.Enums.ConnectionState.Disconnected && client.ConnectionState != OBSStudioClient.Enums.ConnectionState.Disconnecting) {
                client.Disconnect();
                if (debug) Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] Disconnected");
            }
        }


        //from obsclient
        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            if (!debug) return;
            if (args.ExceptionObject is ObsResponseException obsResponseException) {
                Console.WriteLine($"{obsResponseException.ErrorCode}: {obsResponseException.ErrorMessage}", "OBSResponseException");
            } else if (args.ExceptionObject is ObsClientException obsClientException) {
                Console.WriteLine(obsClientException.Message, "OBSClientException");
            } else {
                Exception e = (Exception)args.ExceptionObject;
                Console.WriteLine(e.Message, args.ExceptionObject.GetType().ToString());
            }
        }
    }
}
