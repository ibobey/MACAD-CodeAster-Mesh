using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

using Macad.Occt;
using Macad.Occt.Helper;
using System.Windows.Input;
using System.Diagnostics;

namespace CodeAsterMesh
{
    public class OcctViewportHost : HwndHost
    {
        #region Constructors

        public OcctViewportHost(double width, double height)
        {
            this.MessageHook += HostMessageHook;
            _width = (int)width;
            _height = (int)height;

            // Ensure OCCT window class is registered
            if (_occtWindowClass == null)
            {
                // CS_OWNDC: Allocates a unique device context for each window in the class.
                // CS_VREDRAW | CS_HREDRAW: Redraws the entire window if a movement or size adjustment changes the height/width of the client area.
                _occtWindowClass = new WNT_WClass(
                    new TCollection_AsciiString("OcctWPFApp"), // Class name
                    IntPtr.Zero, // Window procedure (OCCT handles it internally for WNT_Window)
                    (uint)(0x0008 | 0x0001 | 0x0002), // CS_OWNDC | CS_HREDRAW | CS_VREDRAW
                    0 // Extra class bytes
                );
                //_occtWindowClass.SetBackground(new Quantity_Color(Quantity_NameOfColor.GRAY50));
            }
        }

        #endregion Constructors

        #region Fields

        // Standard cursor IDs (MAKEINTRESOURCE is a macro, so we use the direct integer values)
        private const int IDC_ARROW = 32512;

        // OCCT window class - needs to be registered once
        private static WNT_WClass _occtWindowClass;

        private readonly int _width;

        private readonly int _height;

        private IntPtr _hwndOcct;

        private WNT_Window _occtWindow;

        private V3d_View _occtView;

        private V3d_Viewer _occtViewer;

        private AIS_InteractiveContext _aisContext;

        private Graphic3d_GraphicDriver _graphicDriver;

        private bool _isRotating = false;

        private bool _isPanning = false;

        private int _lastMouseX;

        private int _lastMouseY;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorName);

        // If you don't have Win32Api.SetCursor, add this P/Invoke:
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetCursorNative(IntPtr hCursor);

        #endregion Fields

        #region Methods

        public void DisplayShape(TopoDS_Shape shape, bool fitAll = true)
        {
            if (_aisContext == null || _occtView == null || shape == null || shape.IsNull())
                return;

            var aisShape = new AIS_Shape(shape);
            _aisContext.Display(aisShape, true); // true to update viewer immediately

            if (fitAll)
            {
                _occtView.FitAll(0.1, false); // 0.1 margin, don't force update (Display already did)
                _occtView.ZFitAll(1.0);
            }
            _occtView.Redraw();
        }

        public void ClearView()
        {
            if (_aisContext != null)
            {
                _aisContext.RemoveAll(true); // Remove all displayed objects and update
                _occtView?.Redraw();
            }
        }

        public V3d_View GetView() => _occtView;

        public AIS_InteractiveContext GetContext() => _aisContext;

        public void StartRotation(int x, int y)
        {
            if (_occtView == null)
                return;
            _occtView.StartRotation(x, y);
            _isRotating = true;
            _lastMouseX = x;
            _lastMouseY = y;
        }

        public void Rotate(int x, int y)
        {
            if (_occtView == null || !_isRotating)
                return;

            // V3d_View.Rotation expects the new mouse position
            _occtView.Rotation(x, y);
            _occtView.Redraw(); // Or _occtView.Update() if immediate mode is off

            _lastMouseX = x;
            _lastMouseY = y;
        }

        public void EndRotation()
        {
            _isRotating = false;
        }

        public void StartPan(int x, int y)
        {
            if (_occtView == null)
                return;
            _isPanning = true;
            _lastMouseX = x;
            _lastMouseY = y;
        }

        public void Pan(int x, int y)
        {
            if (_occtView == null || !_isPanning)
                return;

            int deltaX = x - _lastMouseX;
            int deltaY = y - _lastMouseY;

            // OCCT Pan: positive dY is typically down on screen, but up in view
            _occtView.Pan(deltaX, -deltaY, 1.0, true);
            _occtView.Redraw();

            _lastMouseX = x;
            _lastMouseY = y;
        }

        public void EndPan()
        {
            _isPanning = false;
        }

