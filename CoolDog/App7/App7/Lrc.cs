using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;
using Windows.Storage;

namespace App7
{
    /// <summary>
    /// Lrc.xaml 的交互逻辑
    /// </summary>
    public class Lrc
    {
        public static string musicName { set; get; }
        public static string lrcPath { set; get; }
        static List<int> lrcTime = new List<int>();
        static List<int> lrcIndex = new List<int>();
        static List<string> lrcText = new List<string>();
        public static TextBox tb = new TextBox();
        private static int index = 0;
        private static int maxCount = 0;
        private static int count = 0;
        private static string ti;
        private static string ar;
        private static string al;
        private static string by;
        public string GetLrcPath()
        {
            if (musicName != null)
            {
                // int i = musicName.LastIndexOf('.') + 1;
                lrcPath = "ms-appx:///Assets/" + musicName + ".lrc";
            }
            else return null;

            return lrcPath;
        }
        /// <summary>
        /// 用于获取歌词中的相关音乐信息
        /// </summary>
        /// <param name="strb"></param>
        public void GetMusicInfo(string strb)
        {
            if (strb.IndexOf("ti") != -1)
            {
                ti = strb.Substring(strb.LastIndexOf(':') + 1);
            }
            else if (strb.IndexOf("ar") != -1)
            {
                ar = strb.Substring(strb.LastIndexOf(':') + 1);
            }
            else if (strb.IndexOf("al") != -1)
            {
                al = strb.Substring(strb.LastIndexOf(':') + 1);
            }
            else if (strb.IndexOf("by") != -1)
            {
                by = strb.Substring(strb.LastIndexOf(':') + 1);
            }
        }
        /// <summary>
        /// 用于计算传进来的字符串时间总和并存进lrcTime中
        /// </summary>
        /// <param name="strb">格式XX:XX.XX</param>
        /// <returns></returns>
        public void Computingtime(string stra)
        {
            if (stra[0] >= 0 && stra[1] >= 0)
            {
                char a = stra[0];
                int m1 = stra[4] - 48;
                int i = ((stra[0] - 48) * 10 + stra[1] - 48) * 60 + (stra[3] - 48) * 10 + stra[4] - 48;
                if (i >= 0)
                {
                    lrcTime.Add(i);
                    lrcIndex.Add(index);
                }
            }
            else GetMusicInfo(stra);

        }
        /// <summary>
        /// 显示歌词
        /// </summary>
        public void ShowLrc(int min, int sec,TextBox txtb1,ListView lrc1)
        {
            if (count >= maxCount || maxCount <= 0) return;
            int time = min * 60 + sec;
            string show;
            if (time < lrcTime[0])
            {
                show = "歌名:" + ti + "演唱：" + ar + "专辑：" + al + "制作：" + by;
                txtb1.Text = show;
            }
            if (time == lrcTime[count])
            {
                
                show = lrcText[lrcIndex[count++]];
              
                lrc1.SelectedItem = show;
                lrc1.ScrollIntoView(lrc1.SelectedItem);
                txtb1.Text = show;



            }
            else if (time > lrcTime[count])
            {
                int i2 = lrcTime.Count();
                for (int i = count; i < i2 - 1; i++)
                {
                    if (time <= lrcTime[i])
                    {
                        count = i;
                        break;

                    }
                }
            }
            else if (count > 0 && time < lrcTime[count - 1])
            {
                for (int i = 0; i < count - 1; i++)
                {
                    if (time >= lrcTime[i])
                    {
                        count = i;
                        break;

                    }
                }
            }

        }

        private async Task<string> readTextFromLocalStorage(string filename)
        {
            var fold = Windows.Storage.ApplicationData.Current.LocalFolder;

            StorageFile file = await fold.GetFileAsync(@"Assets\xiaoxingyun.lrc");

            string result = await FileIO.ReadTextAsync(file);

            return result;
        }


        /// <summary>
        /// 分析歌词并存放
        /// </summary>
        public async void LoadLrc(ListView lrc1)
        {
            string re = await readTextFromLocalStorage("");
            var i = new MessageDialog(re).ShowAsync();
            if (lrcPath == null) GetLrcPath();
            //string file = @"C:\Program Files\xiaoxingyun.lrc";
            //if (System.IO.File.Exists(file))
            //{
            //    var i = new MessageDialog("Login successfully and come back to the CoolDog! ").ShowAsync();
            //    return;
            //}
            if (File.Exists(lrcPath) == false) return;
            // String st = @"C:\Program Files\xiaoxingyun.lrc";
            FileStream aFile = new FileStream(@"C:\Program Files\xiaoxingyun.lrc", FileMode.Open);
            StreamReader fLrc = new StreamReader(aFile, Encoding.UTF8);
            string str;
            while (fLrc.EndOfStream == false)
            {
                //str = fLrc.ReadLine();
                str = fLrc.ReadToEnd();
                int left = str.IndexOf("[");
                int right = str.IndexOf("]");
                if (right - left > 0 && right - left != 9)
                {
                    GetMusicInfo(str.Substring(left + 1, right - left - 1));
                    str = str.Remove(left, right + 1);
                }
                else if (right - left == 9)
                {
                    while (right - left == 9)
                    {
                        Computingtime(str.Substring(1, right - 1 - left));
                        str = str.Remove(0, right + 1 - left);
                        left = str.IndexOf("[");
                        right = str.IndexOf("]");
                        //找出歌词内容并存在lrcText中
                        if (str.Count() > 0)
                            if (str[0] != '[')
                            {
                                index++;
                                string tmp;
                                if (left == -1)
                                {
                                    tmp = str.Substring(0);
                                    lrcText.Add(tmp);
                                    lrc1.Items.Add(tmp);
                                }
                                else
                                {
                                    string strtmp = str.Substring(0, left);
                                    lrcText.Add(strtmp);
                                    str = str.Remove(0, left);
                                }
                            }
                    }
                }
            }
            lrcTime.Sort();
            maxCount = lrcTime.Count();

            fLrc.Dispose();
        }

        /// <summary>
        /// 清楚所有内容
        /// </summary>

        public static void Clear(TextBox txtb1, ListView lrc1)
        {
            lrcPath = null;
            musicName = null;
            lrcText.Clear();
            lrcTime.Clear();
            lrcIndex.Clear();
            count = 0;
            maxCount = 0;
            index = 0;
            ti = null;
            ar = null;
            al = null;
            by = null;
            lrc1.Items.Clear();
            txtb1.Text = "";
        }


    }
}
