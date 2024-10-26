using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Sunny.UI;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.IO;
using Sunny.UI.Win32;

namespace WallpaperToolBox
{
    public partial class MainWindow : UIForm
    {
        public MainWindow()
        {
            this.Size = new Size(1200, 700);
            InitializeComponent();
            InitWindow();
        }

        void InitWindow()
        {
            this.Text = SettingManager.TitleText + SettingManager.Version;

            InitSettingPage();
            InitStorePage();
            InitLocalBackupPage();
            InitBackupPage();
            InitUnpackPage();
        }

        // tab页面切换时
        private void uiTabControlMenu1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = ((UITabControlMenu)sender).SelectedIndex;
            switch (selectedIndex)
            {
                // 主页
                case 0:
                    UpdateUIUserControl1Size();
                    break;
                // 设置页面
                case 1:
                    UpdateSettingPage();
                    break;
                // 订阅壁纸管理页面
                case 2:
                    UpdateStorePage();
                    break;
                // 本地备份管理页面
                case 3:
                    UpdateLocalBackupPage();
                    break;
                // 官方备份壁纸管理页面
                case 4:
                    UpdateBackupPage();
                    break;
                // 解包壁纸管理页面
                case 5:
                    UpdateUnpackPage();
                    break;
                default: 
                    break;
            }
        }

        #region 设置页面
        /// <summary>
        /// 创意工坊壁纸路径设置框
        /// </summary>
        SettingPathPanel m_SettingStorePathPanel = new SettingPathPanel();
        /// <summary>
        /// 本地备份壁纸路径设置框
        /// </summary>
        SettingPathPanel m_SettintLocalBackupPathPanel = new SettingPathPanel();
        /// <summary>
        /// 官方备份壁纸路径设置框
        /// </summary>
        SettingPathPanel m_SettingBackupPathPanel = new SettingPathPanel();
        /// <summary>
        /// 解包壁纸路径设置框
        /// </summary>
        SettingPathPanel m_SettingUnpackPathPanel = new SettingPathPanel();
        void InitSettingPage()
        {
            m_SettingStorePathPanel.Init(
                this,
                storePathTextBox1,
                m_StorePanel,
                WallpaperManager.storeLoader,
                path => { SettingManager.setting.storePath = path; },
                () => SettingManager.setting.storePath,
                SettingManager.SettingStoreSelectDirViewTip,
                SettingManager.SettingStoreEmptyPathTip,
                SettingManager.SettingStoreWaitLoadTip);

            m_SettintLocalBackupPathPanel.Init(
                this,
                localBackupPathTextBox1,
                m_LocalBackupPanel,
                WallpaperManager.localBackupLoader,
                path => { SettingManager.setting.localBackupPath = path; },
                () => SettingManager.setting.localBackupPath,
                SettingManager.SettingLocalBackupSelectDirViewTip,
                SettingManager.SettingLocalBackupEmptyPathTip,
                SettingManager.SettingLocalBackupWaitLoadTip);

            m_SettingBackupPathPanel.Init(
                this,
                backupPathTextBox1,
                m_BackupPanel,
                WallpaperManager.backupLoader,
                path => { SettingManager.setting.backupPath = path; },
                () => SettingManager.setting.backupPath,
                SettingManager.SettingBackupSelectDirViewTip,
                SettingManager.SettingBackupEmptyPathTip,
                SettingManager.SettingBackupWaitLoadTip);

            m_SettingUnpackPathPanel.Init(
                this,
                unpackPathTextBox1,
                m_unpackPanel,
                WallpaperManager.unpackLoader,
                path => { SettingManager.setting.unpackPath = path; },
                () => SettingManager.setting.unpackPath,
                SettingManager.SettingUnpackSelectDirViewTip,
                SettingManager.SettingUnpackEmptyPathTip,
                SettingManager.SettingUnpackWaitLoadTip);
        }

