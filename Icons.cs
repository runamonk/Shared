using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Icons
{
    class IconFuncs
    {
        public static string SHELL_APP_PREFIX = "shell:AppsFolder\\";

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public extern static bool DestroyIcon(IntPtr handle);
        [DllImport("Shell32.dll", EntryPoint = "ExtractIconExW", CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int ExtractIconEx(string sFile, int iIndex, out IntPtr piLargeVersion, out IntPtr piSmallVersion, int amountIcons);
        
        public static Image GetIcon(string fileName, string iconIndex)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            fileName = Environment.ExpandEnvironmentVariables(fileName);
            if (File.Exists(fileName) || (fileName.StartsWith(SHELL_APP_PREFIX)))
            {
                // don't include .ico files here, let windows ExtractAssociatedIcon, this will get the best resolution icon from the ico file.
                string[] ImageTypes = { ".png", ".tif", ".jpg", ".gif", ".bmp" }; 
                if (ImageTypes.Contains(Path.GetExtension(fileName)))
                {
                    return (Image)(new Bitmap(new Bitmap(fileName, false)));
                }
                else
                {
                    if (fileName.StartsWith(SHELL_APP_PREFIX))
                    {                       
                        try
                        {
                            ShellObject shellFile = ShellFile.FromParsingName(fileName);
                            shellFile.Thumbnail.AllowBiggerSize = true;
                            //Bitmap b = shellFile.Thumbnail.Bitmap;
                            Bitmap b = shellFile.Thumbnail.ExtraLargeBitmap;
                            // Shell Apps typically have a stupid border/background, make it transparent.
                            Color c = b.GetPixel(1, 1);
                            b.MakeTransparent(c);
                            return b;
                        } catch
                        {
                            Bitmap b = new Bitmap(32, 32);
                            return b;
                        }
                    }
                    else
                    if (string.IsNullOrEmpty(iconIndex) || (iconIndex == "0"))
                        return (Image)(new Bitmap(Icon.ExtractAssociatedIcon(fileName).ToBitmap()));
                    else
                    {
                        Icon i = GetIconEx(fileName, Convert.ToInt32(iconIndex));
                        return i?.ToBitmap();
                    }
                }
            }
            else
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
                else
                if (small != null)
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


    }
}
