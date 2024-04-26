using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Resolve.HotKeys
{
    public class HotKey : IMessageFilter, IDisposable
    {
        private bool _disposed;
        private short? _id;

        public HotKey(Keys key) : this(key, ModifierKey.None, IntPtr.Zero) { }

        public HotKey(Keys key, ModifierKey modifiers) : this(key, modifiers, IntPtr.Zero) { }

        public HotKey(Keys key, ModifierKey modifiers, IntPtr handle)
        {
            Key = key;
            Modifiers = modifiers;
            Handle = handle;
            _disposed = true;
        }

        private Keys Key { get; }

        public ModifierKey Modifiers { get; }

        public short? Id => _id;

        public IntPtr Handle { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool PreFilterMessage(ref Message m)
        {
            switch (m.Msg)
            {
                case NativeMethods.WmHotkey:
                    if (m.HWnd == Handle && m.WParam == (IntPtr)Id && Pressed != null)
                    {
                        Pressed(this, EventArgs.Empty);
                        return true;
                    }

                    break;
            }

            return false;
        }

        /// <summary>
        ///     Unregister the hotkey.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            // Protect from being called multiple times. 
            if (_disposed) return;

            if (disposing)
                // Removes a message filter from the message pump of the application. 
                Unregister();

            _disposed = true;
        }

        public void Register()
        {
            if (_id.HasValue) return;
            NativeMethods.SetLastError(NativeMethods.ErrorSuccess);
            _id = NativeMethods.GlobalAddAtom(GetHashCode().ToString());

            int error = Marshal.GetLastWin32Error();


            if (error != NativeMethods.ErrorSuccess)
            {
                _id = null;
                throw new Win32Exception(error);
            }

            uint vk = unchecked((uint)(Key & ~Keys.Modifiers));
            NativeMethods.SetLastError(NativeMethods.ErrorSuccess);
            bool result = NativeMethods.RegisterHotKey(Handle, _id.Value, (uint)Modifiers, vk);

            error = Marshal.GetLastWin32Error();

            if (error != 0)
            {
                _id = null;
                throw new Win32Exception(error);
            }

            if (result)
                Application.AddMessageFilter(this);
            else
                _id = null;
        }

        public void Unregister()
        {
            if (_id == null) return;
            NativeMethods.SetLastError(NativeMethods.ErrorSuccess);
            bool result = NativeMethods.UnregisterHotKey(Handle, Id.Value);
            int error = Marshal.GetLastWin32Error();
            if (error != NativeMethods.ErrorSuccess) throw new Win32Exception(error);
            NativeMethods.SetLastError(NativeMethods.ErrorSuccess);
            NativeMethods.GlobalDeleteAtom(_id.Value);
            error = Marshal.GetLastWin32Error();
            if (error != NativeMethods.ErrorSuccess) throw new Win32Exception(error);
            _id = null;
            Application.RemoveMessageFilter(this);
        }

        public event EventHandler Pressed;
    }
}