using System.Windows;
using System.Windows.Input;

namespace CodeAsterMesh
{
    public class HwndMouseState
    {
        #region Properties

        public Point ScreenPosition
        {
            get; set;
        }

        public MouseButtonState LeftButton
        {
            get; set;
        }

        public MouseButtonState MiddleButton
        {
            get; set;
        }

        public MouseButtonState RightButton
        {
            get; set;
        }

        public MouseButtonState X1Button
        {
            get; set;
        }

        public MouseButtonState X2Button
        {
            get; set;
        }

        #endregion Properties

        // Add other properties if needed by HwndMouseEventArgs
    }
}