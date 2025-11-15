using System;
using Standard;
using System.Runtime.InteropServices;

namespace Standard
{
    internal static class SendMouseInput
    {
        public static void LButtonDown()
        {

            INPUT i = new INPUT();
            i.type = (uint)INPUT_TYPE.MOUSE;
            i.mi.dx = 0;
            i.mi.dy = 0;
            i.mi.dwFlags = (int)MOUSEEVENTF.LEFTDOWN;
            i.mi.dwExtraInfo = IntPtr.Zero;
            i.mi.mouseData = 0;
            i.mi.time = 0;
            //send the input 
            NativeMethods.SendInput(1, ref i, Marshal.SizeOf(i));
   
        }

        public static void LButtonUp()
        {
            INPUT i = new INPUT();
            i.type = (uint)INPUT_TYPE.MOUSE;
            i.mi.dx = 0;
            i.mi.dy = 0;
            i.mi.dwFlags = (int)MOUSEEVENTF.LEFTUP;
            i.mi.dwExtraInfo = IntPtr.Zero;
            i.mi.mouseData = 0;
            i.mi.time = 0;
            //send the input 
            NativeMethods.SendInput(1, ref i, Marshal.SizeOf(i));
        }

        public static void Click()
        {
            LButtonDown();
            LButtonUp();
        }
    }
}
