using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices; // For HandleRef if not in NativeMethods
using System.Windows;

using Macad.Occt;
using Macad.Occt.Helper;
using Macad.Resources;

namespace CodeAsterMesh
{
    public class OcctHwndControl : HwndWrapper, IDisposable // Inherit from the provided HwndWrapper
    {
        #region Constructors

        public OcctHwndControl()
        {
            // Subscribe to the mouse events from HwndWrapper
            this.HwndLButtonDown += OnHwndLButtonDown;
            this.HwndLButtonUp += OnHwndLButtonUp;
            this.HwndRButtonDown += OnHwndRButtonDown;
            this.HwndRButtonUp += OnHwndRButtonUp;
            this.HwndMouseMove += OnHwndMouseMove;
            this.HwndMouseWheel += OnHwndMouseWheel;
            // Add MButton for panning if you prefer
            this.HwndMButtonDown += OnHwndMButtonDown;
            this.HwndMButtonUp += OnHwndMButtonUp;
        }

        #endregion Constructors

        #region Fields

        private V3d_View _occtView;

        private V3d_Viewer _occtViewer;

        private AIS_InteractiveContext _aisContext;

        private Graphic3d_GraphicDriver _graphicDriver;

        private WNT_Window _occtWindow; // OCCT's own window wrapper

        // Interactivity state
        private bool _isRotating = false;

        private bool _isPanning = false;

        private System.Windows.Point _lastMousePosition;

        private Macad.Occt.Ext.AIS_ViewCubeEx _aisViewCube;

        private AIS_AnimationCamera _aisAnimationCamera; // For smooth transitions

        #endregion Fields

        #region Methods

        public void DisplayShape(TopoDS_Shape shape, bool fitAll = true)
        {
            if (_aisContext == null || _occtView == null || shape == null || shape.IsNull())
                return;

            var aisShape = new AIS_Shape(shape);
            aisShape.SetDisplayMode((int)AIS_DisplayMode.Shaded);
            aisShape.Attributes().SetFaceBoundaryDraw(true);
            _aisContext.Display(aisShape, false); // false, let Render handle redraw

            if (fitAll)
            {
                _occtView.FitAll(0.1, false);
                _occtView.ZFitAll(1.0);
            }
            // Request a redraw if not relying solely on CompositionTarget.Rendering
            // this.InvalidateVisual(); // Or directly call _occtView.Redraw();
        }

        public void Update()
        {
            _occtView.MustBeResized();
            _occtView.Update();
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            // This method is called by HwndHost to get the HWND to host.
            // HwndWrapper's BuildWindowCore creates its own window using CreateWindowEx
            // and its own registered class. We will let it do that.
            // Then, in InitializeOcct, we will tell OCCT to use this already created HWND.

            HandleRef buildResult = base.BuildWindowCore(hwndParent); // This calls HwndWrapper's CreateHostWindow
            IntPtr hostedHwnd = buildResult.Handle;

            if (hostedHwnd != IntPtr.Zero)
            {
                InitializeOcct(hostedHwnd, (int)this.ActualWidth, (int)this.ActualHeight);
            }
            return buildResult;
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            // Dispose OCCT resources
            _occtView?.Remove();
            _occtView?.Dispose();
            _occtView = null;

            _aisContext?.Dispose();
            _aisContext = null;

            // _occtWindow wraps an HWND created by HwndWrapper.
            // HwndWrapper.DestroyWindowCore will call NativeMethods.DestroyWindow on this HWND.
            // So, we only need to Dispose our WNT_Window object if it holds other resources,
            // but it primarily just wraps the handle.
            _occtWindow?.Dispose(); // Dispose the C# wrapper
            _occtWindow = null;

            _occtViewer?.SetViewOff();
            _occtViewer?.Dispose();
            _occtViewer = null;

            _graphicDriver?.Dispose();
            _graphicDriver = null;

            base.DestroyWindowCore(hwnd); // Call base to destroy the HWND created by HwndWrapper
        }

        // This is called by HwndWrapper's rendering loop
        protected override void Render(IntPtr windowHandle)
        {
            // Update camera animation if active
            if (_aisAnimationCamera != null) // Check if it's initialized
            {
                //_aisAnimationCamera.UpdateTimer(); // Update animation timer
                if (!_aisAnimationCamera.IsStopped())
                {
                    _occtView?.Redraw(); // Redraw if animation is running
                    return; // Don't do another redraw if animation handled it
                }
            }
            _occtView?.Redraw(); // Standard redraw if no animation or animation stopped
        }

