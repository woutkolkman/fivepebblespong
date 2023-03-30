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
        public bool adjustingWindow = true; //set to false if you find movable windows annoying
        Process captureProcess;
        public int frame = 0;
        public ShowMediaMovementBehavior adjusting = new ShowMediaMovementBehavior();

        Queue<byte[]> imgLoad = new Queue<byte[]>();
        Mutex imgLoadMtx = new Mutex(); //prevents queue from being used twice at the same time

        Queue<ProjectedImage> imgLoiter = new Queue<ProjectedImage>();
        public const int IMG_LOITER_COUNT = 3; //prevents flashing images

        Queue<string> imgUnload = new Queue<string>();
        public const int IMG_UNLOAD_AT_COUNT = 4; //delay atlas unload so game doesn't throw exceptions


        public Capture(OracleBehavior self) : base(self)
        {
            adjusting.showMediaPos = new Vector2(midX, midY);

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
                FivePebblesPong.ME.Logger_p.LogError("Capture, Start exception: " + ex.ToString());
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

            if (captureProcess != null && !captureProcess.HasExited) {
                captureProcess?.CloseMainWindow();
                captureProcess?.Close();
                FivePebblesPong.ME.Logger_p.LogInfo("Capture.Destroy, Closed CaptureOBS");
            }
            captureProcess = null;

            //TODO program keeps running if exiting RainWorld while Capture was active

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
                if (imgUnload == null) {
                    FivePebblesPong.ME.Logger_p.LogWarning("Capture.Destroy task, imgUnload queue is null, result: possible memory leak");
                    return;
                }
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

            if (self is SSOracleBehavior)
                (self as SSOracleBehavior).movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
            if (self is SLOracleBehavior)
                (self as SLOracleBehavior).movementBehavior = SLOracleBehavior.MovementBehavior.KeepDistance;

            adjusting.Update(self, new Vector2(midX, midY), !adjustingWindow);

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

            newFrame = CreateGamePNGs.AddTransparentBorder(ref newFrame);
            string imgName = "FPP_Window_" + frame++;
            //FivePebblesPong.ME.Logger_p.LogInfo("Capture.Update, Creating: \"" + imgName + "\"");

            //load and display new frame
            ProjectedImage temp;
            if ((self is SLOracleBehavior && !ModManager.MSC) || self is MoreSlugcats.SSOracleRotBehavior) {
                temp = new MoonProjectedImageFromMemory(new List<Texture2D> { newFrame }, new List<string> { imgName }, 0) { pos = adjusting.showMediaPos };
            } else {
                temp = new ProjectedImageFromMemory(new List<Texture2D> { newFrame }, new List<string> { imgName }, 0) { pos = adjusting.showMediaPos };
            }
            imgLoiter.Enqueue(temp);
            self.oracle.myScreen.room.AddObject(temp);
        }


        public override void Draw(Vector2 offset)
        {
            //update (only first) image position
            if (imgLoiter.Count > 0)
                imgLoiter.Peek().pos = adjusting.showMediaPos - offset;
        }


        public bool firstEventMsg = true;
        public int droppedFrames = 0;
        public void DataReceivedEvent(object sender, DataReceivedEventArgs e)
        {
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
            } catch (FormatException) {
                FivePebblesPong.ME.Logger_p.LogInfo("Capture.DataReceivedEvent, \"" + e.Data + "\"");
                return;
            } catch (ArgumentNullException ex) {
                FivePebblesPong.ME.Logger_p.LogInfo("Capture.DataReceivedEvent, Error parsing data: " + ex.ToString());
                return;
            }

            if (firstEventMsg)
                FivePebblesPong.ME.Logger_p.LogInfo("Capture.DataReceivedEvent, Received first valid frame");
            firstEventMsg = false;

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
