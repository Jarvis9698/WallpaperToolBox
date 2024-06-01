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
        public string dirSize { get; set; }
        /// <summary>
        /// 预览图
        /// </summary>
        public Bitmap previewImage { get; set; }
        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime lastWriteTime { get; set; }
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
        public void WaitForReload()
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
            if (string.IsNullOrEmpty(loadPath) || !Directory.Exists(loadPath))
            {
                isLoaded = true;
                return;
            }

            isLoading = true;
            wallpaperList.Clear();

            m_WallpaperLoadTask = new Task(LoadTask);
            m_WallpaperLoadTask.Start();
        }

        protected virtual void LoadTask()
        {
            DirectoryInfo[] dirInfo = new DirectoryInfo(loadPath).GetDirectories();
            int maxProgress = dirInfo.Length;
            for (int i = 0; i < dirInfo.Length; i++)
            {
                Wallpaper wallpaper = Tools.LoadWallpaperValue(dirInfo[i]);
                if (wallpaper != null)
                {
                    wallpaperList.Add(wallpaper);
                }

                loadingProgress = 100 * i / maxProgress;
            }

            isLoading = false;
            isLoaded = true;
            System.GC.Collect();
            OnLoaded?.Invoke();
        }

        /// <summary>
        /// 移除壁纸
        /// </summary>
        public virtual void Remove(List<Wallpaper> removeList)
        {
            for (int i = 0; i < removeList.Count; i++)
            {
                wallpaperList.Remove(removeList[i]);
            }
        }

        /// <summary>
        /// 根据过滤选项获取用于展示的壁纸数据
        /// </summary>
        public List<Wallpaper> GetViewList(bool isEveryone, bool isQuestionable, bool isMature)
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

            return result;
        }
    }

    public class WallpaperChanger : WallpaperLoader
    {
        WallpaperLoader[] m_Loaders;

        public override void Init(string path)
        {
            base.Init(path);

            m_Loaders = new WallpaperLoader[]
            {
                WallpaperManager.storeLoader,
                WallpaperManager.localBackupLoader,
                WallpaperManager.backupLoader,
            };
            for (int i = 0; i < m_Loaders.Length; i++)
            {
                m_Loaders[i].OnLoaded += WaitForReload;
            }
        }

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

                loadingProgress = loadersProgress / m_Loaders.Length;
            }

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
        /// 获取新增壁纸列表
        /// </summary>
        public List<Wallpaper> GetAddViewList(bool isEveryone, bool isQuestionable, bool isMature)
        {
            var localList = WallpaperManager.localBackupLoader.wallpaperList;
            var wallpaperDic = GetWallpaperDic();

            for (int i = 0; i < localList.Count; i++)
            {
                if (wallpaperDic.ContainsKey(localList[i].id))
                {
                    wallpaperDic.Remove(localList[i].id);
                }
            }
            wallpaperList = wallpaperDic.Values.ToList();
            return GetViewList(isEveryone, isQuestionable, isMature);
        }

        /// <summary>
        /// 获取修改壁纸列表
        /// </summary>
        public List<Wallpaper> GetChangedViewList(bool isEveryone, bool isQuestionable, bool isMature)
        {
            var localList = WallpaperManager.localBackupLoader.wallpaperList;
            var wallpaperDic = GetWallpaperDic();
            wallpaperList = new List<Wallpaper>();

            for (int i = 0; i < localList.Count; i++)
            {
                if (!wallpaperDic.ContainsKey(localList[i].id))
                {
                    continue;
                }

                if (wallpaperDic[localList[i].id].lastWriteTime > localList[i].lastWriteTime)
                {
                    wallpaperList.Add(localList[i]);
                }
            }
            return GetViewList(isEveryone, isQuestionable, isMature);
        }

        /// <summary>
        /// 获取移除壁纸列表
        /// </summary>
        public List<Wallpaper> GetDelViewList(bool isEveryone, bool isQuestionable, bool isMature)
        {
            var localList = WallpaperManager.localBackupLoader.wallpaperList;
            var wallpaperDic = GetWallpaperDic();
            wallpaperList = new List<Wallpaper>();

            for (int i = 0; i < localList.Count; i++)
            {
                if (!wallpaperDic.ContainsKey(localList[i].id))
                {
                    wallpaperList.Add(localList[i]);
                }
            }
            return GetViewList(isEveryone, isQuestionable, isMature);
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

            wallpaperChanger.Init(SettingManager.setting.localBackupPath);
        }
    }
}
