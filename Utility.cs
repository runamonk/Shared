using System;
using System.Collections.Generic;
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
using System.Xml;

namespace Utility
{
    class Funcs
    {
        public static ToolStripMenuItem AddMenuItem(ToolStrip Menu, string Caption, EventHandler Event)
        {
            ToolStripMenuItem t = new ToolStripMenuItem(Caption);
            t.Click += new EventHandler(Event);
            Menu.Items.Add(t);
            return t;
        }
        public static string AppPath(string FileName)
        {
            return Path.GetDirectoryName(Application.ExecutablePath) + "\\" + FileName;
        }
        public static string AppPath()
        {
            return Path.GetDirectoryName(Application.ExecutablePath);
        }

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

            if (dr == DialogResult.OK)
                return fd.FileName;
            else
                return "";
        }
        public static string[] GetFiles(string path, string searchPattern)
        {
            string[] files;

            if (searchPattern != "")
                files = Directory.GetFiles(path, searchPattern).OrderBy(f => new FileInfo(f).CreationTime).ToArray();
            else
                files = Directory.GetFiles(path).OrderBy(f => new FileInfo(f).CreationTime).ToArray();

            return files;
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
        public static string GetNodePath(XmlNode xmlNode)
        {
            string pathName = xmlNode.Name;
            XmlNode node = xmlNode;
            while (true)
            {
                if (node.ParentNode.Name != "#document")
                {
                    pathName = $"{node.ParentNode.Name}/{pathName}";
                }
                else
                {
                    return pathName;

                }
                node = node.ParentNode;
            }
        }
        public static string GetName()
        {
            return Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title;
        }
        public static string GetNameAndVersion()
        {
            string s = ((Debugger.IsAttached) ? Funcs.GetName() + " - **DEBUG** - v" : Funcs.GetName() + " - v");           
            return s + GetVersion().Major.ToString() + "." + File.GetLastWriteTime(Funcs.GetFilePathAndName()).ToString("ddMMyyyy.HHmm");
        }
        public static Version GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }
        public static string GetWebsiteFavIcon(string url)
        { 
            string result = "";
            if ((url.ToLower().StartsWith("http://") || url.ToLower().StartsWith("https://")))
            {
                string baseDomain = new Uri(url).GetLeftPart(UriPartial.Authority);
                HttpWebRequest w = (HttpWebRequest)HttpWebRequest.Create(baseDomain + "/favicon.ico");
                w.AllowAutoRedirect = true;           
                try
                {
                    HttpWebResponse r = (HttpWebResponse)w.GetResponse();
                    Stream s = r.GetResponseStream();
                    Image ico = Image.FromStream(s);
                    result = Convert.ToBase64String(Funcs.ImageToByteArray(ico));
                }
                catch(WebException) 
                {
                }
            }
            return result;
        }
        public static byte[] ImageToByteArray(Image image)
        {
            MemoryStream ms = new MemoryStream();
            image.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
        public static Boolean IsSame(byte[] img1, byte[] img2)
        {
            if ((img1 == null) || (img2 == null))
                return false;

            return img1.SequenceEqual(img2);
        }
        public static Boolean IsSame(Image img1, byte[] img2)
        {
            if ((img1 == null) || (img2 == null))
                return false;

            byte[] b1;
            b1 = ImageToByteArray(img1);
           
            return b1.SequenceEqual(img2);
        }
        public static Boolean IsSame(Image img1, Image img2)
        {
            if ((img1 == null) || (img2 == null))
                return false;

            byte[] b1, b2;
            b1 = ImageToByteArray(img1);
            b2 = ImageToByteArray(img2);
            return b1.SequenceEqual(b2);
        }
        
        public static Boolean IsShortcut(string FileName)
        {
            string ext = Path.GetExtension(FileName).ToLower();
            if ((ext == ".lnk") || (ext == ".url"))
                return true;
            else
                return false;
        }
        public static Boolean IsUrl(string s)
        {
            return (s.ToLower().StartsWith("http://") || s.ToLower().StartsWith("https://") || s.ToLower().StartsWith("ftp://")) && Uri.IsWellFormedUriString(s, UriKind.RelativeOrAbsolute);
        }
        public static Boolean IsWindows7()
        {
            Version Ver = System.Environment.OSVersion.Version;
            return ((Ver.Major == 6) && (Ver.Minor <= 1));
        }
        public static void MoveFormToCursor(Form form, bool IgnoreBounds = false)
        {
            Point p = new Point(Cursor.Position.X, Cursor.Position.Y);
            
            if (!IgnoreBounds)
            {
                //Height
                if ((p.Y + form.Size.Height) > Screen.PrimaryScreen.WorkingArea.Height)
                {
                    //p.Y = (p.Y - form.Size.Height);
                    p.Y -= ((p.Y + form.Size.Height) - Screen.PrimaryScreen.WorkingArea.Height);
                } 
                else
                    p.Y += -50;

                //Width
                if ((p.X + form.Size.Width) > Screen.PrimaryScreen.WorkingArea.Width)
                {
                    p.X -= ((p.X + form.Size.Width)-Screen.PrimaryScreen.WorkingArea.Width);
                } 
                else
                    p.X += -35;

                if (p.Y < 0) 
                    p.Y = 0;

                if (p.X < 0)
                    p.X = 0;
            }
           
            form.Location = p;
        }
        public static void ParseShortcut(string FileName, out string ParsedFileName, out string ParsedFileIcon, out string ParsedFileIconIndex, out string ParsedArgs, out string ParsedWorkingFolder)
        {
            ParsedFileName = "";
            ParsedFileIcon = "";
            ParsedFileIconIndex = "";
            ParsedArgs = "";
            ParsedWorkingFolder = "";

            if (!IsShortcut(FileName))
                throw new Exception("File must be a .lnk or .url file.");
            if (!File.Exists(FileName))
                throw new Exception(FileName + " not found.");
                            
            if (Path.GetExtension(FileName).ToLower() == ".url")
            {
                string[] sFile = File.ReadAllLines(FileName);
                string IconFile = "";
                string URL = "";
                string WorkingFolder = "";

                string urlString = "URL=";
                string IconFileString = "IconFile=";
                string wfString = "WorkingDirectory=";

                string GetValue(string s, string lookup)
                {
                    return s.Substring(s.IndexOf(lookup) + lookup.Length, s.Length - lookup.Length);
                }

                foreach (string s in sFile)
                {
                    if (s.IndexOf(urlString) > -1)
                        URL = GetValue(s, urlString);
                    else
                    if (s.IndexOf(IconFileString) > -1)
                        IconFile = GetValue(s, IconFileString);
                    else
                    if (s.IndexOf(wfString) > -1)
                        WorkingFolder = GetValue(s, wfString);

                    if ((URL != "") && (IconFile != ""))
                        break;
                }

                if (URL != "")
                {
                    ParsedFileName = URL;
                    ParsedFileIcon = IconFile;
                    ParsedWorkingFolder = WorkingFolder;
                }
            }
            else
            {
                // Reference "Windows Script Host Object Model" 
                IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell(); //Create a new WshShell Interface
                IWshRuntimeLibrary.IWshShortcut link = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(FileName); //Link the interface to our shortcut
               
                string IconLoc = link.IconLocation.Substring(0, link.IconLocation.IndexOf(","));
                string IconIndex = link.IconLocation.Substring(link.IconLocation.IndexOf(",")+1, (link.IconLocation.Length-IconLoc.Length-1));
                ParsedFileName = link.TargetPath;

                // There are still some shortcuts (windows 11) that cannot be parsed.
                if (ParsedFileName == "")
                {
                    ParsedFileName = FileName;
                    ParsedFileIcon = "";
                    ParsedFileIconIndex = "";
                    ParsedWorkingFolder = "";
                }
                else
                {
                    // Double check for exe in Program Files if not found in Program Files (x86).
                    // this shouldn't happen with > Properties > Build > Prefer 32-bit unchecked; if it does we'll handle it automatically.

                    if ((!File.Exists(ParsedFileName)) && (ParsedFileName.Contains("Program Files (x86)")))
                    {
                        string s = ParsedFileName.Replace("Program Files (x86)", "Program Files");
                        if (File.Exists(s))
                            ParsedFileName = s;
                    }
                    ParsedFileIcon = IconLoc;
                    ParsedFileIconIndex = IconIndex;
                    ParsedArgs = link.Arguments;
                    ParsedWorkingFolder = link.WorkingDirectory;
                }
            }
        }
        public static int RandomNumber(int size = 99999999)
        {
            Random rand = new Random(size);
            return rand.Next();
        }
        public static string RandomString(int size, bool lowerCase)
        {
            const string src = "abcdefghijklmnopqrstuvwxyz0123456789";
            var sb = new StringBuilder();
            Random RNG = new Random();
            for (var i = 0; i < size; i++)
            {
                var c = src[RNG.Next(0, src.Length)];
                sb.Append(c);
            }
            if (lowerCase)
                return sb.ToString().ToLower();
            else
                return sb.ToString();
        }
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);

