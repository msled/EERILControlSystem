using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Threading;
using System.ComponentModel;
using Microsoft.Xna.Framework.Input;
using System.Windows.Threading;
namespace EERIL.ControlSystem
{
    public delegate void ControllerConnectionChangedHandler(bool connected);
    public delegate void ButtonStateChangedHandler(Button button, bool pressed);
    public delegate void ControllerAxisChangedHandler(ControllerJoystick joystick, ControllerJoystickAxis axis, byte oldValue, byte newValue);
    public delegate void TriggerStateChangedHandler(Trigger trigger, bool pressed);
    public enum Button
    {
        X, Y, A, B, LB, RB, DU, DD
    }
    public enum Trigger
    {
        Left, Right
    }
    public enum ControllerJoystickAxis
    {
        X, Y
    }
    public enum ControllerJoystick
    {
        Left, Right
    }
    public enum ControllerIndex
    {
        One, Two, Three, Four
    }
    
    public class Controller : IDisposable
    {
        private GamePadState state;
        public event ControllerAxisChangedHandler AxisChanged;
        public event ControllerConnectionChangedHandler ConnectionChanged;
        public event ButtonStateChangedHandler ButtonStateChanged;
        public event TriggerStateChangedHandler TriggerStateChanged;
        private readonly PlayerIndex playerIndex;
        private bool connected = false;
        private Thread monitorThread;
        private bool run = true;

        public bool Y
        {
            get
            {
                return state.Buttons.Y == ButtonState.Pressed;
            }
        }

        public bool A
        {
            get
            {
                return state.Buttons.A == ButtonState.Pressed;
            }
        }

        public bool B
        {
            get
            {
                return state.Buttons.B == ButtonState.Pressed;
            }
        }

        public bool LB
        {
            get
            {
                return state.Buttons.LeftShoulder == ButtonState.Pressed;
            }
        }

        public bool RB
        {
            get
            {
                return state.Buttons.RightShoulder == ButtonState.Pressed;
            }
        }

        public bool LeftTrigger
        {
            get
            {
                return state.Triggers.Left > 0;
            }
        }

        public bool DU
        {
            get
            {
                return state.DPad.Up == ButtonState.Pressed;
            }
        }

        public bool DD
        {
            get
            {
                return state.DPad.Down == ButtonState.Pressed;
            }
        }


        public bool RightTrigger
        {
            get
            {
                return state.Triggers.Right > 0;
            }
        }
        protected void OnControllerConnectionChanged(bool connected)
        {
            if (ConnectionChanged != null)
            {
                ControllerConnectionChangedHandler eventHandler = ConnectionChanged;
                Delegate[] delegates = eventHandler.GetInvocationList();
                foreach (ControllerConnectionChangedHandler handler in delegates)
                {
                    DispatcherObject dispatcherObject = handler.Target as DispatcherObject;
                    if (dispatcherObject != null && !dispatcherObject.CheckAccess())
                    {
                        dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, connected);
                    }
                    else
                        handler(connected);
                }
            }
        }

