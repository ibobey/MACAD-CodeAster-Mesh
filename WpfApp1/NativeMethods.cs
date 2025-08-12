using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CodeAsterMesh
{
    internal static class NativeMethods
    {
        #region Constants

        public const int GWL_STYLE = -16;

        public const int GWL_EXSTYLE = -20;

        public const int WS_MAXIMIZEBOX = 0x10000;

        public const int WS_MINIMIZEBOX = 0x20000;

        public const int WS_EX_DLGMODALFRAME = 0x00000001;

        public const int SWP_NOSIZE = 0x0001;

        public const int SWP_NOMOVE = 0x0002;

        public const int SWP_NOZORDER = 0x0004;

        public const int SWP_FRAMECHANGED = 0x0020;

        public const uint WM_SETICON = 0x0080;

        // Define the window styles we use
        public const int WS_CHILD = 0x40000000;

        public const int WS_VISIBLE = 0x10000000;

        // Define the Windows messages we will handle
        public const int WM_MOUSEMOVE = 0x0200;

        public const int WM_LBUTTONDOWN = 0x0201;

        public const int WM_LBUTTONUP = 0x0202;

        public const int WM_LBUTTONDBLCLK = 0x0203;

        public const int WM_RBUTTONDOWN = 0x0204;

        public const int WM_RBUTTONUP = 0x0205;

        public const int WM_RBUTTONDBLCLK = 0x0206;

        public const int WM_MBUTTONDOWN = 0x0207;

        public const int WM_MBUTTONUP = 0x0208;

        public const int WM_MBUTTONDBLCLK = 0x0209;

        public const int WM_MOUSEWHEEL = 0x020A;

        public const int WM_XBUTTONDOWN = 0x020B;

        public const int WM_XBUTTONUP = 0x020C;

        public const int WM_XBUTTONDBLCLK = 0x020D;

        public const int WM_MOUSELEAVE = 0x02A3;

        // Define the values that let us differentiate between the two extra mouse buttons
        public const int MK_XBUTTON1 = 0x020;

        public const int MK_XBUTTON2 = 0x040;

        // Define the cursor icons we use
        public const int IDC_ARROW = 32512;

        // Define the TME_LEAVE value so we can register for WM_MOUSELEAVE messages
        public const uint TME_LEAVE = 0x00000002;

        public const int WM_SIZE = 0x0005;

        #endregion Constants

        #region Delegates and Structs

        public static readonly WndProc DefaultWindowProc = DefWindowProc;

        public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct TRACKMOUSEEVENT
        {
            public int cbSize;

            public uint dwFlags;

            public IntPtr hWnd;

            public uint dwHoverTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WNDCLASSEX
        {
            public uint cbSize;

            public uint style;

            [MarshalAs(UnmanagedType.FunctionPtr)]
            public WndProc lpfnWndProc;

            public int cbClsExtra;

            public int cbWndExtra;

            public IntPtr hInstance;

            public IntPtr hIcon;

            public IntPtr hCursor;

            public IntPtr hbrBackground;

            public string lpszMenuName;

            public string lpszClassName;

            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NativePoint
        {
            public int X;

            public int Y;
        }

        #endregion Delegates and Structs

        #region DllImports

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int value);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter,
            int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hwnd, uint msg,
            IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "CreateWindowEx", CharSet = CharSet.Auto)]
        public static extern IntPtr CreateWindowEx(
            int exStyle,
            string className,
            string windowName,
            int style,
            int x, int y,
            int width, int height,
            IntPtr hwndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", EntryPoint = "DestroyWindow", CharSet = CharSet.Auto)]
        public static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string module);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        [DllImport("user32.dll")]
        public static extern int TrackMouseEvent(ref TRACKMOUSEEVENT lpEventTrack);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.U2)]
        public static extern short RegisterClassEx([In] ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll")]
        public static extern int ScreenToClient(IntPtr hWnd, ref NativePoint pt);

        [DllImport("user32.dll")]
        public static extern int SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetFocus();

        [DllImport("user32.dll")]
        public static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(ref NativePoint point);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern int ShowCursor(bool bShow);

        #endregion DllImports

        #region Helpers

        public static int GetXLParam(int lParam)
        {
            return LowWord(lParam);
        }

        public static int GetYLParam(int lParam)
        {
            return HighWord(lParam);
        }

        public static short GetWheelDeltaWParam(IntPtr wParam) // Changed to IntPtr, returns short
        {
            // For WM_MOUSEWHEEL, wParam's relevant data is effectively 32-bit.
            // The HIWORD of this 32-bit value is the delta.
            // Convert IntPtr to an int first. If IntPtr is 64-bit, this truncates,
            // which is what we want as the message parameters are packed into 32-bit equivalent space.
            int wParam32 = unchecked((int)(long)wParam); // Or (int)wParam.ToInt64() then cast to int for clarity
                                                         // Or simply wParam.ToInt32() if we are sure the upper bits of IntPtr are zero.
                                                         // Given your wParam value, ToInt32() is fine.

            // The delta is in the high-order 16 bits of this 32-bit value.
            return (short)HighWord(wParam32);
        }

        public static int LowWord(int input)
        {
            return (short)(input & 0xffff);
        }

        public static int HighWord(int input)
        {
            return (short)(input >> 16);
        }

        #endregion Helpers
    }
}