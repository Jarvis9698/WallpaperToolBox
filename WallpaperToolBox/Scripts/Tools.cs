﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
using System.Drawing;
using System.Windows.Forms;
using Sunny.UI;
using System.Drawing.Imaging;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Reflection;
using Scripting;

using File = System.IO.File;
using Sunny.UI.Win32;

namespace WallpaperToolBox
{
    /// <summary>
    /// 静态工具类
    /// </summary>
    internal static class Tools
    {
        #region Json处理
        public static string SettingToJson(object obj)
        {
            string result = "";
            DataContractJsonSerializer json = new DataContractJsonSerializer(obj.GetType());
            using (MemoryStream stream = new MemoryStream())
            {
                json.WriteObject(stream, obj);
                result = Encoding.UTF8.GetString(stream.ToArray());
            }
            return result;
        }
        public static Setting JsonToSetting(string json)
        {
            Setting result = JsonConvert.DeserializeObject<Setting>(json);
            return result;
        }
        public static Wallpaper JsonToWallpaper(string json)
        {
            Wallpaper result = JsonConvert.DeserializeObject<Wallpaper>(json);
            return result;
        }
        #endregion Json处理

        /// <summary>
        /// 读取Wallpaper配置数据并返回Wallpaper类对象
        /// </summary>
        public static Wallpaper LoadWallpaperValue(DirectoryInfo dirInfo)
        {
            return LoadWallpaperValue(dirInfo.FullName + "\\");
        }
        /// <summary>
        /// 读取Wallpaper配置数据并返回Wallpaper类对象
        /// </summary>
        public static Wallpaper LoadWallpaperValue(string dirPath)
        {
            Wallpaper result = null;
            string jsonPath = dirPath + "project.json";

            if (File.Exists(jsonPath))
            {
                string[] str = dirPath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                string id = str[str.Length - 1];
                id = id.Replace(SettingManager.UnpackDirectorySuffix, "");

                string json = ReadFile(jsonPath, false);
                result = JsonToWallpaper(json);
                result.directoryPath = dirPath;
                result.id = id;
                result.previewImage = LoadImage(dirPath + result.preview, SettingManager.PreviewImageSize);

                FileSystemObject file = new FileSystemObject();
                result.dirSize = (float)file.GetFolder(dirPath).Size;

                FileInfo fileInfo = new FileInfo(jsonPath);
                result.lastWriteTime = fileInfo.LastWriteTime;
            }
            return result;
        }

        #region I/O
        /// <summary>
        /// 读取文件（没有判断是否存在）
        /// </summary>
        public static List<string> ReadFile(string path)
        {
            List<string> result = new List<string>(File.ReadAllLines(path));
            return result;
        }
        public static string ReadFile(string path, bool isList = false)
        {
            string result = File.ReadAllText(path);
            return result;
        }

        /// <summary>
        /// 读取图片(gif图仅返回中间某帧)
        /// </summary>
        public static Bitmap LoadImage(string path, int size)
        {
            return LoadImage(path, new Size(size, size));
        }

        public static Bitmap LoadImage(string path, Size size)
        {
            if (!File.Exists(path))
            {
                path = SettingManager.currentDirectoryPath + "Images\\image_error.png";
            }
            if (!File.Exists(path))
            {
                return null;
            }

            FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            Image image = Image.FromStream(fileStream);
            if (Path.GetExtension(path) == ".gif")
            {
                FrameDimension fd = new FrameDimension(image.FrameDimensionsList[0]);
                int count = image.GetFrameCount(fd);
                image.SelectActiveFrame(fd, count / 4);
            }
            fileStream.Close();

            return new Bitmap(image, size);
        }

        /// <summary>
        /// 写入文件
        /// </summary>
        public static void WriteFile(string path, List<string> content)
        {
            File.WriteAllLines(path, content, Encoding.UTF8);
        }
        public static void WriteFile(string path, string content)
        {
            File.WriteAllText(path, content, Encoding.UTF8);
        }

        /// <summary>
        /// 获取文件所在的目录路径
        /// </summary>
        public static string GetDirectory(string path)
        {
            string result = path + "\\";
            if (!Directory.Exists(path))
            {
                result = Path.GetDirectoryName(path) + "\\";
            }
            return result;
        }
        #endregion I/O

        /// <summary>
        /// 获取限制字节长度的字符串
        /// <para>长度过长会在后面加上"..."</para>
        /// </summary>
        public static string GetStringWithByteLength(string str, int length)
        {
            int strLength = Encoding.Default.GetByteCount(str);
            if (strLength > length)
            {
                StringBuilder strBuilder = new StringBuilder();
                for (int i = 0; i < length; i++)
                {
                    strBuilder.Append(str[i]);
                    if (Encoding.Default.GetByteCount(strBuilder.ToString()) >= length - 3)
                    {
                        break;
                    }
                }
                strBuilder.Append("...");
                str = strBuilder.ToString();
            }

            return str;
        }

        /// <summary>
        /// 打开询问窗口
        /// </summary>
        public static bool ShowAskDialog(string msg, UIForm uiForm)
        {
            return uiForm.ShowAskDialog(msg);
        }

        /// <summary>
        /// 打开警告询问窗口
        /// </summary>
        public static bool ShowAskWarningAskDialog(string msg, UIForm uiForm)
        {
            return uiForm.ShowAskDialog(UILocalize.WarningTitle, msg, UIStyle.Orange);
        }

        /// <summary>
        /// 运行一个cmd程序
        /// </summary>
        public static string RunCMD(string cmd)
        {
            string result;

            Process cmdPrecess = new Process();
            cmdPrecess.StartInfo.FileName = "cmd.exe";
            cmdPrecess.StartInfo.UseShellExecute = false;
            cmdPrecess.StartInfo.RedirectStandardInput = true;
            cmdPrecess.StartInfo.RedirectStandardOutput = true;
            cmdPrecess.StartInfo.RedirectStandardError = true;
            cmdPrecess.StartInfo.CreateNoWindow = true;
            cmdPrecess.Start();

            cmdPrecess.StandardInput.WriteLine(cmd);
            cmdPrecess.StandardInput.WriteLine("exit");
            cmdPrecess.StandardInput.AutoFlush = true;
            result = cmdPrecess.StandardOutput.ReadToEnd();

            cmdPrecess.WaitForExit();
            cmdPrecess.Close();

            return result;
        }
    }
}