        protected void OnTriggerStateChanged(Trigger trigger, bool pressed)
        {
            if (ButtonStateChanged != null)
            {
                TriggerStateChangedHandler eventHandler = TriggerStateChanged;
                Delegate[] delegates = eventHandler.GetInvocationList();
                foreach (TriggerStateChangedHandler handler in delegates)
                {
                    DispatcherObject dispatcherObject = handler.Target as DispatcherObject;
                    if (dispatcherObject != null && !dispatcherObject.CheckAccess())
                    {
                        dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, trigger, pressed);
                    }
                    else
                        handler(trigger, pressed);
                }
            }
        }

        protected void OnButtonStateChanged(Button button, bool pressed)
        {
            if (ButtonStateChanged != null)
            {
                ButtonStateChangedHandler eventHandler = ButtonStateChanged;
                Delegate[] delegates = eventHandler.GetInvocationList();
                foreach (ButtonStateChangedHandler handler in delegates)
                {
                    DispatcherObject dispatcherObject = handler.Target as DispatcherObject;
                    if (dispatcherObject != null && !dispatcherObject.CheckAccess())
                    {
                        dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, button, pressed);
                    }
                    else
                        handler(button, pressed);
                }
            }
        }

        protected void OnControllerAxisChanged(ControllerJoystick joystick, ControllerJoystickAxis axis, byte oldValue, byte newValue)
        {
            if (AxisChanged != null)
            {
                ControllerAxisChangedHandler eventHandler = AxisChanged;
                Delegate[] delegates = eventHandler.GetInvocationList();
                foreach (ControllerAxisChangedHandler handler in delegates)
                {
                    DispatcherObject dispatcherObject = handler.Target as DispatcherObject;
                    if (dispatcherObject != null && !dispatcherObject.CheckAccess())
                    {
                        dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, joystick, axis, oldValue, newValue);
                    }
                    else
                        handler(joystick, axis, oldValue, newValue);
                }
            }
        }

        public Controller(ControllerIndex index)
        {
            playerIndex = (Microsoft.Xna.Framework.PlayerIndex)Enum.Parse(typeof(Microsoft.Xna.Framework.PlayerIndex), Enum.GetName(typeof(ControllerIndex), index));
            monitorThread = new Thread(new ThreadStart(ControllerMonitor));
            monitorThread.IsBackground = true;
            monitorThread.Start();
        }

        private void ControllerMonitor()
        {
            byte leftX = 0, leftY = 0, rightX = 0, rightY = 0;
            bool y = false, a = false, b = false, rightTrigger = false, leftTrigger = false, rb = false, lb = false, du = false, dd = false;
            while (run)
            {
                state = GamePad.GetState(playerIndex);
                if (connected != state.IsConnected)
                {
                    connected = state.IsConnected;
                    OnControllerConnectionChanged(connected);
                }
                if (connected)
                {
                    byte newLeftX = Convert.ToByte((state.ThumbSticks.Left.X + 1) * 90);
                    if (leftX != newLeftX)
                    {
                        OnControllerAxisChanged(ControllerJoystick.Left, ControllerJoystickAxis.X, leftX, newLeftX);
                        leftX = newLeftX;
                    }
                    byte newLeftY = Convert.ToByte((state.ThumbSticks.Left.Y + 1) * 90);
                    if (leftY != newLeftY)
                    {
                        OnControllerAxisChanged(ControllerJoystick.Left, ControllerJoystickAxis.Y, leftY, newLeftY);
                        leftY = newLeftY;
                    }
                    byte newRightX = Convert.ToByte((state.ThumbSticks.Right.X + 1) * 90);
                    if (rightX != newRightX)
                    {
                        OnControllerAxisChanged(ControllerJoystick.Right, ControllerJoystickAxis.X, rightX, newRightX);
                        rightX = newRightX;
                    }
                   
                    byte newRightY = Convert.ToByte((state.ThumbSticks.Right.Y + 1) * 90);
                    if (rightY != newRightY)
                    {
                        OnControllerAxisChanged(ControllerJoystick.Right, ControllerJoystickAxis.Y, rightY, newRightY);
                        rightY = newRightY;
                    }
                    if (y != Y)
                    {
                        y = Y;
                        OnButtonStateChanged(Button.Y, y);
                    }
                    if (a != A)
                    {
                        a = A;
                        OnButtonStateChanged(Button.A, a);
                    }
                    if (b != B)
                    {
                        b = B;
                        OnButtonStateChanged(Button.B, b);
                    }
                    if (lb != LB)
                    {
                        lb = LB;
                        OnButtonStateChanged(Button.LB, lb);
                    } 
                    if (rb != RB)
                    {
                        rb = RB;
                        OnButtonStateChanged(Button.RB, rb);
                    }
                    if (du != DU)
                    {
                        du = DU;
                        OnButtonStateChanged(Button.DU, du);
                    }
                    if (dd != DD)
                    {
                        dd = DD;
                        OnButtonStateChanged(Button.DD, dd);
                    }
                    if (rightTrigger != RightTrigger)
                    {
                        rightTrigger = RightTrigger;
                        OnTriggerStateChanged(Trigger.Right, rightTrigger);
                    }
                    if (leftTrigger != LeftTrigger)
                    {
                        leftTrigger = LeftTrigger;
                        OnTriggerStateChanged(Trigger.Left, leftTrigger);
                    }
                }
                Thread.Yield();
            }
        }

        #region IDisposable Members

        public virtual void Dispose()
        {
            run = false;
            monitorThread.Suspend();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
