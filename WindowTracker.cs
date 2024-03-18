using System;
using System.Runtime.InteropServices;

namespace zuulWindowTracker
{
    internal class WindowTracker
    {
        public delegate void WindowChangedDelegate(IntPtr hwnd);

        // Constants from winuser.h
        private const uint EventSystemForeground = 3;
        private const uint WineventOutofcontext = 0;
        private readonly IntPtr _hhook;

        // Need to ensure delegate is not collected while we're using it,
        // storing it in a class field is simplest way to do this.
        private readonly WinEventDelegate _procDelegate;

        public WindowTracker()
        {
            _procDelegate = WinEventProc;
            // Listen for foreground changes across all processes/threads on current desktop...
            _hhook = SetWinEventHook(EventSystemForeground, EventSystemForeground, IntPtr.Zero, _procDelegate, 0, 0,
                WineventOutofcontext);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        public event WindowChangedDelegate WindowChanged;

        ~WindowTracker()
        {
            UnhookWinEvent(_hhook);
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime)
        {
            WindowChanged?.Invoke(hwnd);
        }

        // Delegate and imports from pinvoke.net:
        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject,
            int idChild, uint dwEventThread, uint dwmsEventTime);
    }
}