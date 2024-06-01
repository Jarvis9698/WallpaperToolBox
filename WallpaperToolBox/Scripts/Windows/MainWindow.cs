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
                    break;
                // 设置页面
                case 1:
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
            m_SettingBackupPathPanel.UpdatePath(path);

            this.ShowSuccessTip("自动识别成功");
        }

        // 选择解包壁纸目录按钮
        private void unpackPathSelectBtn_Click(object sender, EventArgs e)
        {
            m_SettingUnpackPathPanel.OpenSelectPathView();
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
                storePreviewPicture1,
                storePreviewNameLabel1,
                storePreviewContentratingLabel1,
                storePreviewTypeLabel1,
                storePreviewCountLabel1);
            m_StorePanel.SetSelectAllBtn(storeSelectAllBtn1);
            m_StorePanel.SetReloadBtn(storeReloadBtn1);
            m_StorePanel.SetUnpackBtn(storeUnpackBtn1, m_unpackPanel);
            m_StorePanel.SetDeleteBtn(storeDeleteBtn1);
            m_StorePanel.SetToBackupBtn(storeToBackupBtn1, m_BackupPanel);

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

        void InitLocalBackupPage()
        {
            tabPage_LocalBackup.BackColor = Color.FromArgb(50, 49, 48);
            InitLocalBackupPage_0();
        }

        void UpdateLocalBackupPage()
        {
            UpdateLocalBackupSelectPage(m_selectPageIndex);
        }

        private void uiTabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = ((UITabControl)sender).SelectedIndex;
            m_selectPageIndex = selectedIndex;
            UpdateLocalBackupSelectPage(selectedIndex);
        }

        void UpdateLocalBackupSelectPage(int index)
        {
            switch (index)
            {
                case 0:
                    m_LocalBackupPanel.UpdatePanel();
                    break;
                case 1:
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
                localBackupPreviewPicture1,
                localBackupPreviewNameLabel1,
                localBackupPreviewContentratingLabel1,
                localBackupPreviewTypeLabel1,
                localBackupPreviewCountLabel1);
            m_LocalBackupPanel.SetSelectAllBtn(localBackupSelectAllBtn1);
            m_LocalBackupPanel.SetReloadBtn(localBackupReloadBtn1);
            m_LocalBackupPanel.SetUnpackBtn(localBackupUnpackBtn1, m_unpackPanel);
            m_LocalBackupPanel.SetDeleteBtn(localBackupDeleteBtn1);
            m_LocalBackupPanel.SetToBackupBtn(localBackupToBackupBtn1, m_BackupPanel);

            localBackupPreviewTipLabel1.Text = SettingManager.PreviewContentTip;
        }
        #endregion 本地备份管理页面_壁纸预览页面

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
                backupPreviewPicture1,
                backupPreviewNameLabel1,
                backupPreviewContentratingLabel1,
                backupPreviewTypeLabel1,
                backupPreviewCountLabel1);
            m_BackupPanel.SetSelectAllBtn(backupSelectAllBtn1);
            m_BackupPanel.SetReloadBtn(backupReloadBtn1);
            m_BackupPanel.SetUnpackBtn(backupUnpackBtn1, m_unpackPanel);
            m_BackupPanel.SetDeleteBtn(backupDeleteBtn1);

            backupPreviewTipLabel1.Text = SettingManager.PreviewContentTip;
        }

        void UpdateBackupPage()
        {
            m_BackupPanel.UpdatePanel();
        }
        #endregion 官方备份管理页面

        #region 壁纸解包页面
        /// <summary>
        /// 解包页面更新状态
        /// </summary>
        VieweUpdateState unpackPageState = VieweUpdateState.WaitingForReload;
        /// <summary>
        /// 解包壁纸预览滑动框
        /// </summary>
        WallpaperFlowLayoutPanel m_unpackPanel = new WallpaperFlowLayoutPanel();

        void InitUnpackPage()
        {
            InitStoreView1();
            InitUnpackView1();
            unpackPageState = VieweUpdateState.WaitingForReload;
        }

        /// <summary>
        /// 更新解包页面，未加载的所有列表重新加载
        /// </summary>
        void UpdateTabPageUnpack()
        {
            if (unpackPageState >= VieweUpdateState.IsUpdating)
            {
                return;
            }
            // 更新壁纸预览列表
            UpdateStoreView1();
            // 更新解包壁纸预览列表
            UpdateUnpackView1();
        }

        /// <summary>
        /// 设置解包页面的更新状态
        /// </summary>
        private void SetUnpackPageUpdateState(VieweUpdateState state)
        {
            if (isStoreViewUpdating || isUnpackViewUpdating)
            {
                state = VieweUpdateState.IsUpdating;
            }

            unpackPageState = state;

            if (state <= VieweUpdateState.IsUpdating)
            {
                everyoneSwitch2.ReadOnly = true;
                questionableSwitch2.ReadOnly = true;
                matureSwitch2.ReadOnly = true;
            }
            else
            {
                everyoneSwitch2.ReadOnly = false;
                questionableSwitch2.ReadOnly = false;
                matureSwitch2.ReadOnly = false;
            }
        }

