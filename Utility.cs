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
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\", true);
                object k = key?.GetValue(GetFileName());
                return k != null;
            }
            set
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\", true);
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

            ToolStripMenuItem t = new ToolStripMenuItem(caption);
            if (@event != null) t.Click += @event;

            menu.Items.Add(t);
            return t;
        }

        public static string AppPath(string fileName) { return Path.GetDirectoryName(Application.ExecutablePath) + "\\" + fileName; }

        public static string AppPath() { return Path.GetDirectoryName(Application.ExecutablePath); }

        public static string BrowseForFile(string filterStr = "All files (*.*)|*.*")
        {
            OpenFileDialog fd = new OpenFileDialog
            {
                Multiselect = false,
                Filter = filterStr,
                FilterIndex = 1,
                CheckFileExists = true,
                CheckPathExists = true,
                DereferenceLinks = false
            };

            DialogResult dr = fd.ShowDialog();

            switch (dr)
            {
                case DialogResult.OK:
                    return fd.FileName;
                default:
                    return "";
            }
        }

        public static void Clear(Array arr) { Array.Clear(arr, 0, arr.Length); }


        public static string GeneratePassword(bool incNumbers, bool incSymbols, int size)
        {
            string alpha = "abcdefghijklmnopqrstuvwxyz";
            string numbers = "0123456789";
            string symbols = "!@#$%^&*-+=:;,";
            string src = alpha + (incNumbers ? numbers : "") + (incSymbols ? symbols : "");

            StringBuilder sb = new StringBuilder();
            Random rng = new Random();

            for (int i = 0; i < size; i++)
            {
                char c = src[rng.Next(0, src.Length)];
                sb.Append(c);
            }

            string s = sb.ToString();

            // Uppercase one random alpha character.
            while (true)
            {
                int r = rng.Next(1, s.Length);
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

        public static FileVersionInfo GetFileInfo(string fileName) { return FileVersionInfo.GetVersionInfo(fileName); }

        public static string GetFileName() { return Path.GetFileName(GetFilePathAndName()); }

        public static string GetFilePathAndName() { return Application.ExecutablePath; }

        public static string[] GetFiles(string path, string extensions)
        {
            string[] exts = extensions.Split(',');
            string[] files = new string[0];

            files = Directory.GetFiles(path, "*.*")
                             .Where(f =>
                             {
                                 return (exts.Count() == 0 || exts.Contains(f.Substring(f.IndexOf('.') + 1), StringComparer.OrdinalIgnoreCase)) && !files.Any(a =>
                                 {
                                     return Path.GetFileName(a).ToLower() == Path.GetFileName(f).ToLower();
                                 });
                             })
                             .OrderBy(f => new FileInfo(f).LastWriteTime)
                             .ToArray();

            string[] folders = Directory.GetDirectories(path);

            foreach (string folder in folders) files = files.Concat(GetFiles(folder, extensions)).ToArray();

            return files;
        }

        public static string GetName() { return Assembly.GetExecutingAssembly().GetName().Name ?? ""; }

        public static string GetNameAndVersion()
        {
            Version v = GetVersion();
            if (v == null) return "";

            return (GetName() + " " ?? "") + v.Major + "." + File.GetLastWriteTime(GetFilePathAndName()).ToString("ddMMyyyy.HHmm");
        }

        public static Form GetParentForm(Control control)
        {
            if (control == null) return null;
            if (control is Form) return (Form)control;
            return GetParentForm(control.Parent);
        }

        public static Version GetVersion() { return Assembly.GetExecutingAssembly().GetName().Version; }

        public static string GetWebsiteFavIcon(string url)
        {
            try
            {
                return Convert.ToBase64String(ImageToByteArray(GetWebsiteFavIconAsImage(url)));
            }
            catch (WebException)
            {
                return "";
            }
        }

        public static Image GetWebsiteFavIconAsImage(string url, bool askGoogle = true)
        {
            if (!url.ToLower().StartsWith("http://") && !url.ToLower().StartsWith("https://"))
                return null;

            string baseDomain = new Uri(url).GetLeftPart(UriPartial.Authority);
            HttpWebRequest w = (HttpWebRequest)WebRequest.Create(baseDomain + "/favicon.ico");

            w.AllowAutoRedirect = true;
            try
            {
                HttpWebResponse r = (HttpWebResponse)w.GetResponse();
                Stream s = r.GetResponseStream();
                if (s != null) return Image.FromStream(s);
            }
            catch (WebException)
            {
                if (!askGoogle) return null;

                // lets ask Google for it.
                try
                {
                    return GetWebsiteFavIconAsImage("http://www.google.com/s2/favicons?sz=32&domain_url=" + baseDomain.Replace("http", "").Replace(":", "").Replace("/", ""), false);
                }
                catch
                {
                }
            }

            return null;
        }

        public static byte[] ImageToByteArray(Image image)
        {
            MemoryStream ms = new MemoryStream();
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

            Process current = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(current.ProcessName);

            foreach (Process process in processes)
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

            byte[] b1 = ImageToByteArray(img1);

            return b1.SequenceEqual(img2);
        }

        public static bool IsUrl(string s)
        {
            return (s.ToLower().StartsWith("http://") || s.ToLower().StartsWith("https://") || s.ToLower().StartsWith("ftp://")) && Uri.IsWellFormedUriString(s, UriKind.RelativeOrAbsolute);
        }

        public static bool IsWindows7()
        {
            Version ver = Environment.OSVersion.Version;
            return ver.Major == 6 && ver.Minor <= 1;
        }

        public static void MoveFormToCursor(Form form) { MoveFormToPoint(form, Cursor.Position); }

        private static void MoveFormToPoint(Form form, Point p)
        {
            Rectangle workingArea = Screen.GetWorkingArea(p);

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

        public static string ParseEnvironmentVars(string str)
        {
            int c = str.Count(s => s == '%');
            if (c < 2 || c % 2 != 0) return str;

            string envar;

            while (str.Contains("%"))
            {
                int start = str.IndexOf("%");
                envar = str.Substring(start, str.IndexOf("%", start + 1) + 1);
                str = str.Replace(envar, Environment.ExpandEnvironmentVariables(envar));
            }

            return str;
        }

        public static string RandomString(int size, bool lowerCase)
        {
            const string src = "abcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder sb = new StringBuilder();
            Random rng = new Random();
            for (int i = 0; i < size; i++)
            {
                char c = src[rng.Next(0, src.Length)];
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

            double ratioX = (double)maxWidth / image.Width;
            double ratioY = (double)maxHeight / image.Height;
            double ratio = Math.Min(ratioX, ratioY);
            int newWidth = image.Width;
            int newHeight = image.Height;

            int i = (int)(image.Width * ratio);
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

            Bitmap newImage = new Bitmap(newWidth, newHeight);
            using (Graphics graphics = Graphics.FromImage(newImage))
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

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        private static extern bool SetWindowPos(int  hWnd,            // Window handle
                                                int  hWndInsertAfter, // Placement-order handle
                                                int  x,               // Horizontal position
                                                int  y,               // Vertical position
                                                int  cx,              // Width
                                                int  cy,              // Height
                                                uint uFlags); // Window positioning flags

        public static void ShowInactiveTopmost(Form frm)
        {
            ShowWindow(frm.Handle, SwShownoactivate);
            //SetWindowPos(frm.Handle.ToInt32(), HWND_TOPMOST, frm.Left, frm.Top, frm.Width, frm.Height, SWP_NOACTIVATE);
            SetWindowPos(frm.Handle.ToInt32(), HwndTopmost, 0, 0, 0, 0, SwpNoactivate | SwpNomove | SwpNosize | SwpNoreposition);
        }

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static Keys StringToKey(string key) { return (Keys)Enum.Parse(typeof(Keys), key); }

        public static bool UseLightThemeMode()
        {
            try
            {
                object o = Registry.GetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "AppsUseLightTheme", null);
                return o is null || o.ToString() != "0";
            }
            catch
            {
                return true;
            }
        }

        public static void WaitThenDo(int ms, Action doit)
        {
            Timer waitTimer = new Timer();
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
                doit();
                waitTimer.Dispose();
            };
        }
    }
}