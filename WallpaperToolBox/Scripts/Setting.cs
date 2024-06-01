using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Forms;
using Sunny.UI;
using System.Threading;
using System.ComponentModel;
using System.Drawing;

namespace WallpaperToolBox
{
    [DataContract, Serializable]
    internal class Setting
    {
        /// <summary>
        /// 壁纸存放目录
        /// </summary>
        [DataMember]
        public string storePath { get; set; }
        /// <summary>
        /// 本地壁纸备份目录
        /// </summary>
        [DataMember]
        public string localBackupPath { get; set; }
        /// <summary>
        /// 官方壁纸备份目录
        /// </summary>
        [DataMember]
        public string backupPath { get; set; }
        /// <summary>
        /// 壁纸导出目录
        /// </summary>
        [DataMember]
        public string unpackPath { get; set; }
        /// <summary>
        /// 用户电脑名称
        /// </summary>
        [DataMember]
        public string userPCName { get; set; }
    }

    /// <summary>
    /// 设置管理类
    /// </summary>
    internal class SettingManager
    {
        #region 常量
        public const string Version = "0.1.5";

        public const string TitleText = "Wallpaper工具箱 - v";
        public const string UnpackDirectorySuffix = "_unpack";
        public const string WallpaperEngineProcessName = "wallpaper32";

        public const int PreviewRowsHeight = 100;
        public const int PreviewImageSize = 96;
        public const int PreviewContentPictureSize = 150;

        #region 常量_提示

        public const string PreviewDeleteEmptyTip = "请勾选需要删除的壁纸";
        public const string PreviewUnpackSelectEmptyTip = "请勾选需要解包的壁纸";
        public const string PreviewUnpackPathEmptyTip = "请设置壁纸解包后的存放目录！";
        public const string PreviewToBackupSelectEmptyTip = "请勾选需要添加到官方备份目录的壁纸";
        public const string PreviewToBackupPathEmptyTip = "请设置官方备份的目录！";
        public const string PreviewContentratingEmptyTip = "无年龄分级";
        public const string PreviewTypeEmptyTip = "无壁纸类型";
        public const string PreviewContentTip = "左键查看\n左键双击打开目录\n右键勾选\nshift+右键范围勾选\nctrl+右键范围取消";

        #region 常量_提示_设置页面
        public const string SettingStoreSelectDirViewTip = "请选择创意工坊壁纸的存放目录";
        public const string SettingStoreEmptyPathTip = "使用右侧按钮打开创意工坊壁纸的存放目录，或直接将目录拖入此处。";
        public const string SettingStoreWaitLoadTip = "请等待创意工坊壁纸文件读取完成";

        public const string SettingBackupSelectDirViewTip = "请选择Wallpaper Engine的备份目录";
        public const string SettingBackupEmptyPathTip = "使用右侧按钮打开Wallpaper Engine的备份目录，或直接将目录拖入此处。";
        public const string SettingBackupWaitLoadTip = "请等待Wallpaper Engine的备份文件读取完成";

        public const string SettingLocalBackupSelectDirViewTip = "请选择本地的壁纸备份目录";
        public const string SettingLocalBackupEmptyPathTip = "使用右侧按钮打开本地的壁纸备份目录，或直接将目录拖入此处。";
        public const string SettingLocalBackupWaitLoadTip = "请等待本地备份的壁纸文件读取完成";

        public const string SettingUnpackSelectDirViewTip = "请选择壁纸解包后的存放目录";
        public const string SettingUnpackEmptyPathTip = "使用右侧按钮打开壁纸解包后的存放目录，或直接将目录拖入此处。";
        public const string SettingUnpackWaitLoadTip = "请等待解包后的壁纸文件读取完成";
        #endregion 常量_提示_设置页面

        #region 常量_提示_解包页面
        public const string StoreViewEmptyTip = "一张壁纸都没找到\n可以按下刷新试试\n或检查下过滤选项\n和设置的路径";
        public const string StoreViewSelectEmptyTip = "请勾选需要解包的壁纸";

        public const string UnpackViewEmptyTip = "一张已解包壁纸都没找到\n可以按下刷新试试\n或检查下过滤选项\n和设置的路径";

        #endregion 常量_提示_解包页面

        #endregion 常量_提示

        #endregion 常量

        /// <summary>
        /// 程序设置
        /// </summary>
        public static Setting setting = new Setting();
        /// <summary>
        /// 当前程序所在目录路径
        /// </summary>
        public static string currentDirectoryPath = "";

        private static string m_SettingFilePath = "";
        private static string m_UserPCName;