#region 壁纸解包页面_壁纸预览列表

        /// <summary>
        /// 预览列表是否正在更新
        /// </summary>
        bool isStoreViewUpdating = false;

        // 初始化壁纸预览列表
        void InitStoreView1()
        {
            storeView1.Rows.Clear();
            storeView1.Rows[0].Height = SettingManager.PreviewRowsHeight;
            storeView1.Rows[0].Cells["storeView1_ID"].Value = "114514";
            storeView1.Rows[0].Cells["storeView1_Title"].Value = SettingManager.StoreViewEmptyTip;
            Image image = Image.FromFile(SettingManager.currentDirectoryPath + "Images\\image_1.jpg");
            Bitmap bitmap = new Bitmap(image, SettingManager.PreviewImageSize, SettingManager.PreviewImageSize);
            storeView1.Rows[0].Cells["storeView1_Preview"].Value = bitmap;

            storeViewFooter1.Clear();

            isStoreViewUpdating = false;
            storeViewProcessBar1.Hide();
            SetUnpackPageUpdateState(VieweUpdateState.Updated);
        }

        // 更新壁纸存放列表
        void UpdateStoreView1()
        {
            isStoreViewUpdating = true;

            storeViewList.Clear();
            storeViewSelectedIndexList.Clear();

            // 检查否需要重新读取壁纸信息
            if (!WallpaperManager.storeLoader.isLoaded && !WallpaperManager.storeLoader.isLoading)
            {
                WallpaperManager.storeLoader.Load();
            }

            // 开启进度条
            if (!storeViewProgressWorker.IsBusy)
            {
                storeViewProgressWorker.RunWorkerAsync();
            }
            storeViewProcessBar1.Show();
        }

        // 获取壁纸存放目录读取进度
        private void StoreViewProgressWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // 前面90%进度用于读取壁纸数据
            while (WallpaperManager.storeLoader.isLoading)
            {
                storeViewProgressWorker.ReportProgress((int)(WallpaperManager.storeLoader.loadingProgress * 0.9f));
                Thread.Sleep(10);
            }

            storeViewList = WallpaperManager.storeLoader.GetViewList(
                everyoneSwitch2.Active,
                questionableSwitch2.Active,
                matureSwitch2.Active);

            int storeViewIndex = 0;
            // 后面10%进度用于刷新列表
            while (isStoreViewUpdating && storeViewIndex < storeViewList.Count)
            {
                storeViewIndex++;
                storeViewProgressWorker.ReportProgress(storeViewIndex);

                // 每次更新100张壁纸
                if (storeViewIndex % 100 == 0)
                {
                    Thread.Sleep(10);
                }
            }
            storeViewProgressWorker.ReportProgress(100);
        }

        // 更新壁纸存放目录读取进度条
        private void StoreViewProgressWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!isStoreViewUpdating)
            {
                return;
            }

            int progress = e.ProgressPercentage;

            if (WallpaperManager.storeLoader.isLoading)
            {
                storeViewProcessBar1.Value = progress;
            }
            else if (storeViewList == null || storeViewList.Count == 0)
            {
                InitStoreView1();
                return;
            }
            else
            {
                if (progress == 1)
                {
                    storeView1.Rows.Clear();
                    storeViewFooter1.Clear();
                    storeView1.Rows.Add(storeViewList.Count - 1);
                }

                int i = progress - 1;

                storeView1.Rows[i].Cells[1].Value = storeViewList[i].id;
                storeView1.Rows[i].Cells[2].Value = storeViewList[i].title;
                storeView1.Rows[i].Cells[3].Value = storeViewList[i].previewImage;

                storeView1.Rows[i].Height = SettingManager.PreviewRowsHeight;

                storeViewFooter1["storeView1_ID"] = "合计：" + progress;
                storeViewFooter1["storeView1_Title"] = "(双击图片打开目录)";

                storeViewProcessBar1.Value = 90 + 10 * progress / storeViewList.Count;
            }

            // 进度条跑完
            if (storeViewList.Count != 0 && 90 + 10 * progress / storeViewList.Count >= 100)
            {
                storeViewProcessBar1.Hide();
                isStoreViewUpdating = false;
                SetUnpackPageUpdateState(VieweUpdateState.Updated);
            }
        }

        #region 壁纸解包页面_壁纸预览列表_UI事件

        // 单击预览的壁纸
        private void StoreView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (isStoreViewUpdating)
            {
                return;
            }

            if (e.RowIndex < 0)
            {
                return;
            }

            if (e.ColumnIndex == 0 && storeViewList.Count > 0)
            {
                bool select = !(bool)storeView1.Rows[e.RowIndex].Cells[0].EditedFormattedValue;
                DataGridViewCheckBoxCell chekBox = (DataGridViewCheckBoxCell)storeView1.Rows[e.RowIndex].Cells[0];
                chekBox.Value = select;
                chekBox.EditingCellFormattedValue = select;
                //StoreGridView1.InvalidateCell(0, e.RowIndex);

                Tools.UpdateSelectIndexList(storeViewSelectedIndexList, e.RowIndex, select);
            }
        }

        // 双击壁纸存放列表
        private void StoreView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (isStoreViewUpdating)
            {
                return;
            }

            if (e.ColumnIndex == 0 || e.RowIndex < 0)
            {
                return;
            }

            if (storeViewList.Count > 0)
            {
                string path = storeViewList[e.RowIndex].directoryPath;
                // 打开目录
                System.Diagnostics.Process.Start(path);
            }
        }

        // 全选按钮
        private void storeSelectAllBtn_Click(object sender, EventArgs e)
        {
            if (isStoreViewUpdating)
            {
                return;
            }

            Tools.DataGridViewSelectAll(ref storeView1, storeViewList, storeViewSelectedIndexList);
        }
