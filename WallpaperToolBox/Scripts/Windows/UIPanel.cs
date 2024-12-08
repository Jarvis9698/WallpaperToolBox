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
    /// 壁纸排序类型
    /// </summary>
    public enum WallpaperSortType
    {
        /// <summary>
        /// 无排序
        /// </summary>
        None,
        /// <summary>
        /// 名称
        /// </summary>
        Name,
        /// <summary>
        /// 名称-反向
        /// </summary>
        UnName,
        /// <summary>
        /// 文件大小
        /// </summary>
        Size,
        /// <summary>
        /// 文件大小-反向
        /// </summary>
        UnSize,
        /// <summary>
        /// 更新时间
        /// </summary>
        UpdatedDate,
        /// <summary>
        /// 更新时间-反向
        /// </summary>
        UnUpdatedDate,
    }

    /// <summary>
    /// 壁纸页面详情UI管理类
    /// </summary>
    public class InformationGroupBox
    {
        /// <summary>
        /// 预览图
        /// </summary>
        PictureBox m_pictureBox;
        /// <summary>
        /// 壁纸名称
        /// </summary>
        UILabel m_NameLabel;
        /// <summary>
        /// 年龄分级
        /// </summary>
        UILabel m_ContentratingLabel;
        /// <summary>
        /// 壁纸类型
        /// </summary>
        UILabel m_TypeLabel;

        public InformationGroupBox(
            PictureBox pictureBox, 
            UILabel nameLabel, 
            UILabel contentratingLabel,
            UILabel typeLabel)
        {
            m_pictureBox = pictureBox;
            m_NameLabel = nameLabel;
            m_ContentratingLabel = contentratingLabel;
            m_TypeLabel = typeLabel;
        }

        /// <summary>
        /// 初始化(或重置壁纸信息)
        /// </summary>
        public void Init()
        {
            m_pictureBox.Image = Tools.LoadImage(SettingManager.currentDirectoryPath + "Images\\image_1.jpg", SettingManager.PreviewInformationPictureSize);
            m_NameLabel.Text = "未选择壁纸";
            m_ContentratingLabel.Text = SettingManager.PreviewContentratingEmptyTip;
            m_TypeLabel.Text = SettingManager.PreviewTypeEmptyTip;
        }

        /// <summary>
        /// 更新壁纸信息
        /// </summary>
        public void Update(Wallpaper wallpaper)
        {
            string tempString = null;

            // 更新图片
            tempString = wallpaper.directoryPath + wallpaper.preview;
            m_pictureBox.Image = Tools.LoadImage(tempString, SettingManager.PreviewInformationPictureSize);

            // 壁纸名
            m_NameLabel.Text = wallpaper.title;

            // 年龄分级
            tempString = wallpaper.contentrating;
            if (tempString == Contentrating.Everyone)
            {
                tempString = "大众级";
            }
            else if (tempString == Contentrating.Questionable)
            {
                tempString = "13+";
            }
            else if (tempString == Contentrating.Mature)
            {
                tempString = "18+";
            }
            else
            {
                tempString = SettingManager.PreviewContentratingEmptyTip;
            }
            tempString = wallpaper.GetDirSizeText() + "\n" + tempString;
            m_ContentratingLabel.Text = tempString;

            tempString = wallpaper.type;
            if (string.IsNullOrEmpty(tempString))
            {
                m_TypeLabel.Text = SettingManager.PreviewTypeEmptyTip;
            }
            else
            {
                m_TypeLabel.Text = "壁纸类型：" + tempString;
            }
        }
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

            Update();
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
                SetPath(path);
            }
        }

        /// <summary>
        /// 设置路径
        /// </summary>
        public void SetPath(string path)
        {
            if (m_GetPath() == path)
            {
                return;
            }

            m_SetPath(path);
            SettingManager.SaveSetting();

            Update();
        }

        /// <summary>
        /// 刷新
        /// </summary>
        public void Update()
        {
            if (!Directory.Exists(m_GetPath()))
            {
                m_TextBox.Text = m_EmptyPathTip;
            }
            else
            {
                m_TextBox.Text = m_GetPath();
            }
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

            SetPath(path);
        }
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
        /// 排序类型
        /// </summary>
        protected WallpaperSortType m_WallpaperSortType = WallpaperSortType.None;
        /// <summary>
        /// 壁纸字典
        /// </summary>
        protected Dictionary<string, Wallpaper> m_WallpaperDic = new Dictionary<string, Wallpaper>();
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
        /// 刷新壁纸的CD时间（毫秒）
        /// </summary>
        protected int m_UpdateWallpaperCD = 100;
        /// <summary>
        /// 每次刷新的壁纸数量
        /// </summary>
        protected int m_UpdateWallpaperNum = 100;
        protected static SynchronizationContext synchronizationContext;
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

                if (panelUpdateState != VieweUpdateState.Updated)
                {
                    m_EverySwitch.ReadOnly = true;
                    m_QuestionableSwitch.ReadOnly = true;
                    m_MatureSwitch.ReadOnly = true;
                }
                else
                {
                    // 用新线程是因为在主线程延迟更新会有进度条UIBug
                    Task.Run(() =>
                    {
                        // 延迟一点更新，避免快速切换年龄分级导致显示bug
                        Thread.Sleep(100);
                        m_EverySwitch.ReadOnly = false;
                        m_QuestionableSwitch.ReadOnly = false;
                        m_MatureSwitch.ReadOnly = false;
                    });
                }
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
            synchronizationContext = SynchronizationContext.Current;

            // UI初始化
            m_UIForm = uIForm;
            m_ProgressBar = processBar;
            m_ProgressBar.Hide();

            m_WallpaperLoader = wallpaperLoader;
            m_WallpaperLoader.OnLoaded += () =>
            {
                panelUpdateState = VieweUpdateState.WaitingForUpdateView;
            };

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
            if (panelUpdateState == VieweUpdateState.Updated)
            {
                return;
            }

            panelUpdateState = VieweUpdateState.IsUpdating;

            ClearAllSelectedList();

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
        public virtual void SelectAll(List<int> selectedList, List<Wallpaper> wallpaperList)
        {
            bool isSelectAll = true;
            if (selectedList.Count == wallpaperList.Count)
            {
                isSelectAll = false;
            }

            UpdateSelectedRange(selectedList, 0, wallpaperList.Count - 1, isSelectAll);
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
                SelectAll(m_SelectedIndexList, m_WallpaperList);
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
                Thread.Sleep(50);
            }

            m_WallpaperList = m_WallpaperLoader.GetViewList(
                m_EverySwitch.Active,
                m_QuestionableSwitch.Active,
                m_MatureSwitch.Active,
                m_WallpaperSortType);

            // 后面5%进度用于刷新列表，此时progress不再表示进度，而是滚动列表每次刷新按钮的数量
            int progress = 0;
            while (panelUpdateState != VieweUpdateState.Updated)
            {
                m_LoadProgressWorker.ReportProgress(progress);

                progress += m_UpdateWallpaperNum;
                Thread.Sleep(m_UpdateWallpaperCD);
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
        /// 清空所有勾选下标列表(需要重写)
        /// </summary>
        protected virtual void ClearAllSelectedList()
        {
            m_SelectedIndexList.Clear();
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
        /// 更新m_SelectedIndexList范围内的已选下标
        /// </summary>
        protected void UpdateSelectedIndexListRange(int startIndex, int endIndex, bool isSelected)
        {
            UpdateSelectedRange(m_SelectedIndexList, startIndex, endIndex, isSelected);
        }

        /// <summary>
        /// 更新范围内的已选下标
        /// </summary>
        protected void UpdateSelectedRange(List<int> selectedList, int startIndex, int endIndex, bool isSelected)
        {
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (isSelected && !selectedList.Contains(i))
                {
                    selectedList.Add(i);
                }
                else if (!isSelected)
                {
                    selectedList.Remove(i);
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
        /// 排序下拉框
        /// </summary>
        protected UIComboBox m_sortComboBox;
        /// <summary>
        /// 上一个排序下标
        /// </summary>
        protected int m_LastSortIndex = 0;
        /// <summary>
        /// 壁纸信息管理类
        /// </summary>
        InformationGroupBox m_InformationGroupBox;
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
            InformationGroupBox informationGroupBox,
            UILabel previewCountLabel)
        {
            m_InformationGroupBox = informationGroupBox;
            m_PreviewCountLabel = previewCountLabel;

            m_InformationGroupBox.Init();
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
                        m_ProgressBar.Value = 0;
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
                    "删除的壁纸可以在回收站恢复";
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

        /// <summary>
        /// 设置排序下拉框
        /// </summary>
        public void SetSortComboBox(UIComboBox comboBox)
        {
            m_sortComboBox = comboBox;
            m_sortComboBox.SelectedIndex = m_LastSortIndex;
            m_sortComboBox.SelectedIndexChanged += (sender, e) =>
            {
                if (m_sortComboBox.SelectedIndex == m_LastSortIndex)
                {
                    return;
                }

                if (panelUpdateState == VieweUpdateState.Updated)
                {
                    m_LastSortIndex = comboBox.SelectedIndex;

                    switch (m_LastSortIndex)
                    {
                        case 1:
                            m_WallpaperSortType = WallpaperSortType.Name;
                            break;
                        case 2:
                            m_WallpaperSortType = WallpaperSortType.UnName;
                            break;
                        case 3:
                            m_WallpaperSortType = WallpaperSortType.Size;
                            break;
                        case 4:
                            m_WallpaperSortType = WallpaperSortType.UnSize;
                            break;
                        case 5:
                            m_WallpaperSortType = WallpaperSortType.UpdatedDate;
                            break;
                        case 6:
                            m_WallpaperSortType = WallpaperSortType.UnUpdatedDate;
                            break;
                        default:
                            m_WallpaperSortType = WallpaperSortType.None;
                            break;
                    }
                    panelUpdateState = VieweUpdateState.WaitingForUpdateView;
                    UpdatePanel();
                }
                else
                {
                    m_sortComboBox.SelectedIndex = m_LastSortIndex;
                }
            };
        }
        #endregion 接口

        #region 后台
        // 加载后台同步进度
        protected override void LoadProgressWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int progress = e.ProgressPercentage;

            // 同步壁纸读取器的进度
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
                    m_ProgressBar.Value = 95 + 5 / (m_ButtonList.Count + 1);
                }
            }

            if (m_WallpaperLoader.isLoaded && m_ProgressBar.Value >= 100)
            {
                m_InformationGroupBox.Init();
                Task.Run(() =>
                {
                    Thread.Sleep(100);
                    synchronizationContext.Send(_ => m_ProgressBar.Hide(), null);
                });
                panelUpdateState = VieweUpdateState.Updated;
                AfterUpdateSelectRange();
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

            int progress;
            for (int i = 0; i < selectWallpapers.Count; i++)
            {
                Tools.DeleteDirectory(selectWallpapers[i].directoryPath);
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
                Task.Run(() =>
                {
                    Thread.Sleep(100);
                    synchronizationContext.Send(_ => m_ProgressBar.Hide(), null);
                });
                UpdatePanel();
            }
        }

        // 启动解包后台
        private void UnpackProgressWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<Wallpaper> selectedWallpapers = new List<Wallpaper>();
            m_SelectedIndexList.Sort();
            Wallpaper wallpaper;
            for (int i = 0; i < m_SelectedIndexList.Count; i++)
            {
                wallpaper = m_WallpaperList[m_SelectedIndexList[i]];
                if (wallpaper.type.ToLower() == "scene")
                {
                    selectedWallpapers.Add(wallpaper);
                }
            }

            // 没有可解包的壁纸，关闭进度条
            if (selectedWallpapers.Count <= 0)
            {
                m_UnpackProgressWorker.ReportProgress(100);
                return;
            }

            string unpackPath;
            string cmd;
            int progress;
            for (int i = 0; i < selectedWallpapers.Count; i++)
            {
                unpackPath = SettingManager.setting.unpackPath +
                    selectedWallpapers[i].id + SettingManager.UnpackDirectorySuffix + "\\";
                cmd = "RePKG\\RePKG.exe extract " +
                    selectedWallpapers[i].directoryPath + "scene.pkg" +
                    " -o " +
                    unpackPath;
                Tools.RunCMD(cmd);

                // 复制project.json和预览图文件
                if (Directory.Exists(unpackPath))
                {
                    File.Copy(selectedWallpapers[i].directoryPath + "project.json", unpackPath + "project.json", true);
                    File.Copy(selectedWallpapers[i].directoryPath + selectedWallpapers[i].preview, unpackPath + selectedWallpapers[i].preview, true);
                }

                progress = 100 * (i + 1) / selectedWallpapers.Count;
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
                Task.Run(() =>
                {
                    Thread.Sleep(100);
                    synchronizationContext.Send(_ => m_ProgressBar.Hide(), null);
                });
                panelUpdateState = VieweUpdateState.Updated;
                UpdateSelectedIndexListRange(0, m_WallpaperList.Count - 1, false);
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
            int progress;
            for (int i = 0; i < selectWallpapers.Count; i++)
            {
                backupPath = SettingManager.setting.backupPath + selectWallpapers[i].id + '\\';
                Tools.CopyDirectory(selectWallpapers[i].directoryPath, backupPath);

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
                Task.Run(() =>
                {
                    Thread.Sleep(100);
                    synchronizationContext.Send(_ => m_ProgressBar.Hide(), null);
                });
                panelUpdateState = VieweUpdateState.Updated;
                UpdateSelectedIndexListRange(0, m_WallpaperList.Count - 1, false);
                backupPanel.WaitForReload();
            }
        }
        #endregion 后台

        #region 按钮
        // 更新列表按钮
        private void UpdateUIHeaderButton(int headIndex)
        {
            int btnCount = m_ButtonList.Count;
            int wallpaperCount = m_WallpaperList.Count;

            // 调整按钮数量
            if (btnCount < wallpaperCount)
            {
                for (int i = btnCount; i < wallpaperCount && i - btnCount < m_UpdateWallpaperNum; i++)
                {
                    UIHeaderButton btn = CreateUIHeaderButton(i.ToString());
                    m_ButtonList.Add(btn);
                    m_Panel.Add(btn);
                }
            }
            else if (btnCount > wallpaperCount)
            {
                for (int i = 0; i < m_UpdateWallpaperNum && m_ButtonList.Count > m_WallpaperList.Count; i++)
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

            // 勾选图标设置
            btn.ShowTips = true;
            //btn.TipsColor = Color.FromArgb(115, 179, 255);
            btn.TipsColor = Color.FromArgb(255, 185, 0);
            //btn.TipsForeColor = Color.FromArgb(250, 250, 250);
            btn.TipsForeColor = Color.Black;

            // 按钮事件注册
            btn.Click += UIHeaderBtnClick;
            btn.UseDoubleClick = true;
            btn.DoubleClick += UIHeaderBtnDoubleClick;

            return btn;
        }

        // 单击按钮
        private void UIHeaderBtnClick(object sender, EventArgs e)
        {
            if (panelUpdateState == VieweUpdateState.IsUpdating)
            {
                return;
            }

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
                        UpdateSelectedIndexListRange(lastSelectIndex, index, true);
                    }
                    else
                    {
                        UpdateSelectedIndexListRange(index, lastSelectIndex, true);
                    }
                }
                // ctrl+右键范围取消
                else if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                {
                    if (index > lastSelectIndex)
                    {
                        UpdateSelectedIndexListRange(lastSelectIndex, index, false);
                    }
                    else
                    {
                        UpdateSelectedIndexListRange(index, lastSelectIndex, false);
                    }
                }
                else
                {
                    if (m_SelectedIndexList.Contains(index))
                    {
                        UpdateSelectedIndexListRange(index, index, false);
                    }
                    else
                    {
                        UpdateSelectedIndexListRange(index, index, true);
                    }
                }
            }

            OnBtnSelected(index);
            lastSelectIndex = index;
        }

        // 双击按钮
        private void UIHeaderBtnDoubleClick(object sender, EventArgs e)
        {
            if (panelUpdateState == VieweUpdateState.IsUpdating)
            {
                return;
            }

            UIHeaderButton btn = (UIHeaderButton)sender;
            int index = int.Parse(btn.Name);

            MouseEventArgs mouse_e = (MouseEventArgs)e;
            if (mouse_e.Button == MouseButtons.Right)
            {
                if (m_SelectedIndexList.Contains(index))
                {
                    UpdateSelectedIndexListRange(index, index, false);
                }
                else
                {
                    UpdateSelectedIndexListRange(index, index, true);
                }
                return;
            }

            if (m_WallpaperList.Count > 0)
            {
                string path = m_WallpaperList[index].directoryPath;
                if (Directory.Exists(path))
                {
                    // 打开目录
                    System.Diagnostics.Process.Start(path);
                }
                else
                {
                    m_UIForm.ShowErrorTip(SettingManager.FileNullErrorTip);
                }
            }
        }

        // 选择壁纸
        private void OnBtnSelected(int index)
        {
            if (index < 0)
            {
                m_InformationGroupBox.Init();
                UpdateCountLabel();
            }
            else
            {
                m_InformationGroupBox.Update(m_WallpaperList[index]);
            }
        }

        protected override void AfterUpdateSelectRange()
        {
            for (int i = 0; i < m_ButtonList.Count; i++)
            {
                if (m_SelectedIndexList.Contains(i))
                {
                    m_ButtonList[i].TipsText = "✔";
                }
                else
                {
                    m_ButtonList[i].TipsText = string.Empty;
                }
            }

            UpdateCountLabel();
        }
        #endregion 按钮

        // 更新合计&已选文本
        private void UpdateCountLabel()
        {
            m_PreviewCountLabel.Text = "合计: " + m_WallpaperList.Count + "\n已选 " + m_SelectedIndexList.Count + " 个";
        }
    }

    /// <summary>
    /// 壁纸预览列表
    /// <para>同时控制新增、修改、删除的变更管理列表</para>
    /// </summary>
    public class WallpaperDataGridView : WallpaperFlowPanelBase
    {
        UIDataGridView m_NewGridView;
        UIDataGridViewFooter m_NewGridViewFooter;
        InformationGroupBox m_newInformationGroupBox;
        Dictionary<string, Wallpaper> m_NewWallpaperDic = new Dictionary<string, Wallpaper>();
        List<string> m_NewSelectedIDList = new List<string>();

        UIDataGridView m_ChangedGridView;
        UIDataGridViewFooter m_ChangedGridViewFooter;
        InformationGroupBox m_changedInformationGroupBox;
        Dictionary<string, Wallpaper> m_ChangedWallpaperDic = new Dictionary<string, Wallpaper>();
        List<string> m_ChangedSelectedIDList = new List<string>();

        UIDataGridView m_DelGridView;
        UIDataGridViewFooter m_DelGridViewFooter;
        InformationGroupBox m_delInformationGroupBox;
        Dictionary<string, Wallpaper> m_DelWallpaperDic = new Dictionary<string, Wallpaper>();
        List<string> m_DelSelectedIDList = new List<string>();

        WallpaperFlowLayoutPanel m_StorePanel;
        WallpaperFlowLayoutPanel m_LocalBackupPanel;
        WallpaperFlowLayoutPanel m_BackupPanel;

        /// <summary>
        /// 壁纸同步进度管理后台
        /// </summary>
        BackgroundWorker m_SyncProgressWorker = new BackgroundWorker();
        /// <summary>
        /// 壁纸撤销更改进度管理后台
        /// </summary>
        BackgroundWorker m_RollbackProgressWorker = new BackgroundWorker();

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
        public void InitAllDataGridViews(UIDataGridView newGridView,
            UIDataGridViewFooter newFooter,
            InformationGroupBox newInformationGroupBox,
            UIDataGridView changedGridView,
            UIDataGridViewFooter changedFooter,
            InformationGroupBox changedInformationGroupBox,
            UIDataGridView delGridView,
            UIDataGridViewFooter delFooter,
            InformationGroupBox delInformationGroupBox,
            WallpaperFlowLayoutPanel storePanel,
            WallpaperFlowLayoutPanel localBackupPanel,
            WallpaperFlowLayoutPanel backupPanel)
        {
            DataGridViewCellEventHandler onClick = new DataGridViewCellEventHandler(DataGridViewCellClick);
            DataGridViewCellEventHandler onDoubleClick = new DataGridViewCellEventHandler(DataGridViewCellDoubleClick);

            m_NewGridView = newGridView;
            m_NewGridViewFooter = newFooter;
            m_newInformationGroupBox = newInformationGroupBox;
            m_NewGridView.CellClick += onClick;
            m_NewGridView.CellDoubleClick += onDoubleClick;


            m_ChangedGridView = changedGridView;
            m_ChangedGridViewFooter = changedFooter;
            m_changedInformationGroupBox = changedInformationGroupBox;
            m_ChangedGridView.CellClick += onClick;
            m_ChangedGridView.CellDoubleClick += onDoubleClick;

            m_DelGridView = delGridView;
            m_DelGridViewFooter = delFooter;
            m_delInformationGroupBox = delInformationGroupBox;
            m_DelGridView.CellClick += onClick;
            m_DelGridView.CellDoubleClick += onDoubleClick;

            m_StorePanel = storePanel;
            m_LocalBackupPanel = localBackupPanel;
            m_BackupPanel = backupPanel;

            InitInformationGroupBoxs();

            InitDataGridView(m_NewGridView, m_NewGridViewFooter);
            InitDataGridView(m_ChangedGridView, m_ChangedGridViewFooter);
            InitDataGridView(m_DelGridView, m_DelGridViewFooter);

            m_ProgressBar.Hide();
            panelUpdateState = VieweUpdateState.WaitingForReload;

            isInited = true;
        }

        /// <summary>
        /// 初始化所有壁纸信息UI
        /// </summary>
        public void InitInformationGroupBoxs()
        {
            m_newInformationGroupBox.Init();
            m_changedInformationGroupBox.Init();
            m_delInformationGroupBox.Init();
        }

        /// <summary>
        /// 设置全选按钮(变更页面专用)
        /// </summary>
        public void SetSelectAllBtns(UIButton newBtn, UIButton changedBtn, UIButton delBtn)
        {
            newBtn.Click += (sender, e) =>
            {
                if (panelUpdateState != VieweUpdateState.Updated)
                {
                    return;
                }

                bool isSelectedAll = false;
                if (m_NewSelectedIDList.Count != m_NewWallpaperDic.Count)
                {
                    m_NewSelectedIDList = m_NewWallpaperDic.Keys.ToList();
                    isSelectedAll = true;
                }
                else
                {
                    m_NewSelectedIDList.Clear();
                }
                UpdateGridViewFooters();

                UpdateCheckBoxCell(m_NewGridView, isSelectedAll);
            };

            changedBtn.Click += (sender, e) =>
            {
                if (panelUpdateState != VieweUpdateState.Updated)
                {
                    return;
                }

                bool isSelectedAll = false;
                if (m_ChangedSelectedIDList.Count != m_ChangedWallpaperDic.Count)
                {
                    m_ChangedSelectedIDList = m_ChangedWallpaperDic.Keys.ToList();
                    isSelectedAll = true;
                }
                else
                {
                    m_ChangedSelectedIDList.Clear();
                }
                UpdateGridViewFooters();

                UpdateCheckBoxCell(m_ChangedGridView, isSelectedAll);
            };

            delBtn.Click += (sender, e) =>
            {
                if (panelUpdateState != VieweUpdateState.Updated)
                {
                    return;
                }

                bool isSelectedAll = false;
                if (m_DelSelectedIDList.Count != m_DelWallpaperDic.Count)
                {
                    m_DelSelectedIDList = m_DelWallpaperDic.Keys.ToList();
                    isSelectedAll = true;
                }
                else
                {
                    m_DelSelectedIDList.Clear();
                }
                UpdateGridViewFooters();

                UpdateCheckBoxCell(m_DelGridView, isSelectedAll);
            };
        }

        /// <summary>
        /// 设置UI按钮
        /// </summary>
        public void SetUIBtn(UIButton syncBtn, UIButton rollbackBtn)
        {
            // 同步按钮
            syncBtn.Click += (sender, e) =>
            {
                if (panelUpdateState != VieweUpdateState.Updated)
                {
                    return;
                }

                int selectedNum = m_NewSelectedIDList.Count
                    + m_ChangedSelectedIDList.Count
                    + m_DelSelectedIDList.Count;

                if (selectedNum == 0)
                {
                    m_UIForm.ShowWarningTip(SettingManager.SyncSelectEmptyTip);
                    return;
                }

                if (!Directory.Exists(SettingManager.setting.localBackupPath))
                {
                    m_UIForm.ShowWarningDialog(SettingManager.PreviewLocalBackupPathEmptyTip);
                    return;
                }

                string tip = "将同步 " + selectedNum + " 个壁纸，具体操作如下：\n" +
                    "从订阅壁纸目录和官方备份目录复制 " + m_NewSelectedIDList.Count + " 个壁纸到本地备份目录\n" +
                    "从订阅壁纸目录和官方备份目录复制并替换 " + m_ChangedSelectedIDList.Count + " 个壁纸到本地备份目录\n" +
                    "从本地备份目录删除 " + m_DelSelectedIDList.Count + " 个壁纸\n" +
                    "删除的壁纸可以在回收站找回，被替换的壁纸不可恢复！";
                if (Tools.ShowAskDialog(tip, m_UIForm))
                {
                    panelUpdateState = VieweUpdateState.IsUpdating;
                    if (!m_SyncProgressWorker.IsBusy)
                    {
                        m_SyncProgressWorker.RunWorkerAsync();
                        m_ProgressBar.Show();
                    }
                }
            };

            // 同步后台初始化
            m_SyncProgressWorker.WorkerReportsProgress = true;
            m_SyncProgressWorker.DoWork += SyncProgressWorker_DoWork;
            m_SyncProgressWorker.ProgressChanged += SyncProgressWorker_ProgressChanged;

            // 回滚按钮
            rollbackBtn.Click += (sender, e) =>
            {
                if (panelUpdateState != VieweUpdateState.Updated)
                {
                    return;
                }

                int selectedNum = m_NewSelectedIDList.Count
                    + m_ChangedSelectedIDList.Count
                    + m_DelSelectedIDList.Count;

                if (selectedNum == 0)
                {
                    m_UIForm.ShowWarningTip(SettingManager.RollbackSelectEmptyTip);
                    return;
                }

                if (!Directory.Exists(SettingManager.setting.localBackupPath))
                {
                    m_UIForm.ShowWarningDialog(SettingManager.PreviewLocalBackupPathEmptyTip);
                    return;
                }

                string tip = "将撤销更改 " + selectedNum + " 个壁纸，具体操作如下：\n" +
                    "从订阅壁纸目录和官方备份目录删除 " + m_NewSelectedIDList.Count + " 个壁纸\n" +
                    "从本地备份目录复制并替换 " + m_ChangedSelectedIDList.Count + " 个壁纸到订阅壁纸目录和官方备份目录\n" +
                    "从本地备份目录复制 " + m_DelSelectedIDList.Count + " 个壁纸到官方备份目录\n" +
                    "被替换的订阅目录壁纸若未取消订阅，则会被steam恢复！";
                if (Tools.ShowAskWarningAskDialog(tip, m_UIForm))
                {
                    panelUpdateState = VieweUpdateState.IsUpdating;
                    if (!m_RollbackProgressWorker.IsBusy)
                    {
                        m_RollbackProgressWorker.RunWorkerAsync();
                        m_ProgressBar.Show();
                    }
                }
            };

            // 撤销后台初始化
            m_RollbackProgressWorker.WorkerReportsProgress = true;
            m_RollbackProgressWorker.DoWork += RollbackProgressWorker_DoWork;
            m_RollbackProgressWorker.ProgressChanged += RollbackProgressWorker_ProgressChanged;
        }

        public override void UpdatePanel()
        {
            base.UpdatePanel();

            // 避免切换页面后清空列表
            if (panelUpdateState == VieweUpdateState.Updated)
            {
                return;
            }

            InitDataGridView(m_NewGridView, m_NewGridViewFooter);
            InitDataGridView(m_ChangedGridView, m_ChangedGridViewFooter);
            InitDataGridView(m_DelGridView, m_DelGridViewFooter);
        }
        #endregion 接口

        /// <summary>
        /// 初始化列表UI
        /// </summary>
        private void InitDataGridView(UIDataGridView uiView, UIDataGridViewFooter footer)
        {
            // 这里只初始化了新增列表，还未完善
            uiView.Rows.Clear();
            uiView.Rows[0].Height = SettingManager.PreviewRowsHeight;
            // ID
            uiView.Rows[0].Cells[1].Value = "114514";
            // 名称
            uiView.Rows[0].Cells[2].Value = SettingManager.DataGridViewEmptyTip;
            // 最后更新日期
            uiView.Rows[0].Cells[3].Value = "/";
            // 预览图
            Bitmap bitmap = Tools.LoadImage(SettingManager.currentDirectoryPath + "Images\\image_1.jpg", SettingManager.PreviewImageSize);
            uiView.Rows[0].Cells[4].Value = bitmap;

            footer.Clear();
        }

        protected override void ClearAllSelectedList()
        {
            base.ClearAllSelectedList();
            m_NewSelectedIDList.Clear();
            m_ChangedSelectedIDList.Clear();
            m_DelSelectedIDList.Clear();
        }

        /// <summary>
        /// 更具已选下标列表更新列表UI的勾选状态
        /// </summary>
        private void UpdateCheckBoxCell(UIDataGridView uiView, bool isSelectedAll)
        {
            DataGridViewCheckBoxCell checkBox;
            for (int i = 0; i < uiView.Rows.Count; i++)
            {
                checkBox = (DataGridViewCheckBoxCell)uiView.Rows[i].Cells[0];
                checkBox.Value = isSelectedAll;
                checkBox.EditingCellFormattedValue = checkBox.Value;
            }
        }

        /// <summary>
        /// 根据已选ID获取被勾选的壁纸列表
        /// </summary>
        private List<Wallpaper> GetSelectedWallpapers(Dictionary<string, Wallpaper> wallpaperDic, List<string> idList)
        {
            List<Wallpaper> result = new List<Wallpaper>();

            idList.Sort();
            for (int i = 0; i < idList.Count; i++)
            {
                result.Add(wallpaperDic[idList[i]]);
            }

            return result;
        }

        #region 后台
        /// <summary>
        /// 加载列表的后台
        /// </summary>
        protected override void LoadProgressWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(100);

            // 前面95%进度用于读取壁纸数据以及对比
            while (m_WallpaperLoader.isLoading)
            {
                m_LoadProgressWorker.ReportProgress((int)(m_WallpaperLoader.loadingProgress * 0.95f));
                Thread.Sleep(10);
            }

            m_NewWallpaperDic = ((WallpaperChanger)m_WallpaperLoader).GetNewViewDic(
                m_EverySwitch.Active,
                m_QuestionableSwitch.Active,
                m_MatureSwitch.Active);
            m_ChangedWallpaperDic = ((WallpaperChanger)m_WallpaperLoader).GetChangedViewDic(
                m_EverySwitch.Active,
                m_QuestionableSwitch.Active,
                m_MatureSwitch.Active);
            m_DelWallpaperDic = ((WallpaperChanger)m_WallpaperLoader).GetDelViewDic(
                m_EverySwitch.Active,
                m_QuestionableSwitch.Active,
                m_MatureSwitch.Active);

            // 后面5%进度用于刷新列表，此时progress不再表示进度，而是滚动列表每次刷新按钮的数量
            int progress = 0;
            while (panelUpdateState != VieweUpdateState.Updated)
            {
                m_LoadProgressWorker.ReportProgress(progress);

                progress += 100;
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// 加载后台同步进度
        /// </summary>
        protected override void LoadProgressWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int progress = e.ProgressPercentage;
            // 同步壁纸变更比较器的进度
            if (m_WallpaperLoader.isLoading)
            {
                m_ProgressBar.Value = progress;
            }
            // 刷新UI的进度
            else
            {
                List<Wallpaper> wallpaperList;
                UIDataGridView wallpaperGridView;
                float viewUpdatedProgress = 0;
                int maxNum = 100;
                // 刷新三个列表
                for (int i = 0; i < 3; i++)
                {
                    if (i == 0)
                    {
                        wallpaperList = m_NewWallpaperDic.Values.ToList();
                        wallpaperGridView = m_NewGridView;
                    }
                    else if (i == 1)
                    {
                        wallpaperList = m_ChangedWallpaperDic.Values.ToList();
                        wallpaperGridView = m_ChangedGridView;
                    }
                    else
                    {
                        wallpaperList = m_DelWallpaperDic.Values.ToList();
                        wallpaperGridView = m_DelGridView;
                    }

                    // 更新列表
                    maxNum = RefreshUIGridViewCell(wallpaperGridView, wallpaperList, progress, maxNum);
                    if (maxNum > 0)
                    {
                        progress -= wallpaperList.Count;
                        viewUpdatedProgress++;
                        if (maxNum < 100)
                        {
                            progress = 0;
                        }
                    }
                }
                m_ProgressBar.Value = (int)(95 + 5 * viewUpdatedProgress / 3);
            }

            // 加载完成
            if (m_WallpaperLoader.isLoaded && m_ProgressBar.Value >= 100)
            {
                InitInformationGroupBoxs();
                UpdateGridViewFooters();
                Task.Run(() =>
                {
                    Thread.Sleep(100);
                    synchronizationContext.Send(_ => m_ProgressBar.Hide(), null);
                });
                panelUpdateState = VieweUpdateState.Updated;
            }

        }

        /// <summary>
        /// 刷新列表信息
        /// </summary>
        private int RefreshUIGridViewCell(UIDataGridView gridView, List<Wallpaper> wallpaperList, int index, int maxNum)
        {
            int rowCount = gridView.RowCount;
            int wallpaperCount = wallpaperList.Count;
            int n = 0;// 已处理的数量
            int i = 0;
            // 增加行数
            if (rowCount < wallpaperCount)
            {
                for (i = rowCount; i < wallpaperCount && i - rowCount < maxNum; i++)
                {
                    gridView.Rows.Add(1);
                }
                if ((i - rowCount) >= n)
                {
                    n = i - rowCount;
                }
            }

            // 逐行设置信息
            for (i = index; i < wallpaperCount && i < gridView.RowCount; i++)
            {
                gridView.Rows[i].Height = SettingManager.PreviewRowsHeight;
                gridView.Rows[i].Cells[1].Value = wallpaperList[i].id;
                gridView.Rows[i].Cells[2].Value = wallpaperList[i].title;
                gridView.Rows[i].Cells[3].Value = wallpaperList[i].lastWriteTime;
                gridView.Rows[i].Cells[4].Value = wallpaperList[i].previewImage;
            }

            if ((i - index) >= n)
            {
                n = i - index;
            }

            return maxNum - n;
        }

        /// <summary>
        /// 同步壁纸的后台
        /// </summary>
        private void SyncProgressWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var syncNewList = GetSelectedWallpapers(m_NewWallpaperDic, m_NewSelectedIDList);
            var syncChangedList = GetSelectedWallpapers(m_ChangedWallpaperDic, m_ChangedSelectedIDList);
            var syncDelList = GetSelectedWallpapers(m_DelWallpaperDic, m_DelSelectedIDList);

            string pathTo;
            float maxProgress = (syncNewList.Count + syncChangedList.Count + syncDelList.Count) * 1.1f;
            float progress = 0;
            // 复制新壁纸
            for (int i = 0; i < syncNewList.Count; i++)
            {
                pathTo = SettingManager.setting.localBackupPath + syncNewList[i].id + '\\';
                Tools.CopyDirectory(syncNewList[i].directoryPath, pathTo);

                progress = (i + 1) / maxProgress * 100;
                m_SyncProgressWorker.ReportProgress((int)progress);
                Thread.Sleep(100);
            }

            // 复制并替换变更壁纸
            for (int i = 0; i < syncChangedList.Count; i++)
            {
                pathTo = SettingManager.setting.localBackupPath + syncChangedList[i].id + '\\';
                Tools.CopyDirectory(syncChangedList[i].directoryPath, pathTo);

                progress = (i + 1 + syncNewList.Count) / maxProgress * 100;
                m_SyncProgressWorker.ReportProgress((int)progress);
                Thread.Sleep(100);
            }

            // 删除旧壁纸
            for (int i = 0; i < syncDelList.Count; i++)
            {
                Tools.DeleteDirectory(syncDelList[i].directoryPath);

                progress = (i + 1 + syncNewList.Count + syncChangedList.Count) / maxProgress * 100;
                m_SyncProgressWorker.ReportProgress((int)progress);
                Thread.Sleep(100);
            }

            Thread.Sleep(100);
            // 进度100只调用一次，所以前面进度达不到100
            m_SyncProgressWorker.ReportProgress(100);
        }

        /// <summary>
        /// 同步后台进度管理
        /// </summary>
        private void SyncProgressWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int progress = e.ProgressPercentage;
            m_ProgressBar.Value = progress;

            if (progress >= 100)
            {
                Task.Run(() =>
                {
                    Thread.Sleep(100);
                    synchronizationContext.Send(_ =>
                    {
                        m_ProgressBar.Hide();
                        WaitForReload();
                        UpdatePanel();
                    }, null);
                });
            }
        }

        /// <summary>
        /// 撤销更改壁纸的后台
        /// </summary>
        private void RollbackProgressWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var rollbackNewList = GetSelectedWallpapers(m_NewWallpaperDic, m_NewSelectedIDList);
            var rollbackChangedList = GetSelectedWallpapers(m_ChangedWallpaperDic, m_ChangedSelectedIDList);
            var rollbackDelList = GetSelectedWallpapers(m_DelWallpaperDic, m_DelSelectedIDList);

            string path;
            float maxProgress = (rollbackNewList.Count + rollbackChangedList.Count + rollbackDelList.Count) * 1.1f;
            float progress;
            // 删除新壁纸（已订阅的壁纸删了steam还会重下）
            for (int i = 0; i < rollbackNewList.Count; i++)
            {
                Tools.DeleteDirectory(rollbackNewList[i].directoryPath);

                progress = (i + 1) / maxProgress * 100;
                m_RollbackProgressWorker.ReportProgress((int)progress);
                Thread.Sleep(100);
            }

            // 复制并替换新壁纸
            for (int i = 0; i < rollbackChangedList.Count; i++)
            {
                path = SettingManager.setting.localBackupPath + rollbackChangedList[i].id + '\\';
                Tools.CopyDirectory(path, rollbackChangedList[i].directoryPath);

                progress = (i + 1 + rollbackNewList.Count) / maxProgress * 100;
                m_RollbackProgressWorker.ReportProgress((int)progress);
                Thread.Sleep(100);
            }

            // 复制旧壁纸到官方备份目录
            for (int i = 0; i < rollbackDelList.Count; i++)
            {
                path = SettingManager.setting.backupPath + rollbackDelList[i].id + '\\';
                Tools.CopyDirectory(rollbackDelList[i].directoryPath, path);

                progress = (i + 1 + rollbackNewList.Count + rollbackChangedList.Count) / maxProgress * 100;
                m_RollbackProgressWorker.ReportProgress((int)progress);
                Thread.Sleep(100);
            }

            Thread.Sleep(100);
            // 进度100只调用一次，所以前面进度达不到100
            m_RollbackProgressWorker.ReportProgress(100);
        }

        /// <summary>
        /// 撤销更改壁纸后台进度管理
        /// </summary>
        private void RollbackProgressWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int progress = e.ProgressPercentage;
            m_ProgressBar.Value = progress;

            if (progress >= 100)
            {
                Task.Run(() =>
                {
                    Thread.Sleep(100);
                    synchronizationContext.Send(_ =>
                    {
                        m_ProgressBar.Hide();
                        WaitForReload();
                        UpdatePanel();
                    }, null);
                });
            }
        }
        #endregion 后台

        #region UI事件
        /// <summary>
        /// 左键单击列表
        /// </summary>
        private void DataGridViewCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }
            if (panelUpdateState <= VieweUpdateState.IsUpdating)
            {
                return;
            }

            List<string> selectedList;
            Dictionary<string, Wallpaper> wallpaperDic;
            UIDataGridView gridView = ((UIDataGridView)sender);
            InformationGroupBox informationGroupBox;
            // 区分变更类型
            if (gridView.Name == "newGridView")
            {
                selectedList = m_NewSelectedIDList;
                wallpaperDic = m_NewWallpaperDic;
                informationGroupBox = m_newInformationGroupBox;
            }
            else if (gridView.Name == "changedGridView")
            {
                selectedList = m_ChangedSelectedIDList;
                wallpaperDic = m_ChangedWallpaperDic;
                informationGroupBox = m_changedInformationGroupBox;
            }
            else
            {
                selectedList = m_DelSelectedIDList;
                wallpaperDic = m_DelWallpaperDic;
                informationGroupBox = m_delInformationGroupBox;
            }

            if (wallpaperDic.Count > 0)
            {
                string wallpaperID = gridView.Rows[e.RowIndex].Cells[1].Value.ToString();
                if (!wallpaperDic.ContainsKey(wallpaperID))
                {
                    return;
                }
                // 刷新壁纸信息UI
                informationGroupBox.Update(wallpaperDic[wallpaperID]);

                // 勾选框
                if (e.ColumnIndex == 0)
                {
                    bool isSelected = !(bool)gridView.Rows[e.RowIndex].Cells[0].EditedFormattedValue;
                    DataGridViewCheckBoxCell chekBox = (DataGridViewCheckBoxCell)gridView.Rows[e.RowIndex].Cells[0];
                    chekBox.Value = isSelected;
                    chekBox.EditingCellFormattedValue = isSelected;

                    // 更新下标
                    if (isSelected && !selectedList.Contains(wallpaperID))
                    {
                        selectedList.Add(wallpaperID);
                    }
                    else if (!isSelected)
                    {
                        selectedList.Remove(wallpaperID);
                    }

                    UpdateGridViewFooters();
                }
            }
        }

        /// <summary>
        /// 左键双击列表
        /// </summary>
        private void DataGridViewCellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex == 0)
            {
                return;
            }
            if (panelUpdateState <= VieweUpdateState.IsUpdating)
            {
                return;
            }

            Dictionary<string, Wallpaper> wallpaperDic;
            UIDataGridView gridView = ((UIDataGridView)sender);
            // 区分变更类型
            if (gridView.Name == "newGridView")
            {
                wallpaperDic = m_NewWallpaperDic;
            }
            else if (gridView.Name == "changedGridView")
            {
                wallpaperDic = m_ChangedWallpaperDic;
            }
            else
            {
                wallpaperDic = m_DelWallpaperDic;
            }

            if (wallpaperDic.Count > 0)
            {
                string wallpaperID = ((UIDataGridView)sender).Rows[e.RowIndex].Cells[1].Value.ToString();
                string path = wallpaperDic[wallpaperID].directoryPath;
                if (Directory.Exists(path))
                {
                    // 打开目录
                    System.Diagnostics.Process.Start(path);
                }
                else
                {
                    m_UIForm.ShowErrorTip(SettingManager.FileNullErrorTip);
                }
            }
        }

        private void UpdateGridViewFooters()
        {
            m_NewGridViewFooter["newGridViewID"] = "合计：" + m_NewWallpaperDic.Count;
            m_NewGridViewFooter["newGridViewName"] = "(已选：" + m_NewSelectedIDList.Count + ")";

            m_ChangedGridViewFooter["changedGridViewID"] = "合计：" + m_ChangedWallpaperDic.Count;
            m_ChangedGridViewFooter["changedGridViewName"] = "(已选：" + m_ChangedSelectedIDList.Count + ")";

            m_DelGridViewFooter["DelGridViewID"] = "合计：" + m_DelWallpaperDic.Count;
            m_DelGridViewFooter["DelGridViewName"] = "(已选：" + m_DelSelectedIDList.Count + ")";
        }
        #endregion UI事件
    }
}