        #region 接口
        public static void Init()
        {
            currentDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;
            m_SettingFilePath = currentDirectoryPath + "Setting.json";
            m_UserPCName = SystemInformation.ComputerName;

            if (!File.Exists(m_SettingFilePath))
            {
                InitSetting();
            }
            else
            {
                LoadSetting();

                // 更换环境或用户，重置设置
                if (setting.userPCName != m_UserPCName)
                {
                    InitSetting();
                }
            }
        }

        public static void SaveSetting()
        {
            string json = Tools.SettingToJson(setting);
            Tools.WriteFile(m_SettingFilePath, json);

            AfterSaveSetting();
        }

        public static void LoadSetting()
        {
            string json = Tools.ReadFile(m_SettingFilePath, false);
            setting = Tools.JsonToSetting(json);
        }

        #endregion 接口

        private static void InitSetting()
        {
            setting = new Setting();
            setting.userPCName = SystemInformation.ComputerName;
            SaveSetting();
        }

        private static void AfterSaveSetting()
        {
            WallpaperManager.Init();
        }
    }

    /// <summary>
    /// 壁纸预览列表
    /// </summary>
    public class WallpaperDataGridView : WallpaperFlowPanelBase
    {
        UIDataGridView m_AddGridView;
        UIDataGridViewFooter m_AddGridViewFooter;
        List<Wallpaper> m_AddList = new List<Wallpaper>();
        List<int> m_AddSelectedIndexList = new List<int>();

        UIDataGridView m_ChangedGridView;
        UIDataGridViewFooter m_ChangedGridViewFooter;
        List<Wallpaper> m_ChangedList = new List<Wallpaper>();
        List<int> m_ChangedSelectedIndexList = new List<int>();

        UIDataGridView m_DelGridView;
        UIDataGridViewFooter m_DelGridViewFooter;
        List<Wallpaper> m_DelList = new List<Wallpaper>();
        List <int> m_DelSelectedIndexList = new List<int>();

        #region 接口
        public override void Init(
            UIForm uIForm,
            UIProcessBar processBar,
            WallpaperLoader wallpaperLoader,
            UISwitch everySwitch,
            UISwitch questionableSwitch,
            UISwitch matureSwitch)
        {
            base.Init(uIForm, processBar, wallpaperLoader, everySwitch, questionableSwitch, matureSwitch);
            isInited = false;
        }

        /// <summary>
        /// 初始化托管的滑动列表
        /// </summary>
        public void InitClass(UIDataGridView addGridView, UIDataGridViewFooter addFooter,
            UIDataGridView changedGridView, UIDataGridViewFooter changedFooter,
            UIDataGridView delGridView, UIDataGridViewFooter delFooter)
        {
            m_AddGridView = addGridView;
            m_AddGridViewFooter = addFooter;

            m_ChangedGridView = changedGridView;
            m_ChangedGridViewFooter = changedFooter;

            m_DelGridView = delGridView;
            m_DelGridViewFooter = delFooter;

            InitDataGridView();
            isInited = true;
        }
        #endregion 接口

        private void InitDataGridView()
        {
            m_AddGridView.Rows.Clear();
            m_AddGridView.Rows[0].Height = SettingManager.PreviewRowsHeight;
            m_AddGridView.Rows[0].Cells[1].Value = "114514";
            m_AddGridView.Rows[0].Cells[2].Value = SettingManager.StoreViewEmptyTip;
            Image image = Image.FromFile(SettingManager.currentDirectoryPath + "Images\\image_1.jpg");
            Bitmap bitmap = new Bitmap(image, SettingManager.PreviewImageSize, SettingManager.PreviewImageSize);
            m_AddGridView.Rows[0].Cells[5].Value = bitmap;

            m_AddGridViewFooter.Clear();
            m_ProgressBar.Hide();
            panelUpdateState = VieweUpdateState.Updated;
        }

        #region 后台
        protected override void LoadProgressWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // 前面95%进度用于读取壁纸数据
            while (m_WallpaperLoader.isLoading)
            {
                m_LoadProgressWorker.ReportProgress((int)(m_WallpaperLoader.loadingProgress * 0.95f));
                Thread.Sleep(10);
            }

            m_AddList = ((WallpaperChanger)m_WallpaperLoader).GetAddViewList(
                m_EverySwitch.Active,
                m_QuestionableSwitch.Active,
                m_MatureSwitch.Active);

            // 后面5%进度用于刷新列表
            int progress = 0;
            while (panelUpdateState != VieweUpdateState.Updated)
            {
                m_LoadProgressWorker.ReportProgress(progress);

                progress += 100;
                Thread.Sleep(50);
            }
        }

        // 加载后台同步进度
        protected override void LoadProgressWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int progress = e.ProgressPercentage;


        }
        #endregion 后台

        #region UI事件
        #endregion UI事件
    }
}