        // 选择壁纸目录按钮
        private void StorePathButton_Click(object sender, EventArgs e)
        {
            m_SettingStorePathPanel.OpenSelectPathView();
        }

        // 选择本地备份壁纸目录按钮
        private void localBackupPathSelectBtn_Click(object sender, EventArgs e)
        {
            m_SettintLocalBackupPathPanel.OpenSelectPathView();
        }

        // 选择官方备份壁纸目录按钮
        private void backupPathSelectBtn_Click(object sender, EventArgs e)
        {
            m_SettingBackupPathPanel.OpenSelectPathView();
        }

        // 自动识别官方壁纸目录
        private void backupPathFindBtn_Click(object sender, EventArgs e)
        {
            if (m_SettingBackupPathPanel.wallpaperLoader.isLoading)
            {
                this.ShowWarningTip(SettingManager.SettingBackupWaitLoadTip);
                return;
            }

            string processName = SettingManager.WallpaperEngineProcessName;
            string path;
            Process[] processes = Process.GetProcessesByName(processName);

            if (processes == null || processes.Length == 0)
            {
                this.ShowWarningDialog("自动识别失败！\n未检测到Wallpaper Engine进程");
                return;
            }
            path = Tools.GetDirectory(processes[0].MainModule.FileName);
            path += "projects\\backup\\";
            // 创建目录
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            m_SettingBackupPathPanel.SetPath(path);

            this.ShowSuccessTip("自动识别成功");
        }

        // 选择解包壁纸目录按钮
        private void unpackPathSelectBtn_Click(object sender, EventArgs e)
        {
            m_SettingUnpackPathPanel.OpenSelectPathView();
        }

        private void UpdateSettingPage()
        {
            m_SettingStorePathPanel.Update();
            m_SettintLocalBackupPathPanel.Update();
            m_SettingBackupPathPanel.Update();
            m_SettingUnpackPathPanel.Update();
        }

        #endregion 设置页面

        #region 订阅壁纸管理页面
        /// <summary>
        /// 用于预览的创意工坊壁纸列表
        /// </summary>
        List<Wallpaper> storeViewList = new List<Wallpaper>();
        /// <summary>
        /// 壁纸预览列表中已勾选的下标列表，对应storeViewList的下标
        /// </summary>
        List<int> storeViewSelectedIndexList = new List<int>();
        /// <summary>
        /// 创意工坊壁纸预览滑动框
        /// </summary>
        WallpaperFlowLayoutPanel m_StorePanel = new WallpaperFlowLayoutPanel();

        void InitStorePage()
        {
            m_StorePanel.Init(
                this,
                storePageProgressBar1,
                WallpaperManager.storeLoader,
                storeEveryoneSwitch1, 
                storeQuestionableSwitch1, 
                storeMatureSwitch1);
            m_StorePanel.InitPanel(storeFlowLayoutPanel1);
            m_StorePanel.InitPreviewGroupBox(
                new InformationGroupBox(
                    storePreviewPicture1,
                    storePreviewNameLabel1,
                    storePreviewContentratingLabel1,
                    storePreviewTypeLabel1),
                storePreviewCountLabel1);
            m_StorePanel.SetSelectAllBtn(storeSelectAllBtn1);
            m_StorePanel.SetReloadBtn(storeReloadBtn1);
            m_StorePanel.SetUnpackBtn(storeUnpackBtn1, m_unpackPanel);
            m_StorePanel.SetDeleteBtn(storeDeleteBtn1);
            m_StorePanel.SetToBackupBtn(storeToBackupBtn1, m_BackupPanel);
            m_StorePanel.SetSortComboBox(storeComboBox1);

            storePreviewTipLabel1.Text = SettingManager.PreviewContentTip;
        }

        void UpdateStorePage()
        {
            m_StorePanel.UpdatePanel();
        }
        #endregion 订阅壁纸管理页面

