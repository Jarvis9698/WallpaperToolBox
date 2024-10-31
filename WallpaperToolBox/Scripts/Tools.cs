using System;
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
using System.Runtime.InteropServices;

namespace WallpaperToolBox
{
    /// <summary>
    /// 静态工具类
    /// </summary>
    internal static class Tools
    {
        /// <summary>
        /// 调用Windows API进行文件操作
        /// </summary>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;                // 处理此操作的窗口句柄，可以用 IntPtr.Zero 表示默认窗口。
            public uint wFunc;                 // 表示执行的操作类型，如删除、复制、移动、重命名等。
            public string pFrom;               // 要操作的文件或文件夹路径，以双空字符结尾，支持多路径。
            public string pTo;                 // 目标路径，仅在复制或移动时使用；删除操作设置为空。
            public short fFlags;               // 控制操作行为的标志位，支持多个选项。
            public bool fAnyOperationsAborted; // 表示操作是否被用户中止。
            public IntPtr hNameMappings;       // 重命名时使用的文件名映射指针，删除时可以忽略。
            public string lpszProgressTitle;   // 操作进度标题，仅在需要进度对话框时使用。
        }

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
        /// 读取文件（不判断是否存在）
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

        /// <summary>
        /// 删除目录及其所有文件
        /// </summary>
        public static void DeleteDirectory(string path)
        {
            SHFILEOPSTRUCT fileOp = new SHFILEOPSTRUCT
            {
                hwnd = IntPtr.Zero,
                wFunc = 0x3,// 删除文件
                pFrom = path + "\0\0",  // 双空字符结尾
                pTo = null,
                fFlags = 0x40 | 0x10,// 将文件移动到回收站且不显示对话框
                fAnyOperationsAborted = false,
                hNameMappings = IntPtr.Zero,
                lpszProgressTitle = "Deleting Files..."
            };

            int result = SHFileOperation(ref fileOp);
            if (result == 0)
            {
                Console.WriteLine("[Tools.DeleteDirectory]删除文件成功");
            }
            else
            {
                Console.WriteLine("[Tools.DeleteDirectory]删除文件失败，错误代码：" + result);
            }
        }

        /// <summary>
        /// 复制目录及其所有文件
        /// </summary>
        public static void CopyDirectory(string pathFrom, string pathTo)
        {
            SHFILEOPSTRUCT fileOp = new SHFILEOPSTRUCT
            {
                hwnd = IntPtr.Zero,
                wFunc = 0x2,// 复制文件
                pFrom = pathFrom + '\0',
                pTo = pathTo + '\0',
                fFlags = 0x200 | 0x10,// 自动创建目标文件夹且不显示对话框
                fAnyOperationsAborted = false,
                hNameMappings = IntPtr.Zero,
                lpszProgressTitle = "复制文件夹"
            };

            int result = SHFileOperation(ref fileOp);
            if (result == 0)
            {
                Console.WriteLine("[Tools.CopyDirectory]复制文件成功");
            }
            else
            {
                Console.WriteLine("[Tools.CopyDirectory]复制文件失败，错误代码：" + result);
            }
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
