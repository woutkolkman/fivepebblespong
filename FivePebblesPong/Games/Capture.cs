using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FivePebblesPong
{
    public class Capture : FPGame
    {
        Process captureProcess;
        public int frame = 0;

        Queue<byte[]> imgLoad = new Queue<byte[]>();
        Mutex imgLoadMtx = new Mutex(); //prevents queue from being used twice at the same time

        Queue<ProjectedImage> imgLoiter = new Queue<ProjectedImage>();
        public const int IMG_LOITER_COUNT = 3; //prevents flashing images

        Queue<string> imgUnload = new Queue<string>();
        public const int IMG_UNLOAD_AT_COUNT = 4; //delay atlas unload so game doesn't throw exceptions


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
            imgLoad.Clear();
            bool hasExited = captureProcess == null || captureProcess.HasExited;
            captureProcess?.CloseMainWindow();
            captureProcess?.Close();
            captureProcess = null;

            //TODO program keeps running if exiting RainWorld while Capture was active

            if (!hasExited)
                FivePebblesPong.ME.Logger_p.LogInfo("Capture.Destroy, Closed CaptureOBS");

            while (imgLoiter.Count > 0) {
                ProjectedImage img = imgLoiter.Dequeue();
                img.RemoveFromRoom();
                foreach (string name in img.imageNames) {
                    FivePebblesPong.ME.Logger_p.LogInfo("Capture.Destroy, RemoveFromRoom: \"" + name + "\"");
                    imgUnload.Enqueue(name);
                }
            }

            Task deload = Task.Factory.StartNew(() =>
            {
                Thread.Sleep(1000);
                while (imgUnload.Count > 0) {
                    string name = imgUnload.Dequeue();
                    FivePebblesPong.ME.Logger_p.LogInfo("Capture.Destroy task, Unload: \"" + name + "\"");
                    Futile.atlasManager.ActuallyUnloadAtlasOrImage(name);
                }
            });
        }


        public override void Update(OracleBehavior self)
        {
            base.Update(self);

            if (imgLoiter.Count >= IMG_LOITER_COUNT) {
                ProjectedImage img = imgLoiter.Dequeue();
                self.oracle.room.RemoveObject(img);
                foreach (string name in img.imageNames)
                    imgUnload.Enqueue(name);
            }

            if (imgUnload.Count >= IMG_UNLOAD_AT_COUNT)
                Futile.atlasManager.ActuallyUnloadAtlasOrImage(imgUnload.Dequeue());

            if (imgLoad.Count <= 0)
                return;

            //get new frame and save
            if (!imgLoadMtx.WaitOne(50)) {
                FivePebblesPong.ME.Logger_p.LogInfo("Capture.Update, Mutex timeout");
                return;
            }

            Texture2D newFrame = new Texture2D(0, 0);
            try {
                byte[] imageBase64 = imgLoad.Dequeue();
                newFrame.LoadImage(imageBase64);
            } catch (Exception ex) {
                FivePebblesPong.ME.Logger_p.LogInfo("Capture.Update, Error storing data: " + ex.ToString());
            }
            imgLoadMtx.ReleaseMutex();

            if (newFrame?.width <= 0 || newFrame.height <= 0)
                return;

            string imgName = "FPP_Window_" + frame++;
            //FivePebblesPong.ME.Logger_p.LogInfo("Capture.Update, Creating: \"" + imgName + "\"");

            //load and display new frame
            ProjectedImage temp;
            if ((self is SLOracleBehavior && !ModManager.MSC) || self is MoreSlugcats.SSOracleRotBehavior) {
                temp = new MoonProjectedImageFromMemory(new List<Texture2D> { newFrame }, new List<string> { imgName }, 0);
            } else {
                temp = new ProjectedImageFromMemory(new List<Texture2D> { newFrame }, new List<string> { imgName }, 0);
            }
            temp.pos = new Vector2(midX, midY);
            imgLoiter.Enqueue(temp);
            self.oracle.myScreen.room.AddObject(temp);
        }


        public override void Draw(Vector2 offset)
        {
            //update image positions
            /*if (window == null)
                return;
            window.pos = new Vector2(midX, midY);*/
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

            if (imgLoad.Count > 5) {
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
            if (!imgLoadMtx.WaitOne(50)) {
                FivePebblesPong.ME.Logger_p.LogInfo("Capture.DataReceivedEvent, Mutex timeout");
                return;
            }
            imgLoad.Enqueue(imageBase64);
            imgLoadMtx.ReleaseMutex();
        }
    }
}