        protected override void NativeResized(int newWidth, int newHeight)
        {
            if (this._occtView != null)
            {
                //System.Diagnostics.Debug.WriteLine($"WM_SIZE: {newWidth}x{newHeight}");
                this._occtView.MustBeResized();
                this._occtView.Update();
            }
        }

        private void InitializeOcct(IntPtr hwnd, int width, int height)
        {
            try
            {
                _graphicDriver = Graphic3d.CreateOpenGlDriver(false);
                _occtViewer = new V3d_Viewer(_graphicDriver);
                _occtViewer.SetDefaultLights();
                _occtViewer.SetLightOn();
                _occtViewer.SetDefaultViewProj(V3d_TypeOfOrientation.XposYposZpos);

                _aisContext = new AIS_InteractiveContext(_occtViewer);
                _aisContext.SetDisplayMode((int)AIS_DisplayMode.Shaded, true);

                // CRITICAL CHANGE: Instead of creating a new WNT_Window from scratch,
                // we create one that wraps the HWND provided by HwndWrapper.
                _occtWindow = new WNT_Window(hwnd); // Use the HWND created by HwndWrapper

                _occtView = _occtViewer.CreateView();
                _occtView.SetWindow(_occtWindow); // Associate with our WNT_Window wrapper
                                                  // Set up the viewer's background gradient
                Quantity_Color color1 = new Quantity_Color(0.3, 0.3, 0.3, Quantity_TypeOfColor.RGB);
                Quantity_Color color2 = new Quantity_Color(0.91, 0.93, 0.94, Quantity_TypeOfColor.RGB);
                _occtView.SetBgGradientColors(color1, color2, Aspect_GradientFillMethod.Vertical, false);
                _occtView.TriedronDisplay(
                    Aspect_TypeOfTriedronPosition.LEFT_LOWER,
                    new Quantity_Color(Quantity_NameOfColor.BLACK), 0.08,
                    V3d_TypeOfVisualization.ZBUFFER);

                if (width > 0 && height > 0) // Ensure valid dimensions
                {
                    // WNT_Window created from an existing HWND might not need explicit Map()
                    // or resize if HwndWrapper handles it.
                    // However, V3d_View still needs to know its size.
                    int currentWidth = 0;
                    int currentHeight = 0;
                    _occtWindow.Size(ref currentWidth, ref currentHeight);
                    if (currentWidth != width || currentHeight != height)
                    {
                        // This might not be necessary if HwndWrapper's window is already sized correctly.
                        // _occtWindow.SetPos(0,0, width, height);
                    }
                }

                InitializeViewCube();

                _occtView.MustBeResized(); // Tell V3d_View to adapt to the window size
                //_occtView.TriedronDisplay(Aspect_TypeOfTriedronPosition.LEFT_LOWER, new Quantity_Color(Quantity_NameOfColor.WHITE), 0.05, V3d_TypeOfVisualization.ZBUFFER);
                _occtView.SetImmediateUpdate(true);
                _occtView.Update();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing OCCT: {ex.Message}", "OCCT Init Error");
            }
        }

