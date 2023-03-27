using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace FivePebblesPong
{
    public class Capture : FPGame
    {
        Process captureProcess;
        Queue<byte[]> imgQueue = new Queue<byte[]>();
        Mutex newFrameMtx = new Mutex(); //prevents queue from being used at the same time


        public Capture(OracleBehavior self) : base(self)
        {
            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ProcessStartInfo info = new ProcessStartInfo(assemblyFolder + "\\CaptureOBS.exe");
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;

            FivePebblesPong.ME.Logger_p.LogInfo("Capture, Starting CaptureOBS");
            try {
                captureProcess = Process.Start(info);
                captureProcess.OutputDataReceived += new DataReceivedEventHandler(DataReceivedEvent);
                captureProcess.BeginOutputReadLine();
            } catch (Exception ex) {
                FivePebblesPong.ME.Logger_p.LogInfo("Capture, Start exception: " + ex.ToString());
            }
        }


        ~Capture() //destructor
        {
            this.Destroy(); //if not done already
        }


        public override void Destroy()
        {
            base.Destroy(); //empty
            imgQueue.Clear();
            bool hasExited = captureProcess == null || captureProcess.HasExited;
            captureProcess?.CloseMainWindow();
            captureProcess?.Close();
            captureProcess = null;
            if (!hasExited)
                FivePebblesPong.ME.Logger_p.LogInfo("Capture, Closed CaptureOBS");
        }


        public override void Update(OracleBehavior self)
        {
            base.Update(self);

            if (imgQueue.Count <= 0)
                return;

            //get new frame and create Texture2D
            if (!newFrameMtx.WaitOne(50)) {
                FivePebblesPong.ME.Logger_p.LogInfo("Capture.Update, Mutex timeout");
                return;
            }
            try {
                byte[] imageBase64 = imgQueue.Dequeue();
                Texture2D newFrame = new Texture2D(0, 0);
                newFrame.LoadImage(imageBase64);
                FivePebblesPong.ME.Logger_p.LogInfo("Capture.Update, newFrame: " + newFrame.width + "x" + newFrame.height);
            } catch (Exception ex) {
                FivePebblesPong.ME.Logger_p.LogInfo("Capture.Update, Error parsing data: " + ex.ToString());
            }
            newFrameMtx.ReleaseMutex();
        }


        public override void Draw(Vector2 offset)
        {
            //update image positions
        }


        public bool firstEventMsg = true;
        public int droppedFrames = 0;
        public void DataReceivedEvent(object sender, DataReceivedEventArgs e)
        {
            if (firstEventMsg)
                FivePebblesPong.ME.Logger_p.LogInfo("Capture.DataReceivedEvent, Received first event");
            firstEventMsg = false;

            if (String.IsNullOrEmpty(e?.Data)) {
                FivePebblesPong.ME.Logger_p.LogInfo("Capture.DataReceivedEvent, Data null or empty");
                return;
            }

            if (imgQueue.Count > 5) {
                FivePebblesPong.ME.Logger_p.LogInfo("Capture.DataReceivedEvent, Byte[] queue too large, dropping frame... [" + droppedFrames++ + "]");
                return;
            }

            //every newline is a frame
            byte[] imageBase64 = new byte[0];
            try {
                imageBase64 = Convert.FromBase64String(e.Data);
                //File.WriteAllBytes("C:\\test\\test.png", bytes);
            } catch (Exception ex) {
                FivePebblesPong.ME.Logger_p.LogInfo("Capture.DataReceivedEvent, Error parsing data: " + ex.ToString());
            }

            //the byte array MUST be queued, calling the Texture2D ctor here crashes the game apparently
            if (!newFrameMtx.WaitOne(50)) {
                FivePebblesPong.ME.Logger_p.LogInfo("Capture.DataReceivedEvent, Mutex timeout");
                return;
            }
            imgQueue.Enqueue(imageBase64);
            newFrameMtx.ReleaseMutex();
        }
    }
}
