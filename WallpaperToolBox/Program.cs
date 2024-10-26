using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WallpaperToolBox;

namespace WallpaperToolBox
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SettingManager.Init();
            WallpaperManager.Init();
            // 打开窗口前确保必要管理器已启动
            Application.Run(new MainWindow());
        }
    }
}