#endregion 壁纸解包页面_壁纸预览列表_UI事件
#endregion 壁纸解包页面_壁纸预览列表

#region 壁纸解包页面_解包壁纸预览列表

        /// <summary>
        /// 用于预览的壁纸列表
        /// </summary>
        List<Wallpaper> unpackViewList = new List<Wallpaper>();
        /// <summary>
        /// 壁纸预览列表中已勾选的下标列表，对应unpackViewList的下标
        /// </summary>
        List<int> unpackViewSelectedIndexList = new List<int>();
        /// <summary>
        /// 解包列表是否正在更新
        /// </summary>
        bool isUnpackViewUpdating = false;

        // 初始化解包壁纸预览列表
        void InitUnpackView1()
        {
            unpackView1.Rows.Clear();
            unpackView1.Rows[0].Height = SettingManager.PreviewRowsHeight;
            unpackView1.Rows[0].Cells["unpackView1_ID"].Value = "114514";
            unpackView1.Rows[0].Cells["unpackView1_Title"].Value = SettingManager.UnpackViewEmptyTip;
            Image image = Image.FromFile(SettingManager.currentDirectoryPath + "Images\\image_1.jpg");
            Bitmap bitmap = new Bitmap(image, SettingManager.PreviewImageSize, SettingManager.PreviewImageSize);
            unpackView1.Rows[0].Cells["unpackView1_Preview"].Value = bitmap;

            unpackViewFooter1.Clear();

            isUnpackViewUpdating = false;
            unpackViewProcessBar1.Hide();
            SetUnpackPageUpdateState(VieweUpdateState.Updated);
        }

        // 更新解包列表
        void UpdateUnpackView1()
        {
            isUnpackViewUpdating = true;

            unpackViewList.Clear();
            unpackViewSelectedIndexList.Clear();

            // 检查否需要重新读取壁纸信息
            if (!WallpaperManager.unpackLoader.isLoaded && !WallpaperManager.unpackLoader.isLoading)
            {
                WallpaperManager.unpackLoader.Load();
            }

            // 开启进度条
            if (!unpackViewProgressWorker.IsBusy)
            {
                unpackViewProgressWorker.RunWorkerAsync();
            }
            unpackViewProcessBar1.Show();
        }

        // 获取解包目录读取进度
        private void unpackViewProgressWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // 前面90%进度用于读取壁纸数据
            while (WallpaperManager.unpackLoader.isLoading)
            {
                unpackViewProgressWorker.ReportProgress((int)(WallpaperManager.unpackLoader.loadingProgress * 0.9f));
                Thread.Sleep(10);
            }

            unpackViewList = WallpaperManager.unpackLoader.GetViewList(
                everyoneSwitch2.Active,
                questionableSwitch2.Active,
                matureSwitch2.Active);

            int unpackViewIndex = 0;
            // 后面10%进度用于刷新列表
            while (isUnpackViewUpdating && unpackViewIndex < unpackViewList.Count)
            {
                unpackViewIndex++;
                unpackViewProgressWorker.ReportProgress(unpackViewIndex);

                // 每次更新100张壁纸
                if (unpackViewIndex % 100 == 0)
                {
                    Thread.Sleep(10);
                }
            }
            unpackViewProgressWorker.ReportProgress(100);
        }

        // 更新解包目录读取进度条
        private void unpackViewProgressWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!isUnpackViewUpdating)
            {
                return;
            }

            int progress = e.ProgressPercentage;

            if (WallpaperManager.unpackLoader.isLoading)
            {
                unpackViewProcessBar1.Value = progress;
            }
            else if (unpackViewList == null || unpackViewList.Count == 0)
            {
                InitUnpackView1();
                return;
            }
            else
            {
                if (progress == 1)
                {
                    unpackView1.Rows.Clear();
                    unpackViewFooter1.Clear();
                    unpackView1.Rows.Add(unpackViewList.Count - 1);
                }

                int i = progress - 1;

                unpackView1.Rows[i].Cells["unpackView1_ID"].Value = unpackViewList[i].id;
                unpackView1.Rows[i].Cells["unpackView1_Title"].Value = unpackViewList[i].title;
                unpackView1.Rows[i].Cells["unpackView1_Preview"].Value = unpackViewList[i].previewImage;

                unpackView1.Rows[i].Height = SettingManager.PreviewRowsHeight;

                unpackViewFooter1["unpackView1_ID"] = "合计：" + progress;
                unpackViewFooter1["unpackView1_Title"] = "(双击图片打开目录)";

                unpackViewProcessBar1.Value = 90 + 10 * progress / unpackViewList.Count;
            }

            // 进度条跑完
            if (unpackViewList.Count != 0 && 90 + 10 * progress / unpackViewList.Count >= 100)
            {
                unpackViewProcessBar1.Hide();
                isUnpackViewUpdating = false;
                SetUnpackPageUpdateState(VieweUpdateState.Updated);
            }
        }

