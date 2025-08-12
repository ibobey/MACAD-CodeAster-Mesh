using System;
using System.Windows.Input;

namespace CodeAsterMesh
{
    public class HwndMouseEventArgs : EventArgs
    {
        #region Constructors

        public HwndMouseEventArgs(HwndMouseState state, int wheelDelta = 0, MouseButton? changedButton = null)
        {
            MouseState = state;
            WheelDelta = wheelDelta;
            ChangedButton = changedButton;
        }

        #endregion Constructors

        #region Properties

        public HwndMouseState MouseState
        {
            get;
        }

        public int WheelDelta
        {
            get;
        }

        public MouseButton? ChangedButton
        {
            get;
        }

        #endregion Properties

        // For double clicks
    }
}