        private void InitializeViewCube()
        {
            if (_occtView == null || _aisContext == null)
                return;

            var bitmap = ResourceUtils.ReadBitmapFromResource(@"Visual\ViewCubeSides.png");
            if (bitmap == null)
            {
                Debug.WriteLine($"Could not load view cube texture from resource.");
                return;
            }

            var pixmap = PixMapHelper.ConvertFromBitmap(bitmap);
            if (pixmap == null)
            {
                Debug.WriteLine($"Could not load view cube texture into pixmap.");
                return;
            }

            _aisViewCube = new Macad.Occt.Ext.AIS_ViewCubeEx();
            _aisViewCube.SetTexture(pixmap);

            // Configure ViewCube
            _aisViewCube.SetSize(55); // Size in pixels, adjust as needed
            _aisViewCube.SetBoxFacetExtension(15); // Extension of facets for easier clicking
            _aisViewCube.SetAxesRadius(0); // Set to 0 if you don't want axes displayed from cube center
            _aisViewCube.SetInnerColor(new Quantity_Color(Quantity_NameOfColor.BLACK)); // Color of gaps
            _aisViewCube.SetTransformPersistence(new Graphic3d_TransformPers(
                Graphic3d_TransModeFlags.TriedronPers,
                Aspect_TypeOfTriedronPosition.RIGHT_UPPER, // Position in corner
                new Graphic3d_Vec2i(85, 85) // Offset from corner
            ));

            var color = new Quantity_Color();
            Quantity_Color.ColorFromHex("d9dfe5", color);
            _aisViewCube.BoxSideStyle().SetColor(color);

            Quantity_Color.ColorFromHex("93a4b6", color);
            _aisViewCube.BoxEdgeStyle().SetColor(color);

            Quantity_Color.ColorFromHex("a6b4c3", color);
            _aisViewCube.BoxCornerStyle().SetColor(color);

            var material = new Graphic3d_MaterialAspect(Graphic3d_NameOfMaterial.DEFAULT);
            material.SetAmbientColor(new Quantity_Color(0.8f, 0.8f, 0.8f, Quantity_TypeOfColor.RGB));
            material.SetDiffuseColor(new Quantity_Color(0.2f, 0.2f, 0.2f, Quantity_TypeOfColor.RGB));
            material.SetEmissiveColor(new Quantity_Color(Quantity_NameOfColor.BLACK));
            material.SetSpecularColor(new Quantity_Color(Quantity_NameOfColor.BLACK));
            _aisViewCube.SetMaterial(material);

            _aisViewCube.DynamicHilightAttributes().ShadingAspect().SetColor(new Quantity_Color(0.933f, 0.706f, 0.133f, Quantity_TypeOfColor.RGB));
            _aisViewCube.DynamicHilightAttributes().ShadingAspect().SetMaterial(material);

            // Animation (optional but recommended for smooth transitions)
            _aisAnimationCamera = new AIS_AnimationCamera(new TCollection_AsciiString("ViewCamera"), _occtView);
            _aisViewCube.SetViewAnimation(_aisAnimationCamera);
            _aisViewCube.SetFixedAnimationLoop(false); // Play animation once
            _aisViewCube.SetDuration(0.5); // Animation duration in seconds

            // Display it
            _aisContext.Display(_aisViewCube, false); // Display without immediate update

            // The ViewCube is an "always on top" object due to its transform persistence.
            // It doesn't need special Z-layer handling typically.
        }

        #endregion Methods

        #region OCCT Interactivity (using HwndWrapper events)

        protected override void Dispose(bool disposing)
        {
            _aisViewCube?.Dispose();
            _aisViewCube = null;

            _aisAnimationCamera?.Dispose();
            _aisAnimationCamera = null;

            if (_occtView != null)
            {
                _occtView.Remove();
                _occtView.Dispose();
                _occtView = null;
            }

            if (_occtWindow != null && !_occtWindow.IsDisposed())
            {
                if (_occtWindow.IsMapped())
                    _occtWindow.Unmap();
                _occtWindow.Dispose();
                _occtWindow = null;
            }

            this.HwndLButtonDown -= OnHwndLButtonDown;
            this.HwndLButtonUp -= OnHwndLButtonUp;
            this.HwndRButtonDown -= OnHwndRButtonDown;
            this.HwndRButtonUp -= OnHwndRButtonUp;
            this.HwndMouseMove -= OnHwndMouseMove;
            this.HwndMouseWheel -= OnHwndMouseWheel;
            // Add MButton for panning if you prefer
            this.HwndMButtonDown -= OnHwndMButtonDown;
            this.HwndMButtonUp -= OnHwndMButtonUp;

            GC.SuppressFinalize(this);
        }

