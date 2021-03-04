﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
//using IWshRuntimeLibrary;


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
            fd.DereferenceLinks = false;

            DialogResult dr = fd.ShowDialog();

            if (dr == DialogResult.OK)
                return fd.FileName;
            else
                return "";
        }
        public static void BrowseForFolder(string Text)
        {
            FolderBrowserDialog f = new FolderBrowserDialog();
            if ((f.ShowDialog() == DialogResult.OK) && (Directory.Exists(f.SelectedPath)))
                Text = f.SelectedPath;
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
            return (s.Length <= 2048) && s.ToLower().StartsWith("www") || s.ToLower().StartsWith("http") && Uri.IsWellFormedUriString(s, UriKind.RelativeOrAbsolute);
        }

        public static Boolean IsWindows7()
        {
            Version Ver = System.Environment.OSVersion.Version;
            return ((Ver.Major == 6) && (Ver.Minor <= 1));
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

        public static void ParseShortcut(string FileName, out string ParsedFileName, out string ParsedFileIcon, out string ParsedArgs, out string ParsedWorkingFolder)
        {
            ParsedFileName = "";
            ParsedFileIcon = "";
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

                string urlString = "URL=";
                string IconFileString = "IconFile=";

                foreach (string s in sFile)
                {
                    if (s.IndexOf(urlString) > -1)
                        URL = s.Substring(s.IndexOf(urlString) + urlString.Length, s.Length - urlString.Length);
                    else
                    if (s.IndexOf(IconFileString) > -1)
                        IconFile = s.Substring(s.IndexOf(IconFileString) + IconFileString.Length, s.Length - IconFileString.Length);

                    if ((URL != "") && (IconFile != ""))
                        break;
                }

                if (URL != "")
                {
                    ParsedFileName = URL;
                    ParsedFileIcon = IconFile;
                }
            }
            else
            {
                IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell(); //Create a new WshShell Interface
                IWshRuntimeLibrary.IWshShortcut link = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(FileName); //Link the interface to our shortcut
                //string IconIndex = link.IconLocation.Substring(link.IconLocation.IndexOf(",")+1, link.IconLocation.Length-link.IconLocation.IndexOf(",")-1);
                string IconLoc = link.IconLocation.Substring(0, link.IconLocation.IndexOf(","));
                ParsedFileName = link.TargetPath;
                ParsedFileIcon = IconLoc;
                ParsedArgs = link.Arguments;
                ParsedWorkingFolder = link.WorkingDirectory;
            }
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