using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace Utility
{
    internal class Funcs
    {
        public const int SwRestore = 9;

        //http://www.pinvoke.net/default.aspx/user32/ShowWindow.html
        public const int SwShownoactivate = 4;

        private const int HwndTopmost = -1;

        private const uint SwpNoactivate = 0x0010;

        private const uint SwpNomove = 0x0002;

        private const uint SwpNoreposition = 0x0200;

        private const uint SwpNosize = 0x0001;

        public static bool StartWithWindows
        {
            get
            {
                var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\", true);
                var k = key?.GetValue(GetFileName());
                return k != null;
            }
            set
            {
                var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\", true);
                switch (value)
                {
                    case false when key is null:
                        return;
                    case true:
                        key?.SetValue(GetFileName(), '"' + GetFilePathAndName() + '"');
                        break;
                    default:
                        key.DeleteValue(GetFileName());
                        break;
                }

                key?.Close();
            }
        }

        public static ToolStripMenuItem AddMenuItem(ToolStrip menu, string caption, EventHandler @event)
        {
            switch (caption)
            {
                case "-":
                    menu.Items.Add(new ToolStripSeparator());
                    return null;
            }

            var t = new ToolStripMenuItem(caption);
            if (@event != null) t.Click += @event;

            menu.Items.Add(t);
            return t;
        }

        public static string AppPath(string fileName)
        {
            return Path.GetDirectoryName(Application.ExecutablePath) + "\\" + fileName;
        }

        public static string AppPath()
        {
            return Path.GetDirectoryName(Application.ExecutablePath);
        }

        public static string BrowseForFile(string filterStr = "All files (*.*)|*.*")
        {
            var fd = new OpenFileDialog
            {
                Multiselect = false,
                Filter = filterStr,
                FilterIndex = 1,
                CheckFileExists = true,
                CheckPathExists = true,
                DereferenceLinks = false
            };

            var dr = fd.ShowDialog();

            switch (dr)
            {
                case DialogResult.OK:
                    return fd.FileName;
                default:
                    return "";
            }
        }

        public static string GeneratePassword(bool incNumbers, bool incSymbols, int size)
        {
            var alpha = "abcdefghijklmnopqrstuvwxyz";
            var numbers = "0123456789";
            var symbols = "!@#$%^&*-+=:;,";
            var src = alpha + (incNumbers ? numbers : "") + (incSymbols ? symbols : "");

            var sb = new StringBuilder();
            var rng = new Random();

            for (var i = 0; i < size; i++)
            {
                var c = src[rng.Next(0, src.Length)];
                sb.Append(c);
            }

            var s = sb.ToString();

            // Uppercase one random alpha character.
            while (true)
            {
                var r = rng.Next(1, s.Length);
                switch (alpha.IndexOf(s[r]) <= -1)
                {
                    case true:
                        continue;
                }

                s = s.Substring(0, r) + s.Substring(r, 1).ToUpper() + s.Substring(r + 1);
                break;
            }

            return s;
        }

        public static FileVersionInfo GetFileInfo(string fileName)
        {
            return FileVersionInfo.GetVersionInfo(fileName);
        }

        public static string GetFileName()
        {
            return Path.GetFileName(GetFilePathAndName());
        }

        public static string GetFilePathAndName()
        {
            return Application.ExecutablePath;
        }

        public static string[] GetFiles(string path, string searchPattern)
        {
            var files = searchPattern != ""
                ? Directory.GetFiles(path, searchPattern).OrderBy(f => new FileInfo(f).LastWriteTime).ToArray()
                : Directory.GetFiles(path).OrderBy(f => new FileInfo(f).LastWriteTime).ToArray();

            return files;
        }

        public static string GetName()
        {
            return Assembly.GetExecutingAssembly().GetName().Name ?? "";
        }

        public static string GetNameAndVersion()
        {
            var v = GetVersion();
            if (v == null) return "";

            return (GetName() + " " ?? "") + v.Major + "." +
                   File.GetLastWriteTime(GetFilePathAndName()).ToString("ddMMyyyy.HHmm");
        }

        public static Version GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        public static string GetWebsiteFavIcon(string url)
        {
            var result = "";
            switch (url.ToLower().StartsWith("http://"))
            {
                case false when !url.ToLower().StartsWith("https://"):
                    return result;
            }

            var baseDomain = new Uri(url).GetLeftPart(UriPartial.Authority);
            var w = (HttpWebRequest)WebRequest.Create(baseDomain + "/favicon.ico");
            w.AllowAutoRedirect = true;
            try
            {
                var r = (HttpWebResponse)w.GetResponse();
                var s = r.GetResponseStream();
                if (s != null)
                {
                    var ico = Image.FromStream(s);
                    result = Convert.ToBase64String(ImageToByteArray(ico));
                }
            }
            catch (WebException)
            {
            }

            return result;
        }

        public static byte[] ImageToByteArray(Image image)
        {
            var ms = new MemoryStream();
            image.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }

        public static bool IsRunningDoShow()
        {
            switch (Debugger.IsAttached)
            {
                case true:
                    return false;
            }

            var current = Process.GetCurrentProcess();
            var processes = Process.GetProcessesByName(current.ProcessName);

            foreach (var process in processes)
                if (process.Id != current.Id)
                    if (Assembly.GetExecutingAssembly().Location.Replace("/", "\\") == current.MainModule?.FileName)
                    {
                        ShowWindow(process.MainWindowHandle, SwRestore);
                        return true;
                    }

            return false;
        }

        public static bool IsSame(byte[] img1, byte[] img2)
        {
            if (img1 == null || img2 == null) return false;

            return img1.SequenceEqual(img2);
        }

        public static bool IsSame(Image img1, byte[] img2)
        {
            if (img1 == null || img2 == null) return false;

            var b1 = ImageToByteArray(img1);

            return b1.SequenceEqual(img2);
        }

        public static bool IsUrl(string s)
        {
            return (s.ToLower().StartsWith("http://") || s.ToLower().StartsWith("https://") ||
                    s.ToLower().StartsWith("ftp://")) && Uri.IsWellFormedUriString(s, UriKind.RelativeOrAbsolute);
        }

        public static bool IsWindows7()
        {
            var ver = Environment.OSVersion.Version;
            return ver.Major == 6 && ver.Minor <= 1;
        }

        public static Keys StringToKey(string key)
        {
            return (Keys)Enum.Parse(typeof(Keys), key);
        }

        private static void MoveFormToPoint(Form form, Point p)
        {
            var workingArea = Screen.GetWorkingArea(p);

            //Vert
            if (p.Y + form.Size.Height > workingArea.Bottom) p.Y -= p.Y + form.Size.Height - workingArea.Bottom;

            //Horz
            if (p.X + form.Size.Width > workingArea.Right) p.X -= p.X + form.Size.Width - workingArea.Right;

            if (p.Y < workingArea.Top)
                p.Y = workingArea.Top;

            if (p.X < workingArea.Left)
                p.X = workingArea.Left;

            form.Location = p;
        }

        public static void MoveFormInBounds(Form form)
        {
            MoveFormToPoint(form, form.Location);
        }

        public static void MoveFormToCursor(Form form)
        {
            MoveFormToPoint(form, Cursor.Position);
        }

        public static string RandomString(int size, bool lowerCase)
        {
            const string src = "abcdefghijklmnopqrstuvwxyz0123456789";
            var sb = new StringBuilder();
            var rng = new Random();
            for (var i = 0; i < size; i++)
            {
                var c = src[rng.Next(0, src.Length)];
                sb.Append(c);
            }

            switch (lowerCase)
            {
                case true:
                    return sb.ToString().ToLower();
                default:
                    return sb.ToString();
            }
        }

        public static Image ScaleImage(Image image, int maxWidth, int maxHeight)
        {
            switch (image.Height <= maxHeight)
            {
                case true when image.Width <= maxWidth:
                    return image;
            }

            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);
            var newWidth = image.Width;
            var newHeight = image.Height;

            var i = (int)(image.Width * ratio);
            switch (i > 0)
            {
                case true:
                    newWidth = i;
                    break;
            }

            i = (int)(image.Height * ratio);
            switch (i > 0)
            {
                case true:
                    newHeight = i;
                    break;
            }

            var newImage = new Bitmap(newWidth, newHeight);
            using (var graphics = Graphics.FromImage(newImage))
            {
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            return newImage;
        }

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, bool wParam, int lParam);

        public static void SetColors(Control control)
        {
            if (UseLightThemeMode())
            {
                control.BackColor = SystemColors.ControlLightLight;
                control.ForeColor = Color.Black;
            }
            else
            {
                control.ForeColor = Color.White;
                control.BackColor = Color.FromArgb(45, 45, 48);
            }
        }

        public static void ShowInactiveTopmost(Form frm)
        {
            ShowWindow(frm.Handle, SwShownoactivate);
            //SetWindowPos(frm.Handle.ToInt32(), HWND_TOPMOST, frm.Left, frm.Top, frm.Width, frm.Height, SWP_NOACTIVATE);
            SetWindowPos(frm.Handle.ToInt32(), HwndTopmost, 0, 0, 0, 0,
                SwpNoactivate | SwpNomove | SwpNosize | SwpNoreposition);
        }

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static bool UseLightThemeMode()
        {
            try
            {
                var o = Registry.GetValue(
                    "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize",
                    "AppsUseLightTheme", null);
                return o is null || o.ToString() != "0";
            }
            catch
            {
                return true;
            }
        }

        public static void Wait(int ms)
        {
            var waitTimer = new Timer();
            switch (ms <= 0)
            {
                case true:
                    return;
            }

            waitTimer.Interval = ms;
            waitTimer.Enabled = true;
            waitTimer.Start();

            waitTimer.Tick += (s, e) =>
            {
                waitTimer.Enabled = false;
                waitTimer.Stop();
                waitTimer.Dispose();
            };
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        private static extern bool SetWindowPos(
            int hWnd, // Window handle
            int hWndInsertAfter, // Placement-order handle
            int x, // Horizontal position
            int y, // Vertical position
            int cx, // Width
            int cy, // Height
            uint uFlags); // Window positioning flags
    }
}