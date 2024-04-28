using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.Shell;

namespace Icons
{
    internal class IconFuncs
    {
        public static string ShellAppPrefix = "shell:AppsFolder\\";

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool DestroyIcon(IntPtr handle);

        [DllImport("Shell32.dll", EntryPoint = "ExtractIconExW", CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int ExtractIconEx(string sFile, int iIndex, out IntPtr piLargeVersion, out IntPtr piSmallVersion, int amountIcons);

        public static Image GetIcon(string fileName, string iconIndex)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            fileName = Environment.ExpandEnvironmentVariables(fileName);
            if (File.Exists(fileName) || IsShellApp(fileName))
            {
                // don't include .ico files here, let windows ExtractAssociatedIcon, this will get the best resolution icon from the ico file.
                string[] imageTypes = { ".png", ".tif", ".jpg", ".gif", ".bmp" };
                if (imageTypes.Contains(Path.GetExtension(fileName))) return new Bitmap(new Bitmap(fileName, false));

                if (IsShellApp(fileName))
                    try
                    {
                        ShellObject shellFile = ShellObject.FromParsingName(fileName);
                        shellFile.Thumbnail.AllowBiggerSize = true;
                        //Bitmap b = shellFile.Thumbnail.Bitmap;
                        Bitmap b = shellFile.Thumbnail.ExtraLargeBitmap;
                        // Shell Apps typically have a stupid border/background, make it transparent.
                        Color c = b.GetPixel(1, 1);
                        b.MakeTransparent(c);
                        return b;
                    }
                    catch
                    {
                        Bitmap b = new Bitmap(32, 32);
                        return b;
                    }

                if (string.IsNullOrEmpty(iconIndex) || iconIndex == "0") return new Bitmap(Icon.ExtractAssociatedIcon(fileName).ToBitmap());

                Icon i = GetIconEx(fileName, Convert.ToInt32(iconIndex));
                return i?.ToBitmap();
            }

            return null;
        }

        public static Icon GetIconEx(string fileName, int index)
        {
            try
            {
                ExtractIconEx(fileName, index, out IntPtr large, out IntPtr small, 1);
                Icon iconToReturn = null;

                if (large != null)
                    iconToReturn = (Icon)Icon.FromHandle(large).Clone();
                else if (small != null)
                    iconToReturn = (Icon)Icon.FromHandle(small).Clone();

                if (iconToReturn != null)
                {
                    if (large != null) DestroyIcon(large);
                    if (small != null) DestroyIcon(small);
                    return iconToReturn;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public static bool IsShellApp(string toCompare) { return toCompare != null && (toCompare.ToLower().Contains("shell:") || toCompare.ToLower().Contains(ShellAppPrefix)); }
    }
}