#region 壁纸解包页面_解包壁纸预览列表_UI事件

        // 单击预览解包壁纸
        private void unpackView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (isUnpackViewUpdating)
            {
                return;
            }

            if (e.RowIndex < 0)
            {
                return;
            }

            if (e.ColumnIndex == 0 && unpackViewList.Count > 0)
            {
                bool select = !(bool)unpackView1.Rows[e.RowIndex].Cells[0].EditedFormattedValue;
                DataGridViewCheckBoxCell chekBox = (DataGridViewCheckBoxCell)unpackView1.Rows[e.RowIndex].Cells[0];
                chekBox.Value = select;
                chekBox.EditingCellFormattedValue = select;

                Tools.UpdateSelectIndexList(unpackViewSelectedIndexList, e.RowIndex, select);
            }
        }

        // 双击打开目录
        private void unpackView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (isUnpackViewUpdating)
            {
                return;
            }

            if (e.ColumnIndex == 0 || e.RowIndex < 0)
            {
                return;
            }

            if (unpackViewList.Count > 0)
            {
                string path = unpackViewList[e.RowIndex].directoryPath;
                // 打开目录
                System.Diagnostics.Process.Start(path);
            }
        }

        // 全选按钮
        private void unpackSelectAllBtn_Click(object sender, EventArgs e)
        {
            if (isUnpackViewUpdating)
            {
                return;
            }

            Tools.DataGridViewSelectAll(ref unpackView1, unpackViewList, unpackViewSelectedIndexList);
        }
