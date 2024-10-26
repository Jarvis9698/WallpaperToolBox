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
using System.Reflection;

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
        public static string Version { get;  private set; }

        public const string TitleText = "Wallpaper工具箱 - v";
        public const string UnpackDirectorySuffix = "_unpack";
        public const string WallpaperEngineProcessName = "wallpaper32";

        public const int PreviewRowsHeight = 100;
        public const int PreviewImageSize = 96;
        public const int PreviewInformationPictureSize = 150;

        #region 常量_提示

        public const string PreviewDeleteEmptyTip = "请勾选需要删除的壁纸";
        public const string PreviewUnpackSelectEmptyTip = "请勾选需要解包的壁纸";
        public const string PreviewUnpackPathEmptyTip = "请设置壁纸解包后的存放目录！";
        public const string PreviewToBackupSelectEmptyTip = "请勾选需要添加到官方备份目录的壁纸";
        public const string PreviewToBackupPathEmptyTip = "请设置官方备份的目录！";
        public const string PreviewLocalBackupPathEmptyTip = "请设置本地备份目录！";
        public const string PreviewContentratingEmptyTip = "无年龄分级";
        public const string PreviewTypeEmptyTip = "无壁纸类型";
        public const string PreviewContentTip = "左键查看\n左键双击打开目录\n右键勾选\nshift+右键范围勾选\nctrl+右键范围取消";
        public const string PreviewContentTip_2 = "左键单击选择\n双击打开目录\n单击列表第一列可勾选/取消勾选";
        public const string FileNullErrorTip = "文件不存在！";

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

        #region 常量_提示_本地备份管理页面_差异变更管理页面
        public const string DataGridViewEmptyTip = "未发现有变更的壁纸\n可以按下重新读取并刷新列表\n或检查下过滤选项\n和设置的路径";
        public const string SyncSelectEmptyTip = "未勾选需要同步的壁纸";
        public const string RollbackSelectEmptyTip = "未勾选需要撤销更改的壁纸";
        #endregion 常量_提示_本地备份管理页面_差异变更管理页面

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
        public SettingManager()
        {
        }

        public static void Init()
        {
            currentDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;
            m_SettingFilePath = currentDirectoryPath + "Setting.json";
            m_UserPCName = SystemInformation.ComputerName;

            // 获取版本号
            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

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
    /// 壁纸展示按钮管理类，用于预览列表
    /// <para>===该类暂时无用，留作备用===</para>
    /// </summary>
    public class WallpaperHeaderButton
    {
        protected UIFlowLayoutPanel m_panel;
        protected UIHeaderButton m_Btn;

        /// <summary>
        /// 壁纸ID
        /// </summary>
        public string wallpaperID { get; protected set; }
        /// <summary>
        /// 若已勾选
        /// </summary>
        public bool isSelected { get; protected set; } = false;

        public WallpaperHeaderButton(UIFlowLayoutPanel panel,
            Action<WallpaperHeaderButton, EventArgs> onClick,
            Action<WallpaperHeaderButton, EventArgs> onDoubleClick)
        {
            m_panel = panel;
            m_Btn = new UIHeaderButton();
            int size = SettingManager.PreviewRowsHeight;
            int top = (size - SettingManager.PreviewImageSize) / 2;

            m_Btn.Name = "error";
            m_Btn.Size = new Size(size, size + 20);
            m_Btn.ImageTop = top;
            m_Btn.Font = new Font("微软雅黑", 8f);
            m_Btn.ForeHoverColor = Color.FromArgb(90, 90, 90);
            m_Btn.ForePressColor = Color.FromArgb(64, 64, 64);
            m_Btn.ForeSelectedColor = Color.FromArgb(64, 64, 64);

            m_Btn.Style = UIStyle.Custom;
            m_Btn.StyleCustomMode = true;
            m_Btn.FillColor = Color.FromArgb(115, 115, 115);
            m_Btn.FillDisableColor = Color.FromArgb(115, 115, 115);
            m_Btn.FillHoverColor = Color.FromArgb(215, 215, 215);
            m_Btn.FillPressColor = Color.FromArgb(250, 250, 250);
            m_Btn.FillSelectedColor = Color.FromArgb(250, 250, 250);

            // 勾选图标设置
            m_Btn.ShowTips = true;
            // 勾选图标底色
            m_Btn.TipsColor = Color.FromArgb(255, 185, 0);
            // 勾选图标文本颜色
            m_Btn.TipsForeColor = Color.Black;

            // 按钮事件注册
            m_Btn.Click += (object sender, EventArgs e) =>
            {
                onClick(this, e);
            };
            m_Btn.UseDoubleClick = true;
            m_Btn.DoubleClick += (object sender, EventArgs e) =>
            {
                onDoubleClick(this, e);
            };

            m_panel.Add(m_Btn);
        }

        public void Destory()
        {
            isSelected = false;
            m_panel.Remove(m_Btn);
        }

        public void Update(Wallpaper wallpaper)
        {
            m_Btn.Image = wallpaper.previewImage;
            m_Btn.Name = wallpaper.title;
            m_Btn.Text = Tools.GetStringWithByteLength(wallpaper.title, 16);
            wallpaperID = wallpaper.id;
            m_Btn.Selected = false;
        }

        /// <summary>
        /// 勾选或取消勾选
        /// </summary>
        public void ChangeSelected()
        {
            isSelected = !isSelected;
            OnSelectedChanged();
        }

        /// <summary>
        /// 设置勾选状态
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            OnSelectedChanged();
        }

        protected void OnSelectedChanged()
        {
            if (isSelected)
            {
                m_Btn.TipsText = "✔";
            }
            else
            {
                m_Btn.TipsText = string.Empty;
            }
        }
    }
}
