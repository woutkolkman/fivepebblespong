using System;
using System.Text.RegularExpressions;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FivePebblesPong
{
    public class DoomRetro : FPGame
    {
        // https://www.cyotek.com/blog/capturing-screenshots-using-csharp-and-p-invoke
        // https://improve.dk/getting-window-location-and-size/

        // The GetWindowRect function takes a handle to the window as the first parameter. The second parameter
        // must include a reference to a Rectangle object. This Rectangle object will then have it's values set
        // to the window rectangle properties.
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }


        public DoomRetro(OracleBehavior self) : base(self)
        {
            WindowFinder wf = new WindowFinder();
            FivePebblesPong.ME.Logger_p.LogInfo("searching for window");
            wf.FindWindows(0, null, new Regex("Command Prompt"), null, new WindowFinder.FoundWindowCallback(callback));

            bool callback(int handle) {
                FivePebblesPong.ME.Logger_p.LogInfo("found window!");

                RECT rect = new RECT();
                GetWindowRect((System.IntPtr) handle, out rect);
                FivePebblesPong.ME.Logger_p.LogInfo(rect.left.ToString() + " " + rect.top.ToString() + " " + rect.right.ToString() + " " + rect.bottom.ToString());

                return false; //stop searching for windows
            }

            FivePebblesPong.ME.Logger_p.LogInfo("done");
        }


        ~DoomRetro() //destructor
        {
            this.Destroy(); //if not done already
        }


        public override void Destroy()
        {
            base.Destroy(); //empty
        }


        public override void Update(OracleBehavior self)
        {
            base.Update(self);
        }


        public override void Draw(Vector2 offset)
        {
            //update image positions
        }
    }


    //not my code, but from http://improve.dk/finding-specific-windows/
    public class WindowFinder
    {
        // Win32 constants.
        const int WM_GETTEXT = 0x000D;
        const int WM_GETTEXTLENGTH = 0x000E;

        // Win32 functions that have all been used in previous blogs.
        [DllImport("User32.Dll")]
        private static extern void GetClassName(int hWnd, StringBuilder s, int nMaxCount);

        [DllImport("User32.dll")]
        private static extern int GetWindowText(int hWnd, StringBuilder text, int count);

        [DllImport("User32.dll")]
        private static extern Int32 SendMessage(int hWnd, int Msg, int wParam, StringBuilder lParam);

        [DllImport("User32.dll")]
        private static extern Int32 SendMessage(int hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32")]
        private static extern int GetWindowThreadProcessId(int hWnd, out int lpdwProcessId);

        // EnumChildWindows works just like EnumWindows, except we can provide a parameter that specifies the parent
        // window handle. If this is NULL or zero, it works just like EnumWindows. Otherwise it'll only return windows
        // whose parent window handle matches the hWndParent parameter.
        [DllImport("user32.Dll")]
        private static extern Boolean EnumChildWindows(int hWndParent, PChildCallBack lpEnumFunc, int lParam);

        // The PChildCallBack delegate that we used with EnumWindows.
        private delegate bool PChildCallBack(int hWnd, int lParam);

        // This is an event that is run each time a window was found that matches the search criterias. The boolean
        // return value of the delegate matches the functionality of the PChildCallBack delegate function.
        private event FoundWindowCallback foundWindow;
        public delegate bool FoundWindowCallback(int hWnd);

        // Members that'll hold the search criterias while searching.
        private int parentHandle;
        private Regex className;
        private Regex windowText;
        private Regex process;

        // The main search function of the WindowFinder class. The parentHandle parameter is optional, taking in a zero if omitted.
        // The className can be null as well, in this case the class name will not be searched. For the window text we can input
        // a Regex object that will be matched to the window text, unless it's null. The process parameter can be null as well,
        // otherwise it'll match on the process name (Internet Explorer = "iexplore"). Finally we take the FoundWindowCallback
        // function that'll be called each time a suitable window has been found.
        public void FindWindows(int parentHandle, Regex className, Regex windowText, Regex process, FoundWindowCallback fwc)
        {
            this.parentHandle = parentHandle;
            this.className = className;
            this.windowText = windowText;
            this.process = process;

            // Add the FounWindowCallback to the foundWindow event.
            foundWindow = fwc;

            // Invoke the EnumChildWindows function.
            EnumChildWindows(parentHandle, new PChildCallBack(enumChildWindowsCallback), 0);
        }

        // This function gets called each time a window is found by the EnumChildWindows function. The foun windows here
        // are NOT the final found windows as the only filtering done by EnumChildWindows is on the parent window handle.
        private bool enumChildWindowsCallback(int handle, int lParam)
        {
            // If a class name was provided, check to see if it matches the window.
            if (className != null)
            {
                StringBuilder sbClass = new StringBuilder(256);
                GetClassName(handle, sbClass, sbClass.Capacity);

                // If it does not match, return true so we can continue on with the next window.
                if (!className.IsMatch(sbClass.ToString()))
                    return true;
            }

            // If a window text was provided, check to see if it matches the window.
            if (windowText != null)
            {
                int txtLength = SendMessage(handle, WM_GETTEXTLENGTH, 0, 0);
                StringBuilder sbText = new StringBuilder(txtLength + 1);
                SendMessage(handle, WM_GETTEXT, sbText.Capacity, sbText);

                // If it does not match, return true so we can continue on with the next window.
                if (!windowText.IsMatch(sbText.ToString()))
                    return true;
            }

            // If a process name was provided, check to see if it matches the window.
            if (process != null)
            {
                int processID;
                GetWindowThreadProcessId(handle, out processID);

                // Now that we have the process ID, we can use the built in .NET function to obtain a process object.
                Process p = Process.GetProcessById(processID);

                // If it does not match, return true so we can continue on with the next window.
                if (!process.IsMatch(p.ProcessName))
                    return true;
            }

            // If we get to this point, the window is a match. Now invoke the foundWindow event and based upon
            // the return value, whether we should continue to search for windows.
            return foundWindow(handle);
        }
    }
}