        #region 本地备份管理页面
        /// <summary>
        /// 本地备份管理界面打开的页面下标
        /// </summary>
        int m_selectPageIndex = 0;
        /// <summary>
        /// 本地备份壁纸预览滑动框
        /// </summary>
        WallpaperFlowLayoutPanel m_LocalBackupPanel = new WallpaperFlowLayoutPanel();
        /// <summary>
        /// 壁纸变更管理页面
        /// </summary>
        WallpaperDataGridView m_GridViewPanel = new WallpaperDataGridView();

        void InitLocalBackupPage()
        {
            tabPage_LocalBackup.BackColor = Color.FromArgb(50, 49, 48);
            InitLocalBackupPage_0();
            InitLocalBackupPage_1();
        }

        void UpdateLocalBackupPage()
        {
            UpdateLocalBackupSelectPage(m_selectPageIndex);
        }

        // 子标签页切换时调用
        private void uiTabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = ((UITabControl)sender).SelectedIndex;
            m_selectPageIndex = selectedIndex;
            UpdateLocalBackupSelectPage(selectedIndex);
        }

        // 刷新子标签页
        void UpdateLocalBackupSelectPage(int index)
        {
            switch (index)
            {
                case 0:
                    m_LocalBackupPanel.UpdatePanel();
                    break;
                case 1:
                    m_GridViewPanel.UpdatePanel();
                    break;
            }
        }
        #region 本地备份管理页面_壁纸预览页面
        void InitLocalBackupPage_0()
        {
            m_LocalBackupPanel.Init(
                this,
                localBackupPageProgressBar1,
                WallpaperManager.localBackupLoader,
                localBackupEveryoneSwitch1,
                localBackupQuestionableSwitch1,
                localBackupMatureSwitch1);
            m_LocalBackupPanel.InitPanel(localBackupFlowLayoutPanel1);
            m_LocalBackupPanel.InitPreviewGroupBox(
                new InformationGroupBox(
                    localBackupPreviewPicture1,
                    localBackupPreviewNameLabel1,
                    localBackupPreviewContentratingLabel1,
                    localBackupPreviewTypeLabel1),
                localBackupPreviewCountLabel1);
            m_LocalBackupPanel.SetSelectAllBtn(localBackupSelectAllBtn1);
            m_LocalBackupPanel.SetReloadBtn(localBackupReloadBtn1);
            m_LocalBackupPanel.SetUnpackBtn(localBackupUnpackBtn1, m_unpackPanel);
            m_LocalBackupPanel.SetDeleteBtn(localBackupDeleteBtn1);
            m_LocalBackupPanel.SetToBackupBtn(localBackupToBackupBtn1, m_BackupPanel);
            m_LocalBackupPanel.SetSortComboBox(localBackupComboBox1);

            localBackupPreviewTipLabel1.Text = SettingManager.PreviewContentTip;
        }
        #endregion 本地备份管理页面_壁纸预览页面

        #region 本地备份管理页面_差异变更管理页面
        void InitLocalBackupPage_1()
        {
            m_GridViewPanel.Init(
                this,
                changedPanelProcessBar,
                (WallpaperLoader)WallpaperManager.wallpaperChanger,
                changedPanelSwitch1,
                changedPanelSwitch2,
                changedPanelSwitch3);
            m_GridViewPanel.InitAllDataGridViews(
                newGridView, newGridViewFooter,
                new InformationGroupBox(
                    newPictureBox,
                    newNameLabel,
                    newContentratingLabel,
                    newTypeLabel),
                changedGridView, changedGridViewFooter,
                new InformationGroupBox(
                    changedPictureBox,
                    changedNameLabel,
                    changedContentratingLabel,
                    changedTypeLabel),
                delGridView, delGridViewFooter,
                new InformationGroupBox(
                    delPictureBox,
                    delNameLabel,
                    delContentratingLabel,
                    delTypeLabel),
                m_StorePanel,
                m_LocalBackupPanel,
                m_BackupPanel);
            m_GridViewPanel.SetReloadBtn(reloadBtn1);
            m_GridViewPanel.SetSelectAllBtns(
                newViewSelectAllBtn,
                changedViewSelectAllBtn,
                delViewSelectAllBtn);
            m_GridViewPanel.SetUIBtn(syncBtn1, rollbackBtn1);

            uiLabel6.Text = SettingManager.PreviewContentTip_2;
            uiLabel4.Text = SettingManager.PreviewContentTip_2;
            uiLabel3.Text = SettingManager.PreviewContentTip_2;
        }
        #endregion 本地备份管理页面_差异变更管理页面

        #endregion 本地备份管理页面

        #region 官方备份管理页面
        /// <summary>
        /// 官方备份壁纸预览滑动框
        /// </summary>
        WallpaperFlowLayoutPanel m_BackupPanel = new WallpaperFlowLayoutPanel();
        void InitBackupPage()
        {
            m_BackupPanel.Init(
                this,
                backupPageProgressBar1,
                WallpaperManager.backupLoader,
                backupEveryoneSwitch1,
                backupQuestionableSwitch1,
                backupMatureSwitch1);
            m_BackupPanel.InitPanel(backupFlowLayoutPanel1);
            m_BackupPanel.InitPreviewGroupBox(
                new InformationGroupBox(
                    backupPreviewPicture1,
                    backupPreviewNameLabel1,
                    backupPreviewContentratingLabel1,
                    backupPreviewTypeLabel1),
                backupPreviewCountLabel1);
            m_BackupPanel.SetSelectAllBtn(backupSelectAllBtn1);
            m_BackupPanel.SetReloadBtn(backupReloadBtn1);
            m_BackupPanel.SetUnpackBtn(backupUnpackBtn1, m_unpackPanel);
            m_BackupPanel.SetDeleteBtn(backupDeleteBtn1);
            m_BackupPanel.SetSortComboBox(backupComboBox1);

            backupPreviewTipLabel1.Text = SettingManager.PreviewContentTip;
        }

        void UpdateBackupPage()
        {
            m_BackupPanel.UpdatePanel();
        }
        #endregion 官方备份管理页面

        #region 壁纸解包页面
        /// <summary>
        /// 解包壁纸预览滑动框
        /// </summary>
        WallpaperFlowLayoutPanel m_unpackPanel = new WallpaperFlowLayoutPanel();

        void InitUnpackPage()
        {
            m_unpackPanel.Init(
                this,
                unpackPageProgressBar1,
                WallpaperManager.unpackLoader,
                unpackEveryoneSwitch1,
                unpackQuestionableSwitch1,
                unpackMatureSwitch1);
            m_unpackPanel.InitPanel(unpackFlowLayoutPanel1);
            m_unpackPanel.InitPreviewGroupBox(
                new InformationGroupBox(
                    unpackPreviewPicture1,
                    unpackPreviewNameLabel1,
                    unpackPreviewContentratingLabel1,
                    unpackPreviewTypeLabel1),
                unpackPreviewCountLabel1);
            m_unpackPanel.SetSelectAllBtn(unpackSelectAllBtn1);
            m_unpackPanel.SetReloadBtn(unpackReloadBtn1);
            m_unpackPanel.SetDeleteBtn(unpackDeleteBtn1);
            m_unpackPanel.SetSortComboBox(unpackComboBox1);

            unpackPreviewTipLabel1.Text = SettingManager.PreviewContentTip;
        }

        void UpdateUnpackPage()
        {
            m_unpackPanel.UpdatePanel();
        }

        #endregion 壁纸解包页面

        private void UpdateUIUserControl1Size()
        {
            // 重置主页uiUserControl1的宽度
            uiUserControl1.Size = new Size(uiFlowLayoutPanel2.Size.Width - 28, uiUserControl1.Size.Height);
        }

        private void MainWindow_SizeChanged(object sender, EventArgs e)
        {
            UpdateUIUserControl1Size();
        }
    }
}
