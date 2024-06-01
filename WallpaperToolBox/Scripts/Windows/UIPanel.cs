using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WallpaperToolBox
{
    /// <summary>
    /// UI页面更新状态
    /// </summary>
    public enum VieweUpdateState
    {
        /// <summary>
        /// 等待重新加载
        /// </summary>
        WaitingForReload = 0,
        /// <summary>
        /// 等待更新页面
        /// </summary>
        WaitingForUpdateView = 1,
        /// <summary>
        /// 更新页面中
        /// </summary>
        IsUpdating = 2,
        /// <summary>
        /// 已更新
        /// </summary>
        Updated = 3,
    }

    /// <summary>
    /// 预览壁纸UI基类
    /// </summary>
    public abstract class WallpaperFlowPanelBase
    {
        /// <summary>
        /// 滑动框所在的窗口
        /// </summary>
        protected UIForm m_UIForm;
        /// <summary>
        /// 进度条
        /// </summary>
        protected UIProcessBar m_ProgressBar;
        /// <summary>
        /// 壁纸加载器
        /// </summary>
        protected WallpaperLoader m_WallpaperLoader;
        /// <summary>
        /// 全年龄开关
        /// </summary>
        protected UISwitch m_EverySwitch;
        /// <summary>
        /// 13+开关
        /// </summary>
        protected UISwitch m_QuestionableSwitch;
        /// <summary>
        /// 18+开关
        /// </summary>
        protected UISwitch m_MatureSwitch;

        /// <summary>
        /// 用于预览的壁纸列表
        /// </summary>
        protected List<Wallpaper> m_WallpaperList = new List<Wallpaper>();
        /// <summary>
        /// 壁纸预览列表中已勾选的下标列表，对应m_WallpaperList的下标
        /// </summary>
        protected List<int> m_SelectedIndexList = new List<int>();

        /// <summary>
        /// 加载进度管理后台
        /// </summary>
        protected BackgroundWorker m_LoadProgressWorker = new BackgroundWorker();

        /// <summary>
        /// 是否已启动
        /// </summary>
        public bool isInited { get; protected set; }

        private VieweUpdateState m_PanelUpdateState { get; set; }
        /// <summary>
        /// 滚动列表更新状态
        /// </summary>
        public VieweUpdateState panelUpdateState
        {
            get
            {
                return m_PanelUpdateState;
            }
            protected set
            {
                m_PanelUpdateState = value;
                // 用新线程是因为在主线程延迟更新会有进度条UIBug
                Task.Run(AfterUpdateStateChanged);
            }
        }

        #region 接口
        public virtual void Init(
            UIForm uIForm,
            UIProcessBar processBar,
            WallpaperLoader wallpaperLoader,
            UISwitch everySwitch,
            UISwitch questionableSwitch,
            UISwitch matureSwitch)
        {
            // UI初始化
            m_UIForm = uIForm;
            m_ProgressBar = processBar;
            m_ProgressBar.Hide();

            m_WallpaperLoader = wallpaperLoader;

            // 过滤器初始化
            m_EverySwitch = everySwitch;
            m_QuestionableSwitch = questionableSwitch;
            m_MatureSwitch = matureSwitch;
            m_EverySwitch.ValueChanged += OnSwitchValueChanged;
            m_QuestionableSwitch.ValueChanged += OnSwitchValueChanged;
            m_MatureSwitch.ValueChanged += OnSwitchValueChanged;

            // 后台初始化
            m_LoadProgressWorker.WorkerReportsProgress = true;
            m_LoadProgressWorker.DoWork += LoadProgressWorker_DoWork;
            m_LoadProgressWorker.ProgressChanged += LoadProgressWorker_ProgressChanged;

            isInited = true;
            WaitForReload();
        }

        /// <summary>
        /// 准备重新加载壁纸
        /// </summary>
        public virtual void WaitForReload()
        {
            if (!isInited)
            {
                return;
            }
            m_WallpaperLoader.WaitForReload();
            panelUpdateState = VieweUpdateState.WaitingForReload;
        }

        /// <summary>
        /// 刷新滚动列表
        /// </summary>
        public virtual void UpdatePanel()
        {
            if (!m_WallpaperLoader.isLoaded)
            {
                panelUpdateState = VieweUpdateState.WaitingForReload;
            }

            // 若已更新，就直接跳过
            if (panelUpdateState >= VieweUpdateState.IsUpdating)
            {
                return;
            }

            panelUpdateState = VieweUpdateState.IsUpdating;

            m_SelectedIndexList.Clear();
            AfterUpdateSelectRange();

            // 只是更新页面则会直接返回，重新加载才会深入执行
            m_WallpaperLoader.Load();

            if (!m_LoadProgressWorker.IsBusy)
            {
                m_LoadProgressWorker.RunWorkerAsync();
                m_ProgressBar.Show();
            }
        }

        /// <summary>
        /// 全选 / 全不选
        /// </summary>
        public virtual void SelectAll()
        {
            bool isSelectAll = true;
            if (m_SelectedIndexList.Count == m_WallpaperList.Count)
            {
                isSelectAll = false;
            }

            UpdateSelectRange(0, m_WallpaperList.Count - 1, isSelectAll);
        }

        /// <summary>
        /// 设置全选按钮
        /// </summary>
        public virtual void SetSelectAllBtn(UIButton button)
        {
            button.Click += (sender, e) =>
            {
                if (panelUpdateState != VieweUpdateState.Updated)
                {
                    return;
                }
                SelectAll();
            };
        }

        /// <summary>
        /// 设置重新加载按钮
        /// </summary>
        public virtual void SetReloadBtn(UIButton button)
        {
            button.Click += (sender, e) =>
            {
                if (panelUpdateState <= VieweUpdateState.IsUpdating)
                {
                    return;
                }
                WaitForReload();
                UpdatePanel();
            };
        }
        #endregion 接口

        #region 后台

        /// <summary>
        /// 启动加载后台
        /// </summary>
        protected virtual void LoadProgressWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // 前面95%进度用于读取壁纸数据
            while (m_WallpaperLoader.isLoading)
            {
                m_LoadProgressWorker.ReportProgress((int)(m_WallpaperLoader.loadingProgress * 0.95f));
                Thread.Sleep(20);
            }

            m_WallpaperList = m_WallpaperLoader.GetViewList(
                m_EverySwitch.Active,
                m_QuestionableSwitch.Active,
                m_MatureSwitch.Active);

            // 后面5%进度用于刷新列表，此时progress不再表示进度，而是滚动列表每次刷新按钮的数量
            int progress = 0;
            while (panelUpdateState != VieweUpdateState.Updated)
            {
                m_LoadProgressWorker.ReportProgress(progress);

                progress += 100;
                Thread.Sleep(50);
            }
        }

        /// <summary>
        /// 加载后台同步进度(需要重写)
        /// </summary>
        protected virtual void LoadProgressWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }
        #endregion 后台

        /// <summary>
        /// 更新状态变化后
        /// </summary>
        private void AfterUpdateStateChanged()
        {
            Thread.Sleep(50);
            if (panelUpdateState <= VieweUpdateState.IsUpdating)
            {
                m_EverySwitch.ReadOnly = true;
                m_QuestionableSwitch.ReadOnly = true;
                m_MatureSwitch.ReadOnly = true;
            }
            else
            {
                m_EverySwitch.ReadOnly = false;
                m_QuestionableSwitch.ReadOnly = false;
                m_MatureSwitch.ReadOnly = false;
            }
        }

        /// <summary>
        /// 变更过滤条件
        /// </summary>
        protected void OnSwitchValueChanged(object sender, bool value)
        {
            if (panelUpdateState <= VieweUpdateState.IsUpdating)
            {
                return;
            }

            panelUpdateState = VieweUpdateState.WaitingForUpdateView;
            UpdatePanel();
        }

        /// <summary>
        /// 更新范围内的已选下标
        /// </summary>
        protected void UpdateSelectRange(int startIndex, int endIndex, bool isSelect)
        {
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (isSelect && !m_SelectedIndexList.Contains(i))
                {
                    m_SelectedIndexList.Add(i);
                }
                else if (!isSelect)
                {
                    m_SelectedIndexList.Remove(i);
                }
            }

            AfterUpdateSelectRange();
        }

        /// <summary>
        /// 已选下标更新之后(一般用于刷新勾选UI)(需要重写)
        /// </summary>
        protected virtual void AfterUpdateSelectRange()
        {

        }
    }

    /// <summary>
    /// 壁纸预览滑动框
    /// </summary>
    public class WallpaperFlowLayoutPanel : WallpaperFlowPanelBase
    {
        /// <summary>
        /// 托管的滑动框
        /// </summary>
        UIFlowLayoutPanel m_Panel;

        /// <summary>
        /// 滚动列表里的按钮
        /// </summary>
        List<UIHeaderButton> m_ButtonList = new List<UIHeaderButton>();

        /// <summary>
        /// 删除进度管理后台
        /// </summary>
        BackgroundWorker m_DeleteProgressWorker = new BackgroundWorker();
        /// <summary>
        /// 解包进度管理后台
        /// </summary>
        BackgroundWorker m_UnpackProgressWorker = new BackgroundWorker();
        /// <summary>
        /// 添加官方备份进度管理后台
        /// </summary>
        BackgroundWorker m_ToBackupProgressWorker = new BackgroundWorker();

        /// <summary>
        /// 壁纸预览图
        /// </summary>
        PictureBox m_PreviewPictureBox;
        /// <summary>
        /// 壁纸预览名
        /// </summary>
        UILabel m_PreviewNameLabel;
        /// <summary>
        /// 壁纸分级
        /// </summary>
        UILabel m_PreviewContentratingLabel;
        /// <summary>
        /// 壁纸类型
        /// </summary>
        UILabel m_previewTypeLabel;
        /// <summary>
        /// 壁纸预览数量
        /// </summary>
        UILabel m_PreviewCountLabel;
        /// <summary>
        /// 最后一个选择的按钮下标
        /// </summary>
        public int lastSelectIndex { get; private set; } = -1;

        #region 接口
        public override void Init(UIForm uIForm,
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
        /// 初始化托管的滑动框
        /// </summary>
        public void InitPanel(UIFlowLayoutPanel panel)
        {
            m_Panel = panel;

            isInited = true;
        }

        /// <summary>
        /// 壁纸信息UI初始化
        /// </summary>
        public void InitPreviewGroupBox(
            PictureBox previewPictureBox,
            UILabel previewNameLabel,
            UILabel previewContentratingLabel,
            UILabel previewTypeLabel,
            UILabel previewCountLabel)
        {
            m_PreviewPictureBox = previewPictureBox;
            m_PreviewNameLabel = previewNameLabel;
            m_PreviewContentratingLabel = previewContentratingLabel;
            m_previewTypeLabel = previewTypeLabel;
            m_PreviewCountLabel = previewCountLabel;

            InitPreviewGroupBox();
        }

        /// <summary>
        /// 设置解包按钮
        /// </summary>
        public void SetUnpackBtn(UIButton button, WallpaperFlowLayoutPanel unpackPanel)
        {
            button.Click += (sender, e) =>
            {
                if (panelUpdateState != VieweUpdateState.Updated)
                {
                    return;
                }

                if (m_SelectedIndexList.Count == 0)
                {
                    m_UIForm.ShowWarningTip(SettingManager.PreviewUnpackSelectEmptyTip);
                    return;
                }

                if (!Directory.Exists(SettingManager.setting.unpackPath))
                {
                    m_UIForm.ShowWarningDialog(SettingManager.PreviewUnpackPathEmptyTip);
                    return;
                }

                string txt = "将解包 " + m_SelectedIndexList.Count + " 个壁纸\n" +
                    "注意：\n" +
                    "只能解包类型为scene的壁纸！\n" +
                    "无法解包的壁纸将自动跳过\n" +
                    "其中已解包过的壁纸将会重新解包并覆盖";
                if (Tools.ShowAskDialog(txt, m_UIForm))
                {
                    panelUpdateState = VieweUpdateState.WaitingForUpdateView;
                    if (!m_UnpackProgressWorker.IsBusy)
                    {
                        m_UnpackProgressWorker.RunWorkerAsync();
                        m_ProgressBar.Show();
                    }
                }
            };
            // 后台初始化
            m_UnpackProgressWorker.WorkerReportsProgress = true;
            m_UnpackProgressWorker.DoWork += UnpackProgressWorker_DoWork;
            m_UnpackProgressWorker.ProgressChanged += (sender, e) =>
            {
                UnpackProgressWorker_ProgressChanged(sender, e, unpackPanel);
            };
        }

        /// <summary>
        /// 设置删除按钮
        /// </summary>
        public void SetDeleteBtn(UIButton button)
        {
            button.Click += (sender, e) =>
            {
                if (panelUpdateState != VieweUpdateState.Updated)
                {
                    return;
                }

                if (m_SelectedIndexList.Count == 0)
                {
                    m_UIForm.ShowWarningTip(SettingManager.PreviewDeleteEmptyTip);
                    return;
                }

                string txt = "将删除：\n" +
                    m_SelectedIndexList.Count + " 个壁纸文件\n" +
                    "注意：\n" +
                    "未订阅的壁纸删除后将无法恢复！";
                if (Tools.ShowAskWarningAskDialog(txt, m_UIForm))
                {
                    panelUpdateState = VieweUpdateState.WaitingForUpdateView;
                    if (!m_DeleteProgressWorker.IsBusy)
                    {
                        m_DeleteProgressWorker.RunWorkerAsync();
                        m_ProgressBar.Show();
                    }
                }
            };
            // 后台初始化
            m_DeleteProgressWorker.WorkerReportsProgress = true;
            m_DeleteProgressWorker.DoWork += DeleteProgressWorker_DoWork;
            m_DeleteProgressWorker.ProgressChanged += DeleteProgressWorker_ProgressChanged;
        }

        /// <summary>
        /// 设置添加到官方备份按钮
        /// </summary>
        public void SetToBackupBtn(UIButton button, WallpaperFlowLayoutPanel backupPanel)
        {
            button.Click += (sender, e) =>
            {
                if (panelUpdateState != VieweUpdateState.Updated)
                {
                    return;
                }

                if (m_SelectedIndexList.Count == 0)
                {
                    m_UIForm.ShowWarningTip(SettingManager.PreviewToBackupSelectEmptyTip);
                    return;
                }

                if (!Directory.Exists(SettingManager.setting.backupPath))
                {
                    m_UIForm.ShowWarningDialog(SettingManager.PreviewToBackupPathEmptyTip);
                    return;
                }

                string txt = "将添加 " + m_SelectedIndexList.Count + " 个壁纸到官方备份目录\n" +
                    "注意：\n" +
                    "已添加过的壁纸会自动覆盖\n" +
                    "添加完成后重启Wallpaper Engine才有用\n";
                if (Tools.ShowAskDialog(txt, m_UIForm))
                {
                    panelUpdateState = VieweUpdateState.WaitingForUpdateView;
                    if (!m_ToBackupProgressWorker.IsBusy)
                    {
                        m_ToBackupProgressWorker.RunWorkerAsync();
                        m_ProgressBar.Show();
                    }
                }
            };
            // 后台初始化
            m_ToBackupProgressWorker.WorkerReportsProgress = true;
            m_ToBackupProgressWorker.DoWork += ToBackupProgressWorker_DoWork;
            m_ToBackupProgressWorker.ProgressChanged += (sender, e) =>
            {
                ToBackupProgressWorker_ProgressChanged(sender, e, backupPanel);
            };
        }
        #endregion 接口

        #region 后台
        // 加载后台同步进度
        protected override void LoadProgressWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int progress = e.ProgressPercentage;

            // 壁纸读取进度条
            if (m_WallpaperLoader.isLoading)
            {
                m_ProgressBar.Value = progress;
            }
            // 页面刷新进度条
            else
            {
                UpdateUIHeaderButton(progress);
                if (m_WallpaperList.Count > 0)
                {
                    m_ProgressBar.Value = 95 + 5 * (progress + 1) / m_WallpaperList.Count;
                }
                else
                {
                    // 没有可预览的壁纸，显示清空按钮的进度
                    m_ProgressBar.Value = 95 + 5 * progress / (m_ButtonList.Count + 1);
                }
            }

            if (m_WallpaperLoader.isLoaded && m_ProgressBar.Value >= 100)
            {
                InitPreviewGroupBox();
                m_ProgressBar.Hide();
                m_ProgressBar.Value = 0;
                panelUpdateState = VieweUpdateState.Updated;
            }
        }

        // 启动删除后台
        private void DeleteProgressWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<Wallpaper> selectWallpapers = new List<Wallpaper>();
            m_SelectedIndexList.Sort();
            for (int i = 0; i < m_SelectedIndexList.Count; i++)
            {
                selectWallpapers.Add(m_WallpaperList[m_SelectedIndexList[i]]);
            }
            m_WallpaperLoader.Remove(selectWallpapers);
            m_DeleteProgressWorker.ReportProgress(0);

            string cmd;
            int progress;
            for (int i = 0; i < selectWallpapers.Count; i++)
            {
                cmd = "rmdir /s/q \"" + selectWallpapers[i].directoryPath + "\"";
                Tools.RunCMD(cmd);
                progress = 100 * (i + 1) / selectWallpapers.Count;
                m_DeleteProgressWorker.ReportProgress(progress);

                Thread.Sleep(50);
            }
        }

        // 删除后台同步进度
        private void DeleteProgressWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int progress = e.ProgressPercentage;
            m_ProgressBar.Value = progress;

            if (progress >= 100)
            {
                m_ProgressBar.Hide();
                UpdatePanel();
            }
        }

        // 启动解包后台
        private void UnpackProgressWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<Wallpaper> selectWallpapers = new List<Wallpaper>();
            m_SelectedIndexList.Sort();
            Wallpaper wallpaper;
            for (int i = 0; i < m_SelectedIndexList.Count; i++)
            {
                wallpaper = m_WallpaperList[m_SelectedIndexList[i]];
                if (wallpaper.type.ToLower() == "scene")
                {
                    selectWallpapers.Add(wallpaper);
                }
            }

            string unpackPath;
            string cmd;
            int progress;
            for (int i = 0; i < selectWallpapers.Count; i++)
            {
                unpackPath = SettingManager.setting.unpackPath +
                    selectWallpapers[i].id + SettingManager.UnpackDirectorySuffix + "\\";
                cmd = "RePKG\\RePKG.exe extract " +
                    selectWallpapers[i].directoryPath + "scene.pkg" +
                    " -o " +
                    unpackPath;
                Tools.RunCMD(cmd);

                if (Directory.Exists(unpackPath))
                {
                    File.Copy(selectWallpapers[i].directoryPath + "project.json", unpackPath + "project.json", true);
                    File.Copy(selectWallpapers[i].directoryPath + selectWallpapers[i].preview, unpackPath + selectWallpapers[i].preview, true);
                }

                progress = 100 * (i + 1) / selectWallpapers.Count;
                m_UnpackProgressWorker.ReportProgress(progress);

                Thread.Sleep(50);
            }
        }

        // 解包后台同步进度
        private void UnpackProgressWorker_ProgressChanged(object sender, ProgressChangedEventArgs e, WallpaperFlowLayoutPanel unpackPanel)
        {
            int progress = e.ProgressPercentage;
            m_ProgressBar.Value = progress;

            if (progress >= 100)
            {
                m_ProgressBar.Hide();
                panelUpdateState = VieweUpdateState.Updated;
                UpdateSelectRange(0, m_WallpaperList.Count - 1, false);
                unpackPanel.WaitForReload();
            }
        }

        // 启动添加到官方备份目录后台
        private void ToBackupProgressWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<Wallpaper> selectWallpapers = new List<Wallpaper>();
            m_SelectedIndexList.Sort();
            for (int i = 0; i < m_SelectedIndexList.Count; i++)
            {
                selectWallpapers.Add(m_WallpaperList[m_SelectedIndexList[i]]);
            }

            string backupPath;
            string cmd;
            int progress;
            for (int i = 0; i < selectWallpapers.Count; i++)
            {
                backupPath = SettingManager.setting.backupPath + selectWallpapers[i].id + "\\";
                cmd = "XCOPY \"" + selectWallpapers[i].directoryPath + "\" \"" + backupPath + "\" /e/y";
                Tools.RunCMD(cmd);

                progress = 100 * (i + 1) / selectWallpapers.Count;
                m_ToBackupProgressWorker.ReportProgress(progress);

                Thread.Sleep(50);
            }
        }

        // 添加到官方备份目录后台同步进度
        private void ToBackupProgressWorker_ProgressChanged(object sender, ProgressChangedEventArgs e, WallpaperFlowLayoutPanel backupPanel)
        {
            int progress = e.ProgressPercentage;
            m_ProgressBar.Value = progress;

            if (progress >= 100)
            {
                m_ProgressBar.Hide();
                panelUpdateState = VieweUpdateState.Updated;
                UpdateSelectRange(0, m_WallpaperList.Count - 1, false);
                backupPanel.WaitForReload();
            }
        }
        #endregion 后台

        #region 按钮
        // 更新列表按钮
        private void UpdateUIHeaderButton(int headIndex)
        {
            int maxCount = 100;
            int btnCount = m_ButtonList.Count;
            int wallpaperCount = m_WallpaperList.Count;

            // 调整按钮数量
            if (btnCount < wallpaperCount)
            {
                for (int i = btnCount; i < wallpaperCount && i - btnCount < maxCount; i++)
                {
                    UIHeaderButton btn = CreateUIHeaderButton(i.ToString());
                    m_ButtonList.Add(btn);
                    m_Panel.Add(btn);
                }
            }
            else if (btnCount > wallpaperCount)
            {
                for (int i = 0; i < maxCount && m_ButtonList.Count > m_WallpaperList.Count; i++)
                {
                    UIHeaderButton btn = m_ButtonList[m_ButtonList.Count - 1];
                    m_ButtonList.RemoveAt(m_ButtonList.Count - 1);
                    m_Panel.Remove(btn);
                }
            }

            // 逐个设置按钮
            for (int i = headIndex; i < m_ButtonList.Count && i < m_WallpaperList.Count; i++)
            {
                m_ButtonList[i].Image = m_WallpaperList[i].previewImage;
                m_ButtonList[i].Text = Tools.GetStringWithByteLength(m_WallpaperList[i].title, 16);
                m_ButtonList[i].Selected = false;
            }
        }

        // 创建按钮
        private UIHeaderButton CreateUIHeaderButton(string id)
        {
            int size = SettingManager.PreviewRowsHeight;
            int top = (size - SettingManager.PreviewImageSize) / 2;

            UIHeaderButton btn = new UIHeaderButton();
            btn.Name = id;
            btn.Size = new Size(size, size + 20);
            btn.ImageTop = top;
            btn.Font = new Font("微软雅黑", 8f);
            btn.ForeHoverColor = Color.FromArgb(90, 90, 90);
            btn.ForePressColor = Color.FromArgb(64, 64, 64);
            btn.ForeSelectedColor = Color.FromArgb(64, 64, 64);

            btn.Style = UIStyle.Custom;
            btn.StyleCustomMode = true;
            btn.FillColor = Color.FromArgb(115, 115, 115);
            btn.FillDisableColor = Color.FromArgb(115, 115, 115);
            btn.FillHoverColor = Color.FromArgb(215, 215, 215);
            btn.FillPressColor = Color.FromArgb(250, 250, 250);
            btn.FillSelectedColor = Color.FromArgb(250, 250, 250);

            btn.ShowTips = true;
            btn.TipsColor = Color.FromArgb(115, 179, 255);
            btn.TipsForeColor = Color.FromArgb(250, 250, 250);

            btn.Click += UIHeaderBtnClick;
            btn.UseDoubleClick = true;
            btn.DoubleClick += UIHeaderBtnDoubleClick;

            return btn;
        }

        // 单击按钮
        private void UIHeaderBtnClick(object sender, EventArgs e)
        {
            UIHeaderButton btn = (UIHeaderButton)sender;
            int index = int.Parse(btn.Name);
            MouseEventArgs mouse_e = (MouseEventArgs)e;

            if (mouse_e.Button == MouseButtons.Right)
            {
                // shift+右键范围选择
                if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                {
                    if (index > lastSelectIndex)
                    {
                        UpdateSelectRange(lastSelectIndex, index, true);
                    }
                    else
                    {
                        UpdateSelectRange(index, lastSelectIndex, true);
                    }
                }
                // ctrl+右键范围取消
                else if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                {
                    if (index > lastSelectIndex)
                    {
                        UpdateSelectRange(lastSelectIndex, index, false);
                    }
                    else
                    {
                        UpdateSelectRange(index, lastSelectIndex, false);
                    }
                }
                else
                {
                    if (m_SelectedIndexList.Contains(index))
                    {
                        UpdateSelectRange(index, index, false);
                    }
                    else
                    {
                        UpdateSelectRange(index, index, true);
                    }
                }
            }

            OnBtnSelected(index);
            lastSelectIndex = index;
        }

        // 双击按钮
        private void UIHeaderBtnDoubleClick(object sender, EventArgs e)
        {
            UIHeaderButton btn = (UIHeaderButton)sender;
            int index = int.Parse(btn.Name);

            MouseEventArgs mouse_e = (MouseEventArgs)e;
            if (mouse_e.Button == MouseButtons.Right)
            {
                if (m_SelectedIndexList.Contains(index))
                {
                    UpdateSelectRange(index, index, false);
                }
                else
                {
                    UpdateSelectRange(index, index, true);
                }
                return;
            }

            if (m_WallpaperList.Count > 0)
            {
                string path = m_WallpaperList[index].directoryPath;
                // 打开目录
                System.Diagnostics.Process.Start(path);
            }
        }

        // 选择壁纸
        private void OnBtnSelected(int index)
        {
            if (index < 0)
            {
                InitPreviewGroupBox();
            }
            else
            {
                UpdatePreviewGroupBox(m_WallpaperList[index]);
            }
        }

        protected override void AfterUpdateSelectRange()
        {
            for (int i = 0; i < m_ButtonList.Count; i++)
            {
                if (m_SelectedIndexList.Contains(i))
                {
                    m_ButtonList[i].TipsText = "✓";
                }
                else
                {
                    m_ButtonList[i].TipsText = string.Empty;
                }
            }

            UpdateCountLabel();
        }
        #endregion 按钮

        #region 壁纸信息预览框
        /// <summary>
        /// 初始化壁纸预览框
        /// </summary>
        private void InitPreviewGroupBox()
        {
            m_PreviewPictureBox.Image = Tools.LoadImage(SettingManager.currentDirectoryPath + "Images\\image_1.jpg", SettingManager.PreviewContentPictureSize);
            m_PreviewNameLabel.Text = "未选择壁纸";
            m_PreviewContentratingLabel.Text = SettingManager.PreviewContentratingEmptyTip;
            m_previewTypeLabel.Text = SettingManager.PreviewTypeEmptyTip;

            UpdateCountLabel();
        }

        // 更新合计&已选文本
        private void UpdateCountLabel()
        {
            m_PreviewCountLabel.Text = "合计: " + m_WallpaperList.Count + "\n已选 " + m_SelectedIndexList.Count + " 个";
        }

        /// <summary>
        /// 更新壁纸预览框
        /// </summary>
        private void UpdatePreviewGroupBox(Wallpaper wallpaper)
        {
            string imagePath = wallpaper.directoryPath + wallpaper.preview;
            m_PreviewPictureBox.Image = Tools.LoadImage(imagePath, SettingManager.PreviewContentPictureSize);
            m_PreviewNameLabel.Text = wallpaper.title;

            string contentratingText = wallpaper.contentrating;
            if (contentratingText == Contentrating.Everyone)
            {
                contentratingText = "大众级";
            }
            else if (contentratingText == Contentrating.Questionable)
            {
                contentratingText = "13+";
            }
            else if (contentratingText == Contentrating.Mature)
            {
                contentratingText = "18+";
            }
            else
            {
                contentratingText = SettingManager.PreviewContentratingEmptyTip;
            }
            contentratingText = wallpaper.dirSize + " " + contentratingText;
            m_PreviewContentratingLabel.Text = contentratingText;

            string type = wallpaper.type;
            if (string.IsNullOrEmpty(type))
            {
                m_previewTypeLabel.Text = SettingManager.PreviewTypeEmptyTip;
            }
            else
            {
                m_previewTypeLabel.Text = "壁纸类型：" + type;
            }

            System.GC.Collect();
        }
        #endregion 壁纸信息预览框
    }

    /// <summary>
    /// 目录设置框
    /// </summary>
    public class SettingPathPanel
    {
        UIForm m_UIForm;
        /// <summary>
        /// 路径文本框
        /// </summary>
        UITextBox m_TextBox;
        /// <summary>
        /// 对应的滑动列表管理器
        /// </summary>
        WallpaperFlowPanelBase m_WallpaperPanel;
        /// <summary>
        /// 对应的壁纸加载器
        /// </summary>
        public WallpaperLoader wallpaperLoader { get; private set; }

        /// <summary>
        /// 设置路径的方法
        /// </summary>
        Action<string> m_SetPath;
        /// <summary>
        /// 获取路径的方法
        /// </summary>
        Func<string> m_GetPath;

        /// <summary>
        /// 选择目录窗口的标题
        /// </summary>
        string m_SelectDirViewTip;
        /// <summary>
        /// 没有路径时路径文本显示的提示
        /// </summary>
        string m_EmptyPathTip;
        /// <summary>
        /// 等待加载完成提示
        /// </summary>
        string m_WaitLoadTip;

        #region 接口
        public void Init(
            UIForm uiForm,
            UITextBox textBox,
            WallpaperFlowPanelBase wallpaperPanel,
            WallpaperLoader wallpaperLoader,
            Action<string> setPath,
            Func<string> getPath,
            string selectDirViewTip,
            string emptyPathTip,
            string waitLoadTip)
        {
            m_UIForm = uiForm;
            m_TextBox = textBox;
            m_TextBox.DragEnter += DragEnter;
            m_TextBox.DragDrop += DragDrop;
            m_WallpaperPanel = wallpaperPanel;
            this.wallpaperLoader = wallpaperLoader;

            m_SetPath = setPath;
            m_GetPath = getPath;

            m_SelectDirViewTip = selectDirViewTip;
            m_EmptyPathTip = emptyPathTip;
            m_WaitLoadTip = waitLoadTip;

            m_TextBox.Text = m_GetPath();
        }

        /// <summary>
        /// 打开目录选择窗口
        /// </summary>
        public void OpenSelectPathView()
        {
            if (wallpaperLoader.isLoading)
            {
                m_UIForm.ShowWarningTip(m_WaitLoadTip);
                return;
            }

            string path = "";
            if (DirEx.SelectDirEx(m_SelectDirViewTip, ref path))
            {
                UpdatePath(path);
            }
        }

        /// <summary>
        /// 更新路径
        /// </summary>
        public void UpdatePath(string path)
        {
            if (m_GetPath() == path)
            {
                return;
            }

            m_SetPath(path);
            SettingManager.SaveSetting();
            m_TextBox.Text = path;
        }
        #endregion 接口

        // 鼠标拖入
        private void DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        // 拖入壁纸存放目录并松开鼠标
        private void DragDrop(object sender, DragEventArgs e)
        {
            if (wallpaperLoader.isLoading)
            {
                m_UIForm.ShowWarningTip(m_WaitLoadTip);
                return;
            }

            string path = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            path = Tools.GetDirectory(path);

            UpdatePath(path);
        }
    }
}
