using System;
using System.Text.RegularExpressions;
using System.Threading;
using OBSStudioClient;
using OBSStudioClient.Exceptions;
using static System.Net.Mime.MediaTypeNames;

namespace CaptureOBS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Startup");

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);

            var task = Task.Run(() =>
                HandleOBS()
            );
            task.Wait();

            Console.WriteLine("Done");
        }


        public static async Task HandleOBS()
        {
            Console.WriteLine("Waiting on OBS Studio connection");
            ObsClient client = new();
            bool isConnected = await client.ConnectAsync();
            Console.WriteLine("Connected: " + isConnected);

            //check if supported image format is available
            if (isConnected)
            {
                var getVersion = await client.GetVersion();
                bool containsPNG = false;

                if (getVersion != null) {
                    foreach (string format in getVersion.SupportedImageFormats) {
                        Console.WriteLine("Supported image format: " + format);
                        containsPNG |= 0 == String.Compare(format, "png");
                    }
                }

                Console.WriteLine("Contains \"png\": " + containsPNG);
                if (!containsPNG)
                    goto SKIPLOOP;
            }

            while (isConnected)
            {
                //check if source is available
                var sourceAvailable = await client.GetSourceActive("Window Capture");
                if (sourceAvailable == null || !sourceAvailable.VideoActive) {
                    Console.WriteLine("Source \"Window Capture\" not available");
                    break;
                }

                var data = await client.GetSourceScreenshot("Window Capture", "png");

                if (data != null)
                {
                    string[] png = data.Split(',');
                    if (data.Length <= 0 || png.Length < 2) {
                        Console.WriteLine("Invalid Base64-encoded screenshot: " + data);
                        break; //TODO continue
                    }
                    File.WriteAllBytes(@"C:\Users\Wout Kolkman\Downloads\test.png", Convert.FromBase64String(png[1]));
                }

                isConnected = client.ConnectionState != OBSStudioClient.Enums.ConnectionState.Disconnected && client.ConnectionState != OBSStudioClient.Enums.ConnectionState.Disconnecting;
                break; //TODO remove
            }

            SKIPLOOP:
            if (isConnected) {
                Console.WriteLine("Disconnecting");
                client.Disconnect();
            } else {
                Console.WriteLine("Not connected or connection broken");
            }
        }


        //from obsclient
        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
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
