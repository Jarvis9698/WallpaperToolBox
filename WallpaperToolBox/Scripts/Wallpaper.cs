using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WallpaperToolBox
{
    /// <summary>
    /// 年龄分级
    /// </summary>
    public class Contentrating
    {
        /// <summary>
        /// 全年龄
        /// </summary>
        public const string Everyone = "Everyone";
        /// <summary>
        /// 13+
        /// </summary>
        public const string Questionable = "Questionable";
        /// <summary>
        /// 18+
        /// </summary>
        public const string Mature = "Mature";
    }

    /// <summary>
    /// wallpaper壁纸数据类
    /// <para>属性不能改名，与壁纸project.json内变量名对应</para>
    /// </summary>
    public class Wallpaper
    {
        /// <summary>
        /// 目录ID
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// 壁纸名
        /// </summary>
        public string title { get; set; }
        /// <summary>
        /// 年龄分级
        /// </summary>
        public string contentrating { get; set; }
        /// <summary>
        /// 壁纸类型
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// 预览图的文件名
        /// </summary>
        public string preview { get; set; }
        /// <summary>
        /// 壁纸目录路径(末尾带\)
        /// </summary>
        public string directoryPath { get; set; }
        /// <summary>
        /// 目录大小
        /// </summary>
        public float dirSize { get; set; }
        /// <summary>
        /// 预览图
        /// </summary>
        public Bitmap previewImage { get; set; }
        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime lastWriteTime { get; set; }

        /// <summary>
        /// 获取目录大小文本（自带单位，保留小数点后一位）
        /// </summary>
        public string GetDirSizeText()
        {
            float size = dirSize;
            string result;
            int i = 0;
            for (i = 0; size >= 1000; i++)
            {
                size /= 1024;
            }
            result = size.ToString("#0.0");

            string unit = "KB";
            if (i == 2)
            {
                unit = "MB";
            }
            else if (i == 3)
            {
                unit = "GB";
            }
            result += unit;

            return result;
        }
    }

    public class WallpaperLoader
    {
        /// <summary>
        /// 加载目录
        /// </summary>
        public string loadPath;
        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool isLoading { get; protected set; } = false;
        /// <summary>
        /// 是否加载完毕
        /// </summary>
        public bool isLoaded { get; protected set; } = false;
        /// <summary>
        /// 加载进度(0-100)
        /// </summary>
        public int loadingProgress { get; protected set; } = 0;
        /// <summary>
        /// 壁纸数据列表
        /// </summary>
        public List<Wallpaper> wallpaperList { get; protected set; } = new List<Wallpaper>();
        /// <summary>
        /// 加载开始前的回调
        /// </summary>
        public event Action OnBeforeLoading;
        /// <summary>
        /// 加载完成后的回调
        /// </summary>
        public event Action OnLoaded;

        protected Task m_WallpaperLoadTask;

        public virtual void Init(string path)
        {
            if (loadPath != path)
            {
                loadPath = path;
                WaitForReload();
            }
        }

        /// <summary>
        /// 准备重新加载
        /// </summary>
        public virtual void WaitForReload()
        {
            if (isLoading)
            {
                return;
            }

            isLoaded = false;
            loadingProgress = 0;
        }

        /// <summary>
        /// 读取壁纸信息
        /// </summary>
        public void Load()
        {
            if (isLoaded || isLoading)
            {
                return;
            }
            if (m_WallpaperLoadTask != null && !m_WallpaperLoadTask.IsCompleted)
            {
                return;
            }

            wallpaperList.Clear();
            BeforeLoading();
            if (string.IsNullOrEmpty(loadPath) || !Directory.Exists(loadPath))
            {
                isLoaded = true;
                return;
            }

            isLoading = true;
            loadingProgress = 0;
            m_WallpaperLoadTask = new Task(LoadTask);
            m_WallpaperLoadTask.Start();
        }

        /// <summary>
        /// 启动加载线程之前执行
        /// </summary>
        protected virtual void BeforeLoading()
        {
            OnBeforeLoading?.Invoke();
        }

        protected virtual void LoadTask()
        {
            DirectoryInfo[] dirInfo = new DirectoryInfo(loadPath).GetDirectories();
            int maxProgress = dirInfo.Length;
            for (int i = 0; i < maxProgress; i++)
            {
                Wallpaper wallpaper = Tools.LoadWallpaperValue(dirInfo[i]);
                if (wallpaper != null)
                {
                    wallpaperList.Add(wallpaper);
                }

                loadingProgress = 100 * (i + 1) / maxProgress;
            }

            loadingProgress = 100;
            isLoading = false;
            isLoaded = true;
            System.GC.Collect();
            OnLoaded?.Invoke();
        }

        /// <summary>
        /// 移除壁纸（不删除文件）
        /// </summary>
        public virtual void Remove(List<Wallpaper> removeList)
        {
            for (int i = 0; i < removeList.Count; i++)
            {
                wallpaperList.Remove(removeList[i]);
            }
            OnBeforeLoading?.Invoke();
        }

        /// <summary>
        /// 根据过滤选项获取用于展示的壁纸数据
        /// </summary>
        public List<Wallpaper> GetViewList(bool isEveryone, bool isQuestionable, bool isMature, WallpaperSortType sortType = WallpaperSortType.None)
        {
            List<Wallpaper> result = new List<Wallpaper>();
            string contentrating;

            for (int i = 0; i < wallpaperList.Count; i++)
            {
                contentrating = wallpaperList[i].contentrating;

                if (isQuestionable && contentrating == Contentrating.Questionable)
                {
                    result.Add(wallpaperList[i]);
                }
                else if (isMature && contentrating == Contentrating.Mature)
                {
                    result.Add(wallpaperList[i]);
                }
                else if (isEveryone && (contentrating != Contentrating.Questionable && contentrating != Contentrating.Mature))
                {
                    result.Add(wallpaperList[i]);
                }
            }

            #region 排序
            if (sortType == WallpaperSortType.Name)
            {
                result.Sort((x, y) =>
                {
                    return x.title.CompareTo(y.title);
                });
            }
            else if (sortType == WallpaperSortType.UnName)
            {
                result.Sort((x, y) =>
                {
                    return x.title.CompareTo(y.title);
                });
                result.Reverse();
            }
            else if (sortType == WallpaperSortType.Size)
            {
                result.Sort((x, y) =>
                {
                    return x.dirSize.CompareTo(y.dirSize);
                });
                result.Reverse();
            }
            else if (sortType == WallpaperSortType.UnSize)
            {
                result.Sort((x, y) =>
                {
                    return x.dirSize.CompareTo(y.dirSize);
                });
            }
            else if (sortType == WallpaperSortType.UpdatedDate)
            {
                result.Sort((x, y) =>
                {
                    return x.lastWriteTime.CompareTo(y.lastWriteTime);
                });
                result.Reverse();
            }
            else if (sortType == WallpaperSortType.UnUpdatedDate)
            {
                result.Sort((x, y) =>
                {
                    return x.lastWriteTime.CompareTo(y.lastWriteTime);
                });
            }
            #endregion 排序

            return result;
        }
    }

    public class WallpaperChanger : WallpaperLoader
    {
        /// <summary>
        /// 新增壁纸总列表
        /// </summary>
        public List<Wallpaper> newWallpaperList = new List<Wallpaper>();
        /// <summary>
        /// 变更壁纸总列表
        /// </summary>
        public List<Wallpaper> changedWallpaperList = new List<Wallpaper>();
        /// <summary>
        /// 已删除壁纸总列表
        /// </summary>
        public List<Wallpaper> delWallpaperList = new List<Wallpaper>();

        /// <summary>
        /// 0-订阅壁纸加载器，1-本地备份壁纸加载器，2-官方备份壁纸加载器
        /// </summary>
        WallpaperLoader[] m_Loaders;

        /// <summary>
        /// 初始化
        /// <para>参数path无用，默认null，不需要输入</para>
        /// </summary>
        public override void Init(string path = null)
        {
            base.Init(WallpaperManager.localBackupLoader.loadPath);

            m_Loaders = new WallpaperLoader[]
            {
                WallpaperManager.storeLoader,
                WallpaperManager.localBackupLoader,
                WallpaperManager.backupLoader,
            };
            for (int i = 0; i < m_Loaders.Length; i++)
            {
                m_Loaders[i].OnBeforeLoading += base.WaitForReload;
            }
        }

        protected override void BeforeLoading()
        {
            newWallpaperList.Clear();
            changedWallpaperList.Clear();
            delWallpaperList.Clear();
        }

        // 该线程负责读取订阅、本低备份和官方备份壁纸，并比对
        protected override void LoadTask()
        {
            // 确保壁纸都已加载
            for (int i = 0; i < m_Loaders.Length; i++)
            {
                if (!m_Loaders[i].isLoaded && !m_Loaders[i].isLoading)
                {
                    m_Loaders[i].Load();
                }
            }

            // 90%进度用于读取壁纸
            bool isLoadersLoaded = false;
            int loadersProgress;
            while (!isLoadersLoaded)
            {
                Thread.Sleep(50);
                isLoadersLoaded = true;
                loadersProgress = 0;
                for (int i = 0; i < m_Loaders.Length; i++)
                {
                    if (!m_Loaders[i].isLoaded)
                    {
                        isLoadersLoaded = false;
                    }

                    loadersProgress += m_Loaders[i].loadingProgress;
                }

                loadingProgress = (int)(0.9f * loadersProgress / m_Loaders.Length);
            }

            // 剩下10%进度用于比对变更
            var storeWallpapersDic = GetWallpaperDic();
            var localWallpaperList = WallpaperManager.localBackupLoader.wallpaperList;
            for (int i = 0; i < localWallpaperList.Count; i++)
            {
                if (storeWallpapersDic.ContainsKey(localWallpaperList[i].id))
                {
                    if (storeWallpapersDic[localWallpaperList[i].id].lastWriteTime 
                        > localWallpaperList[i].lastWriteTime)
                    {
                        // 更改的壁纸
                        changedWallpaperList.Add(storeWallpapersDic[localWallpaperList[i].id]);
                    }

                    storeWallpapersDic.Remove(localWallpaperList[i].id);
                }
                else
                {
                    // 移除的壁纸
                    delWallpaperList.Add(localWallpaperList[i]);
                }

                loadingProgress = 90 + 9 * (i + 1) / localWallpaperList.Count;
            }
            // 新增的壁纸
            newWallpaperList = storeWallpapersDic.Values.ToList();

            loadingProgress = 100;
            isLoading = false;
            isLoaded = true;
            System.GC.Collect();
        }

        /// <summary>
        /// 获取整合订阅、官方备份的壁纸字典
        /// </summary>
        private Dictionary<string, Wallpaper> GetWallpaperDic()
        {
            var storeList = WallpaperManager.storeLoader.wallpaperList;
            var backupList = WallpaperManager.backupLoader.wallpaperList;

            Dictionary<string, Wallpaper> result = new Dictionary<string, Wallpaper>();
            for (int i = 0; i < storeList.Count; i++)
            {
                result.Add(storeList[i].id, storeList[i]);
            }
            for (int i = 0; i < backupList.Count; i++)
            {
                if (!result.ContainsKey(backupList[i].id))
                {
                    result.Add(backupList[i].id, backupList[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// 等待重载（会重载所有加载器）
        /// </summary>
        public override void WaitForReload()
        {
            base.WaitForReload();
            WaitForReloadAll();
        }

        /// <summary>
        /// 准备全部重载(全部加载器)
        /// </summary>
        public void WaitForReloadAll()
        {
            if (m_Loaders == null)
            {
                return;
            }

            for (int i = 0; i < m_Loaders.Length; i++)
            {
                m_Loaders[i].WaitForReload();
            }
        }

        /// <summary>
        /// 获取用于展示的壁纸字典
        /// </summary>
        private Dictionary<string, Wallpaper> GetViewDic(List<Wallpaper> list, bool isEveryone, bool isQuestionable, bool isMature)
        {
            var result = new Dictionary<string, Wallpaper>();
            string contentrating;

            for (int i = 0; i < list.Count; i++)
            {
                contentrating = list[i].contentrating;
                if (isQuestionable && contentrating == Contentrating.Questionable)
                {
                    result.Add(list[i].id, list[i]);
                }
                else if (isMature && contentrating == Contentrating.Mature)
                {
                    result.Add(list[i].id, list[i]);
                }
                else if (isEveryone && (contentrating != Contentrating.Questionable && contentrating != Contentrating.Mature))
                {
                    result.Add(list[i].id, list[i]);
                }
            }

            return result;
        }

        public Dictionary<string, Wallpaper> GetNewViewDic(bool isEveryone, bool isQuestionable, bool isMature)
        {
            return GetViewDic(newWallpaperList, isEveryone, isQuestionable, isMature);
        }

        public Dictionary<string, Wallpaper> GetChangedViewDic(bool isEveryone, bool isQuestionable, bool isMature)
        {
            return GetViewDic(changedWallpaperList, isEveryone, isQuestionable, isMature);
        }

        public Dictionary<string, Wallpaper> GetDelViewDic(bool isEveryone, bool isQuestionable, bool isMature)
        {
            return GetViewDic(delWallpaperList, isEveryone, isQuestionable, isMature);
        }
    }

    /// <summary>
    /// 壁纸数据管理器
    /// </summary>
    internal class WallpaperManager
    {
        /// <summary>
        /// 创意工坊壁纸加载器
        /// </summary>
        public static WallpaperLoader storeLoader = new WallpaperLoader();
        /// <summary>
        /// 本地备份壁纸加载器
        /// </summary>
        public static WallpaperLoader localBackupLoader = new WallpaperLoader();
        /// <summary>
        /// 官方备份壁纸加载器
        /// </summary>
        public static WallpaperLoader backupLoader = new WallpaperLoader();
        /// <summary>
        /// 解包壁纸加载器
        /// </summary>
        public static WallpaperLoader unpackLoader = new WallpaperLoader();

        /// <summary>
        /// 变更管理器
        /// </summary>
        public static WallpaperChanger wallpaperChanger = new WallpaperChanger();

        public static void Init()
        {
            storeLoader.Init(SettingManager.setting.storePath);
            localBackupLoader.Init(SettingManager.setting.localBackupPath);
            backupLoader.Init(SettingManager.setting.backupPath);
            unpackLoader.Init(SettingManager.setting.unpackPath);

            wallpaperChanger.Init();
        }
    }
}