        public void Zoom(int x, int y, double delta) // delta is from mouse wheel
        {
            if (_occtView == null)
                return;

            double zoomFactor = 1.1; // Adjust zoom sensitivity as needed
            if (delta < 0) // Zoom out (delta is typically -120 or multiples)
            {
                zoomFactor = 1.0 / zoomFactor;
            }
            // If delta is positive (typically +120 or multiples), zoomFactor remains > 1 for zoom in.

            // Set the center for the upcoming zoom operation to the current mouse position
            _occtView.StartZoomAtPoint(x, y);

            // Apply the zoom factor
            // The 'true' for the second parameter means the Coef is relative to the current scale
            _occtView.SetZoom(zoomFactor, true);

            _occtView.Redraw();
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            _hwndOcct = IntPtr.Zero;

            try
            {
                // 1. Create Graphic Driver
                _graphicDriver = Graphic3d.CreateOpenGlDriver(false);

                // 2. Create Viewer
                _occtViewer = new V3d_Viewer(_graphicDriver);
                _occtViewer.SetDefaultLights();
                _occtViewer.SetLightOn();
                _occtViewer.SetDefaultViewProj(V3d_TypeOfOrientation.XposYposZpos); // Default isometric view
                //_occtViewer.SetBackgroundColor(Quantity_NameOfColor.GRAY30.ToQuantityColor());

                // 3. Create AIS Interactive Context
                _aisContext = new AIS_InteractiveContext(_occtViewer);
                _aisContext.SetDisplayMode((int)AIS_DisplayMode.Shaded, true); // Shaded with edges by default

                // 4. Create WNT_Window (the native OCCT window)
                _occtWindow = new WNT_Window("OcctView", // Window name
                    _occtWindowClass,
                    (uint)(0x40000000 | 0x10000000), // WS_CHILD | WS_VISIBLE
                    0, 0, _width, _height, // x, y, width, height
                    Quantity_NameOfColor.GRAY50, // Background color name
                    hwndParent.Handle // Parent HWND
                );
                _occtWindow.Map(); // Make the window visible

                _hwndOcct = _occtWindow.HWindow();

                // 5. Create V3d_View
                _occtView = _occtViewer.CreateView();
                _occtView.SetWindow(_occtWindow);
                _occtView.SetBackgroundColor(new Quantity_Color(Quantity_NameOfColor.GRAY70));
                _occtView.MustBeResized(); // Ensure initial size is processed
                _occtView.TriedronDisplay(Aspect_TypeOfTriedronPosition.LEFT_LOWER, new Quantity_Color(Quantity_NameOfColor.WHITE), 0.05, V3d_TypeOfVisualization.ZBUFFER);

                // Set immediate mode for updates
                _occtView.SetImmediateUpdate(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing OCCT: {ex.Message}", "OCCT Init Error");
                return new HandleRef(this, IntPtr.Zero);
            }

            return new HandleRef(this, _hwndOcct);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            if (_occtView != null)
            {
                _occtView.Remove();
                _occtView.Dispose();
                _occtView = null;
            }

            if (_aisContext != null)
            {
                _aisContext.Dispose();
                _aisContext = null;
            }

            if (_occtWindow != null)
            {
                if (_occtWindow.IsMapped())
                    _occtWindow.Unmap();
                _occtWindow.Dispose(); // This should destroy the native window
                _occtWindow = null;
            }

            if (_occtViewer != null)
            {
                _occtViewer.SetViewOff(); // Detach all views
                _occtViewer.Dispose();
                _occtViewer = null;
            }

            if (_graphicDriver != null)
            {
                _graphicDriver.Dispose();
                _graphicDriver = null;
            }
            // _occtWindowClass is static and typically not disposed until app exit
        }

        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_PAINT = 0x000F;
            const int WM_SIZE = 0x0005;
            const int WM_NCHITTEST = 0x0084;
            const int HTCLIENT = 1;
            const int WM_SETCURSOR = 0x0020; // Decimal 32

            // For WM_SETCURSOR, LOWORD(lParam) is the hit-test code.
            // HIWORD(lParam) is the mouse message identifier (e.g., WM_MOUSEMOVE).

            switch (msg)
            {
                // ... (WM_PAINT, WM_SIZE, WM_NCHITTEST cases remain the same) ...
                case WM_NCHITTEST:
                    handled = true;
                    return new IntPtr(HTCLIENT);

                case WM_SETCURSOR:
                    // Check if the cursor is over our client area.
                    // LOWORD of lParam contains the hit-test result from WM_NCHITTEST.
                    //int hitTestResult = LOWORD(lParam.ToInt32());
                    //if (hitTestResult == HTCLIENT)
                    //{
                    //    // System.Diagnostics.Debug.WriteLine("WM_SETCURSOR for HTCLIENT");
                    //    // Try to set the standard arrow cursor.
                    //    // LoadCursor(IntPtr.Zero, new IntPtr(IDC_ARROW)) loads the system arrow cursor.
                    //    // SetCursor is a Win32 API function.
                    //    IntPtr hCursor = LoadCursor(IntPtr.Zero, new IntPtr(IDC_ARROW));
                    //    if (hCursor != IntPtr.Zero)
                    //    {
                    //        Win32Api.SetCursor(hCursor); // Assuming you have a Win32Api helper or P/Invoke SetCursor
                    //                                     // If not, P/Invoke it:
                    //                                     // SetCursorNative(hCursor);
                    //    }
                    //    handled = true; // We've handled this message.
                    //    return new IntPtr(1); // Return TRUE (non-zero) to indicate we set the cursor.
                    //}
                    // If not HTCLIENT, let the default procedure handle it.
                    break;
            }
            return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
        }

        private IntPtr HostMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_MOUSEMOVE = 0x0200;
            const int WM_LBUTTONDOWN = 0x0201;
            const int WM_SETCURSOR = 0x0020; // This message determines the cursor

            // System.Diagnostics.Debug.WriteLine($"HostMessageHook: msg=0x{msg:X4}");

            if (msg == WM_SETCURSOR)
            {
                // You could try to force a cursor here, but it's usually better to let WPF handle it
                // if IsEnabled is true.
                // System.Diagnostics.Debug.WriteLine("WM_SETCURSOR received by HwndHost");
            }
            // Return IntPtr.Zero to indicate the message has not been handled by this hook
            // if you don't set handled = true.
            return IntPtr.Zero;
        }

        #endregion Methods
    }
}