#endregion 壁纸解包页面_解包壁纸预览列表_UI事件

#endregion 壁纸解包页面_解包壁纸预览列表

#region 壁纸解包页面_按钮及过滤器
        // 刷新列表按钮
        private void tabPageUnpackUpdateBtn1_Click(object sender, EventArgs e)
        {
            if (isStoreViewUpdating || isUnpackViewUpdating)
            {
                return;
            }

            WallpaperManager.storeLoader.WaitForReload();
            WallpaperManager.unpackLoader.WaitForReload();
            SetUnpackPageUpdateState(VieweUpdateState.WaitingForReload);

            UpdateTabPageUnpack();
        }

        // 解包按钮
        private void unpackBtn1_Click(object sender, EventArgs e)
        {
            if (isStoreViewUpdating || isUnpackViewUpdating)
            {
                return;
            }

            if (storeViewSelectedIndexList.Count == 0)
            {
                this.ShowWarningTip(SettingManager.StoreViewSelectEmptyTip);
                return;
            }

            string txt = "将解包 " + storeViewSelectedIndexList.Count + " 个壁纸\n" +
                "其中已解包过的壁纸将会重新解包并覆盖\n" +
                "无法解包的壁纸将自动跳过";
            Tools.ShowAskDialog(txt, this);

            // 解包逻辑
            //...
        }

        // 删除按钮
        private void deleteBtn1_Click(object sender, EventArgs e)
        {
            if (isStoreViewUpdating || isUnpackViewUpdating)
            {
                return;
            }

            if (storeViewSelectedIndexList.Count == 0 && unpackViewSelectedIndexList.Count == 0)
            {
                this.ShowWarningTip(SettingManager.PreviewDeleteEmptyTip);
                return;
            }

            string txt = "将删除：\n" +
                storeViewSelectedIndexList.Count + " 个创意工坊壁纸文件\n" +
                unpackViewSelectedIndexList.Count + " 个已解包的壁纸文件";
            Tools.ShowAskWarningAskDialog(txt, this);

            // 删除逻辑
            //...
        }

        // 大众级选项更改时
        private void everyoneSwitch1_ValueChanged(object sender, bool value)
        {
            if (isStoreViewUpdating || isUnpackViewUpdating)
            {
                return;
            }

            SetUnpackPageUpdateState(VieweUpdateState.WaitingForUpdateView);
            UpdateTabPageUnpack();
        }

        // 13+选项更改时
        private void questionableSwitch1_ValueChanged(object sender, bool value)
        {
            if (isStoreViewUpdating || isUnpackViewUpdating)
            {
                return;
            }

            SetUnpackPageUpdateState(VieweUpdateState.WaitingForUpdateView);
            UpdateTabPageUnpack();
        }

        // 18+选项更改时
        private void matureSwitch1_ValueChanged(object sender, bool value)
        {
            if (isStoreViewUpdating || isUnpackViewUpdating)
            {
                return;
            }

            SetUnpackPageUpdateState(VieweUpdateState.WaitingForUpdateView);
            UpdateTabPageUnpack();
        }
        #endregion 壁纸解包页面_按钮及过滤器

        #endregion 壁纸解包页面
    }
}
