using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sunny.UI;

namespace WallpaperToolBox
{
    public partial class TabPage_Main : UIUserControl
    {
        public TabPage_Main()
        {
            InitializeComponent();
            pictureBox2.Image = Tools.LoadImage(SettingManager.currentDirectoryPath + "Images\\功能概念图.png", new Size(441, 381));
        }
    }
}
