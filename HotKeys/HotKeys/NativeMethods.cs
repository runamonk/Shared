using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Resolve.HotKeys
{
    public static class NativeMethods
    {
        public const int WmHotkey = 0x0312;
        public const int ErrorSuccess = 0;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern short GlobalAddAtom(string atomName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern short GlobalDeleteAtom(short atom);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        internal static extern void SetLastError(int dwErrorCode);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}