        private void OnHwndLButtonDown(object sender, HwndMouseEventArgs e)
        {
            if (_occtView == null || _aisContext == null) // Ensure context is also available
                return;

            this.Focus();
            System.Windows.Point relativePos = PointFromScreen(e.MouseState.ScreenPosition);
            int x = (int)relativePos.X;
            int y = (int)relativePos.Y;

            // --- ViewCube Interaction Logic ---
            // Move AIS context to the click position to detect objects
            _aisContext.MoveTo(x, y, _occtView, false);

            /*if (_aisContext.HasDetected())
            {
                AIS_InteractiveObject detectedIO = _aisContext.DetectedInteractive();
                AIS_InteractiveObject detectedOwner = _aisContext.DetectedOwner();
                if (detectedIO is AIS_ViewCube viewCube
            && detectedOwner is AIS_ViewCubeOwner viewCubeOwner)
                {
                    if (!viewportController.LockedToPlane)
                    {
                        viewCube.HandleClick(viewCubeOwner);
                    }
                    return;
                }

                if (detectedIO != null && detectedIO == _aisViewCube) // Check if the detected object IS the ViewCube
                {
                    // Get the specific owner (face, edge, corner) of the ViewCube that was clicked

                    AIS_InteractiveObject detectedOwner = _aisContext.DetectedOwner();
                    if (detectedOwner != null && detectedOwner.IsKind(AIS_ViewCubeOwner.get_type_descriptor()))
                    {
                        AIS_ViewCubeOwner viewCubeOwner = AIS_ViewCubeOwner.DownCast(detectedOwner);
                        if (viewCubeOwner != null)
                        {
                            // This is the magic call: it tells the ViewCube to handle the click.
                            // It will determine which face/edge/corner was clicked and
                            // animate the main _occtView to the corresponding orientation.
                            _aisViewCube.HandleClick(viewCubeOwner);

                            // The animation will trigger redraws via the Render() method.
                            // No need to explicitly call _occtView.Redraw() here if animation is enabled.
                            return; // Click was handled by the ViewCube, so don't start view rotation.
                        }
                    }
                }
            }*/
            // --- End ViewCube Interaction Logic ---

            // If the click was not on the ViewCube (or ViewCube interaction failed), proceed with normal view rotation.
            _occtView.StartRotation(x, y);
            _isRotating = true;
            _lastMousePosition = relativePos;
        }

        private void OnHwndLButtonUp(object sender, HwndMouseEventArgs e)
        {
            _isRotating = false;
        }

        private void OnHwndRButtonDown(object sender, HwndMouseEventArgs e) // Using Right for Pan
        {
            if (_occtView == null)
                return;
            this.Focus();
            System.Windows.Point relativePos = PointFromScreen(e.MouseState.ScreenPosition);
            _isPanning = true;
            _lastMousePosition = relativePos;
        }

        private void OnHwndRButtonUp(object sender, HwndMouseEventArgs e)
        {
            _isPanning = false;
        }

        // Example for Middle Mouse Button Pan
        private void OnHwndMButtonDown(object sender, HwndMouseEventArgs e)
        {
            if (_occtView == null)
                return;
            this.Focus();
            System.Windows.Point relativePos = PointFromScreen(e.MouseState.ScreenPosition);
            // If you want Middle button for pan instead of/in addition to Right:
            // _isPanning = true;
            // _lastMousePosition = relativePos;
            // For now, let's use it for a different action or leave it
            System.Diagnostics.Debug.WriteLine("Middle Mouse Down (OCCT)");
        }

        private void OnHwndMButtonUp(object sender, HwndMouseEventArgs e)
        {
            // if (_isPanning && e.MouseState.MiddleButton == System.Windows.Input.MouseButtonState.Released)
            // {
            //    _isPanning = false;
            // }
            System.Diagnostics.Debug.WriteLine("Middle Mouse Up (OCCT)");
        }

        private void OnHwndMouseMove(object sender, HwndMouseEventArgs e)
        {
            if (_occtView == null)
                return;
            System.Windows.Point relativePos = PointFromScreen(e.MouseState.ScreenPosition);

            if (_isRotating)
            {
                _occtView.Rotation((int)relativePos.X, (int)relativePos.Y);
                // Render method will handle redraw
            }
            else if (_isPanning)
            {
                int deltaX = (int)(relativePos.X - _lastMousePosition.X);
                int deltaY = (int)(relativePos.Y - _lastMousePosition.Y);
                _occtView.Pan(deltaX, -deltaY, 1.0, true);
            }
            _lastMousePosition = relativePos;
        }

        private void OnHwndMouseWheel(object sender, HwndMouseEventArgs e)
        {
            if (_occtView == null)
                return;

            System.Windows.Point relativePos = PointFromScreen(e.MouseState.ScreenPosition);

            double zoomFactor = 1.1;
            if (e.WheelDelta < 0) // Wheel scrolled down (towards user) -> zoom out
            {
                zoomFactor = 1.0 / zoomFactor;
            }

            _occtView.StartZoomAtPoint((int)relativePos.X, (int)relativePos.Y);
            _occtView.SetZoom(zoomFactor, true);
            // Render method will handle redraw
        }

        #endregion OCCT Interactivity (using HwndWrapper events)
    }
}