        #region ShowInactiveTopmost
        //http://www.pinvoke.net/default.aspx/user32/ShowWindow.html
        private const int SW_SHOWNOACTIVATE = 4;
        private const int HWND_TOPMOST = -1;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOREPOSITION = 0x0200;


        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        private static extern bool SetWindowPos(
             int hWnd,             // Window handle
             int hWndInsertAfter,  // Placement-order handle
             int X,                // Horizontal position
             int Y,                // Vertical position
             int cx,               // Width
             int cy,               // Height
             uint uFlags);         // Window positioning flags

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static void ShowInactiveTopmost(Form frm)
        {
            ShowWindow(frm.Handle, SW_SHOWNOACTIVATE);
            //SetWindowPos(frm.Handle.ToInt32(), HWND_TOPMOST, frm.Left, frm.Top, frm.Width, frm.Height, SWP_NOACTIVATE);
            SetWindowPos(frm.Handle.ToInt32(), HWND_TOPMOST, 0, 0, 0, 0, SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOSIZE | SWP_NOREPOSITION);
        }
        #endregion

        public static Image ScaleImage(Image image, int maxWidth, int maxHeight)
        {
            if ((image.Height > maxHeight) || (image.Width > maxWidth))
            {
                var ratioX = (double)maxWidth / image.Width;
                var ratioY = (double)maxHeight / image.Height;
                var ratio = Math.Min(ratioX, ratioY);
                var newWidth = image.Width;
                var newHeight = image.Height;
                int i = 0;

                i = (int)(image.Width * ratio);               
                if (i > 0) 
                    newWidth = i;

                i = (int)(image.Height * ratio);
                if (i > 0)
                    newHeight = i;

                var newImage = new Bitmap(newWidth, newHeight);
                using (var graphics = Graphics.FromImage(newImage))
                    graphics.DrawImage(image, 0, 0, newWidth, newHeight);

                return newImage;
            }
            else
                return image;
        }
    }
}