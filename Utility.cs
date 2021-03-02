using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
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

        public static string BrowseForFile()
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Multiselect = false;
            fd.Filter = "All files (*.*)|*.*";
            fd.FilterIndex = 1;
            fd.CheckFileExists = true;
            fd.CheckPathExists = true;

            DialogResult dr = fd.ShowDialog();

            if (dr == DialogResult.OK)
                return fd.FileName;
            else
                return "";
        }

        public static string[] GetFiles(string path, string searchPattern)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            string[] files;

            if (searchPattern != "")
                files = Directory.GetFiles(path, searchPattern).OrderBy(f => new FileInfo(f).CreationTime).ToArray();
            else
                files = Directory.GetFiles(path).OrderBy(f => new FileInfo(f).CreationTime).ToArray();

            return files;
        }

        public static Image GetIcon(string fileName)
        {
            if (File.Exists(fileName))
            {
                string[] ImageTypes = { ".png", ".tif", ".jpg", ".gif", ".bmp", ".ico" };

                if (ImageTypes.Contains(Path.GetExtension(fileName)))
                {
                    return (Image)(Image)(new Bitmap(new Bitmap(fileName, false)));
                }

                else
                    return (Image)(new Bitmap(Icon.ExtractAssociatedIcon(fileName).ToBitmap()));
            }
            else
                return null;
        }
        public static FileVersionInfo GetFileInfo(string fileName)
        {
            return FileVersionInfo.GetVersionInfo(fileName);
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
            return Assembly.GetExecutingAssembly().GetName().Name;
        }

        public static string GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public static byte[] ImageToByteArray(Image image)
        {
            MemoryStream ms = new MemoryStream();
            image.Save(ms, ImageFormat.Png);
            return ms.ToArray();
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

        public static Boolean IsUrl(string s)
        {
            return (s.Length <= 2048) && s.ToLower().StartsWith("www") || s.ToLower().StartsWith("http") && Uri.IsWellFormedUriString(s, UriKind.RelativeOrAbsolute);
        }

        public static void MoveFormToCursor(Form form, bool IgnoreBounds = false)
        {
            Point p = new Point(Cursor.Position.X + 10, Cursor.Position.Y - 10);
            
            if (!IgnoreBounds)
            {
                //Height
                if ((p.Y + form.Size.Height) > Screen.PrimaryScreen.WorkingArea.Height)
                {
                    //p.Y = (p.Y - form.Size.Height);
                    p.Y = (p.Y - ((p.Y + form.Size.Height) - Screen.PrimaryScreen.WorkingArea.Height));
                }

                //Width
                if ((p.X + form.Size.Width) > Screen.PrimaryScreen.WorkingArea.Width)
                {
                    p.X = (p.X-((p.X + form.Size.Width)-Screen.PrimaryScreen.WorkingArea.Width));
                }
            }

            form.Location = p;
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

        public static string SaveToCache(string fileContents)
        {
            string randFileName = AppPath() + "\\Cache\\" + DateTime.Now.ToString("yyyymmddhhmmssfff")  + RandomString(10, true) + ".xml";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(fileContents);            
            doc.Save(randFileName);
            doc = null;
            return randFileName;
        }
    }
}