using App7;
using App7.ViewModels;
using Newtonsoft.Json;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;


// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上提供

namespace App7
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class NewPage : Page
    {
        // when the user login and we get the name
        public static string user = "";
        // music lrc
        //static Lrc lrc = new Lrc();

        //记录最后暂停是在哪个播放器
        int lastPaused_index;

        public NewPage()
        {
            this.InitializeComponent();
            var viewTitleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
            viewTitleBar.BackgroundColor = Windows.UI.Colors.CornflowerBlue;
            viewTitleBar.ButtonBackgroundColor = Windows.UI.Colors.CornflowerBlue;
            lastPaused_index = 0;
        }

        ViewModels.SongItemViewModel ViewModel { get; set; }
        public object ViewModels { get; private set; }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // we get the username and show him the songs
            user = e.Parameter.ToString();
            user_name.Text = user;

            this.ViewModel = new ViewModels.SongItemViewModel();
            
            // 在主页面永远屏蔽返回按钮
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                AppViewBackButtonVisibility.Collapsed;
            

            if (e.Parameter.GetType() == typeof(ViewModels.SongItemViewModel))
            {
                this.ViewModel = (ViewModels.SongItemViewModel)(e.Parameter);
            }

            DataTransferManager.GetForCurrentView().DataRequested += OnShareDataRequested;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            DataTransferManager.GetForCurrentView().DataRequested -= OnShareDataRequested;
        }

        // 点击SongItem后即选中该首歌
        private void SongItem_Clicked(object sender, ItemClickEventArgs e)
        {
            if (ViewModel.SelectItems != (Models.SongItem)(e.ClickedItem))
            {
                ViewModel.SelectItems = (Models.SongItem)(e.ClickedItem);

                lastPaused_index = 0;
                media2.Source = new Uri("ms-appx:///Assets/" + ViewModel.SelectItems.songfilename + ".mp3");

                if (ViewModel.SelectItems.songid != 0)
                    media1.Source = new Uri("ms-appx:///Assets/" + ViewModel.AllItems.ElementAt(ViewModel.SelectItems.songid - 1).songfilename + ".mp3");

                if (ViewModel.SelectItems.songid != ViewModel.AllItems.Count - 1)
                    media3.Source = new Uri("ms-appx:///Assets/" + ViewModel.AllItems.ElementAt(ViewModel.SelectItems.songid + 1).songfilename + ".mp3");

            }
        }

        private void updateTile_Click(object sender, RoutedEventArgs e)
        {
            Windows.Data.Xml.Dom.XmlDocument tileXml = new Windows.Data.Xml.Dom.XmlDocument();
            tileXml.LoadXml(File.ReadAllText("tiles.xml"));

            var tileTextAttributes = tileXml.GetElementsByTagName("text");

            if (ViewModel.AllItems.Count <= 0)
            {
                for (int i = 0; i < 6; i++)
                    if (i % 2 == 0)
                        tileTextAttributes[i].InnerText = "No SongItem now !";
                    else
                        tileTextAttributes[i].InnerText = "";
            }
            else
            {
                for (int i = 0; i < 6; i++)
                {
                    if (i % 2 == 0)
                        tileTextAttributes[i].InnerText = "title: ";
                    else
                        tileTextAttributes[i].InnerText = ViewModel.AllItems.ElementAt(0).songname;
                }
            }

            var updator = TileUpdateManager.CreateTileUpdaterForApplication();
            var notification = new TileNotification(tileXml);
            updator.Update(notification);
        }

        //以下为左边一列5个按钮需要实现的功能
        //下面的为从本地挑选歌曲的函数
        private async void addSongButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

            // Users expect to have a filtered view of their folders 
            openPicker.FileTypeFilter.Add(".mp3");

            // Open the picker for the user to pick a file
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                String songfilename = file.DisplayName;
                String[] substr = Regex.Split(songfilename, "-");
                //var ii = new MessageDialog(substr[0]+"\n"+ substr[1]).ShowAsync();
                ViewModel.AddSongItem(ViewModel.AllItems.Count, songfilename, substr[0], substr[1], null);

                var db = App.conn2;
                try
                {
                    using (var custstmt = db.Prepare("INSERT INTO Songs (Songid, Songfilename, Songname, Singer, user) VALUES (?, ?, ?, ?, ?)"))
                    {
                        custstmt.Bind(1, ViewModel.AllItems.Count - 1);
                        custstmt.Bind(2, songfilename);
                        custstmt.Bind(3, substr[0]);
                        custstmt.Bind(4, substr[1]);
                        custstmt.Bind(5, user);
                        custstmt.Step();
                    }
                }
                catch (Exception ex)
                {
                    var i = new MessageDialog(ex.ToString()).ShowAsync();
                }
            }
        }

        private async void GetLrc(string tel, string tel1)
        {
            try
            {
                // 创建一个HTTP client实例对象
                HttpClient httpClient = new HttpClient();

                // Add a user-agent header to the GET request. 
                var headers = httpClient.DefaultRequestHeaders;

                // The safe way to add a header value is to use the TryParseAdd method and verify the return value is true,
                // especially if the header value is coming from user input.
                string header = "ie Mozilla/5.0 (Windows NT 6.2; WOW64; rv:25.0) Gecko/20100101 Firefox/25.0";
                if (!headers.UserAgent.TryParseAdd(header))
                {
                    throw new Exception("Invalid header value: " + header);
                }


                string getlrc = "http://geci.me/api/lyric/" + tel + "/" + tel1;

                //发送GET请求
                HttpResponseMessage response = await httpClient.GetAsync(getlrc);

                // 确保返回值为成功状态
                response.EnsureSuccessStatusCode();
                
                Byte[] getByte = await response.Content.ReadAsByteArrayAsync();
                
                // UTF-8是Unicode的实现方式之一。这里采用UTF-8进行编码
                Encoding code = Encoding.GetEncoding("UTF-8");
                string result = code.GetString(getByte, 0, getByte.Length);

                JsonTextReader json = new JsonTextReader(new StringReader(result));
                string jsonVal = "", songslrc = "";

                // 获取lrc文件
                while (json.Read())
                {
                    jsonVal += json.Value;
                    if (jsonVal.Equals("lrc"))  // 读到“lrc”时，取出下一个json token
                    {
                        json.Read();
                        songslrc += json.Value;  // 该对象重载了“+=”,故可与字符串进行连接
                        break;
                    }
                    jsonVal = "";
                }

                //发送GET请求
                HttpResponseMessage response1 = await httpClient.GetAsync(songslrc);

                response1.EnsureSuccessStatusCode();

                Byte[] getByte1 = await response1.Content.ReadAsByteArrayAsync();
                Encoding code1 = Encoding.GetEncoding("UTF-8");
                string result1 = code1.GetString(getByte1, 0, getByte1.Length);
                txtb1.Text = result1;
            }
            catch (HttpRequestException ex1)
            {
                txtb1.Text = ex1.ToString();
            }
            catch (Exception ex2)
            {
                txtb1.Text = ex2.ToString();
            }
        }

        private async void Getimg(string tel, string tel1)
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                
                var headers = httpClient.DefaultRequestHeaders;
                
                string header = "ie Mozilla/5.0 (Windows NT 6.2; WOW64; rv:25.0) Gecko/20100101 Firefox/25.0";
                if (!headers.UserAgent.TryParseAdd(header))
                {
                    throw new Exception("Invalid header value: " + header);
                }


                string getsongsaid = "http://geci.me/api/lyric/" + tel + "/" + tel1;
                
                HttpResponseMessage response = await httpClient.GetAsync(getsongsaid);
                
                response.EnsureSuccessStatusCode();
                
                Byte[] getByte = await response.Content.ReadAsByteArrayAsync();
                
                Encoding code = Encoding.GetEncoding("UTF-8");
                string result = code.GetString(getByte, 0, getByte.Length);

                JsonTextReader json = new JsonTextReader(new StringReader(result));
                string jsonVal = "", songsaid = "";

                // 先获取歌曲对应的AID，再去第二个URL中查询
                while (json.Read())
                {
                    jsonVal += json.Value;
                    if (jsonVal.Equals("aid"))  // 读到“aid”时，取出下一个json token
                    {
                        json.Read();
                        songsaid += json.Value;  // 该对象重载了“+=”,故可与字符串进行连接
                        break;
                    }
                    jsonVal = "";
                }

                string getaidimg = "http://geci.me/api/cover/" + songsaid;

                //发送GET请求
                HttpResponseMessage response1 = await httpClient.GetAsync(getaidimg);

                response1.EnsureSuccessStatusCode();

                Byte[] getByte1 = await response1.Content.ReadAsByteArrayAsync();
                Encoding code1 = Encoding.GetEncoding("UTF-8");
                string result1 = code1.GetString(getByte1, 0, getByte1.Length);
                JsonTextReader json1 = new JsonTextReader(new StringReader(result1));
                string jsonVal1 = "", aidimg = "";

                // 获取cover
                while (json1.Read())
                {
                    jsonVal1 += json1.Value;
                    if (jsonVal1.Equals("cover"))  // 读到“cover”时，取出下一个json token
                    {
                        json1.Read();
                        aidimg += json1.Value; 
                        break;
                    }
                    jsonVal1 = "";
                }

                image.Source = new BitmapImage(new Uri(aidimg));
            }
            catch (HttpRequestException ex1)
            {
                txtb1.Text += ex1.ToString();
            }
            catch (Exception ex2)
            {
                txtb1.Text += ex2.ToString();
            }
        }

        private void broadcastButton_Click(object sender, RoutedEventArgs e)
        {
            //播放选中的歌，在SongItem_Clicked函数已经选中某首歌曲
            if (ViewModel.SelectItems != null)
            {

                if (lastPaused_index == 0)
                {
                    media2.Play();
                    GetLrc(ViewModel.SelectItems.songname, ViewModel.SelectItems.singer);
                    Getimg(ViewModel.SelectItems.songname, ViewModel.SelectItems.singer);
                }
                else
                {
                    if (lastPaused_index == 1)
                    {
                        media1.Play();
                        GetLrc(ViewModel.SelectItems.songname, ViewModel.SelectItems.singer);
                        Getimg(ViewModel.SelectItems.songname, ViewModel.SelectItems.singer);
                    }
                    else if (lastPaused_index == 2)
                    {
                        media2.Play();
                        GetLrc(ViewModel.SelectItems.songname, ViewModel.SelectItems.singer);
                        Getimg(ViewModel.SelectItems.songname, ViewModel.SelectItems.singer);
                    }
                    else if (lastPaused_index == 3)
                    {
                        media3.Play();
                        GetLrc(ViewModel.SelectItems.songname, ViewModel.SelectItems.singer);
                        Getimg(ViewModel.SelectItems.songname, ViewModel.SelectItems.singer);
                    }
                }
                //Lrc.Clear(txtb1, lrc1);
                //Lrc.musicName = ViewModel.SelectItems.songname;
                //lrc.LoadLrc(lrc1);
            }
            else
            {
                var i = new MessageDialog("No selecting songs now!").ShowAsync();
            }
        }
        private void pauseButton_Click(object sender, RoutedEventArgs e)
        {
            //暂停选中的歌，在SongItem_Clicked函数已经选中某首歌曲
            if (ViewModel.SelectItems != null)
            {
                if (media1.CurrentState == MediaElementState.Playing)
                {
                    media1.Pause();
                    lastPaused_index = 1;
                }
                else if (media2.CurrentState == MediaElementState.Playing)
                {
                    media2.Pause();
                    lastPaused_index = 2;
                }
                else if (media3.CurrentState == MediaElementState.Playing)
                {
                    media3.Pause();
                    lastPaused_index = 3;
                }
            }
            else
            {
                var i = new MessageDialog("No selecting songs now!").ShowAsync();
            }
        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            //停止播放选中的歌，在SongItem_Clicked函数已经选中某首歌曲
            if (ViewModel.SelectItems != null)
            {
                if (media1.CurrentState == MediaElementState.Playing)
                    media1.Stop();
                else if (media2.CurrentState == MediaElementState.Playing)
                    media2.Stop();
                else if (media3.CurrentState == MediaElementState.Playing)
                    media3.Stop();

                ViewModel.SelectItems = null;
            }
            else
            {
                var i = new MessageDialog("No selecting songs now!").ShowAsync();
            }
        }

        private void previousSongButton_Click(object sender, RoutedEventArgs e)
        {
            int playing_index = 0;
            if (media1.CurrentState == MediaElementState.Playing)
            {
                media1.Stop();
                playing_index = 1;
            }
            else if (media2.CurrentState == MediaElementState.Playing)
            {
                media2.Stop();
                playing_index = 2;
            }
            else if (media3.CurrentState == MediaElementState.Playing)
            {
                media3.Stop();
                playing_index = 3;
            }

            //播放选中的歌的前一首，在SongItem_Clicked函数已经选中某首歌曲
            if (ViewModel.SelectItems != null)
            {
                if (ViewModel.AllItems.Count == 0)
                {
                    var i = new MessageDialog("No songs now!").ShowAsync();
                }
                else if (ViewModel.SelectItems.songid == 0)
                {
                    var i = new MessageDialog("This is the first song now!").ShowAsync();
                }
                else
                {
                    if (playing_index == 1)
                    {
                        media3.Play();
                        
                        if (ViewModel.SelectItems.songid - 1 != 0)
                            media2.Source = new Uri("ms-appx:///Assets/" + ViewModel.AllItems.ElementAt(ViewModel.SelectItems.songid - 2).songfilename + ".mp3");

                        media1.Source = new Uri("ms-appx:///Assets/" + ViewModel.SelectItems.songfilename + ".mp3");

                    }
                    else if (playing_index == 2)
                    {
                        media1.Play();

                        if (ViewModel.SelectItems.songid - 1 != 0)
                            media3.Source = new Uri("ms-appx:///Assets/" + ViewModel.AllItems.ElementAt(ViewModel.SelectItems.songid - 2).songfilename + ".mp3");

                        media2.Source = new Uri("ms-appx:///Assets/" + ViewModel.SelectItems.songfilename + ".mp3");
                    }
                    else if (playing_index == 3)
                    {
                        media2.Play();

                        if (ViewModel.SelectItems.songid - 1 != 0)
                            media1.Source = new Uri("ms-appx:///Assets/" + ViewModel.AllItems.ElementAt(ViewModel.SelectItems.songid - 2).songfilename + ".mp3");

                        media3.Source = new Uri("ms-appx:///Assets/" + ViewModel.SelectItems.songfilename + ".mp3");
                    }

                    GetLrc(ViewModel.AllItems.ElementAt(ViewModel.SelectItems.songid - 1).songname, ViewModel.AllItems.ElementAt(ViewModel.SelectItems.songid - 1).singer);
                    Getimg(ViewModel.AllItems.ElementAt(ViewModel.SelectItems.songid - 1).songname, ViewModel.AllItems.ElementAt(ViewModel.SelectItems.songid - 1).singer);

                    ViewModel.SelectItems = ViewModel.AllItems.ElementAt(ViewModel.SelectItems.songid - 1);
                }
            }
            else
            {
                var i = new MessageDialog("No selecting songs now!").ShowAsync();
            }

        }
        private void nextSongButton_Click(object sender, RoutedEventArgs e)
        {
            int playing_index = 0;
            if (media1.CurrentState == MediaElementState.Playing)
            {
                media1.Stop();
                playing_index = 1;
            }
            else if (media2.CurrentState == MediaElementState.Playing)
            {
                media2.Stop();
                playing_index = 2;
            }
            else if (media3.CurrentState == MediaElementState.Playing)
            {
                media3.Stop();
                playing_index = 3;
            }

            //播放选中的歌的下一首，在SongItem_Clicked函数已经选中某首歌曲
            if (ViewModel.SelectItems != null)
            {
                if (ViewModel.AllItems.Count == 0)
                {
                    var i = new MessageDialog("No songs now!").ShowAsync();
                }
                else if (ViewModel.SelectItems.songid == ViewModel.AllItems.Count - 1)
                {
                    var i = new MessageDialog("This is the last song now!").ShowAsync();
                }
                else
                {
                    if (playing_index == 1)
                    {
                        media2.Play();

                        if (ViewModel.SelectItems.songid + 1 != ViewModel.AllItems.Count - 1)
                            media3.Source = new Uri("ms-appx:///Assets/" + ViewModel.AllItems.ElementAt(ViewModel.SelectItems.songid + 2).songfilename + ".mp3");

                        media1.Source = new Uri("ms-appx:///Assets/" + ViewModel.SelectItems.songfilename + ".mp3");

                    }
                    else if (playing_index == 2)
                    {
                        media3.Play();

                        if (ViewModel.SelectItems.songid + 1 != ViewModel.AllItems.Count - 1)
                            media1.Source = new Uri("ms-appx:///Assets/" + ViewModel.AllItems.ElementAt(ViewModel.SelectItems.songid + 2).songfilename + ".mp3");

                        media2.Source = new Uri("ms-appx:///Assets/" + ViewModel.SelectItems.songfilename + ".mp3");
                    }
                    else if (playing_index == 3)
                    {
                        media1.Play();

                        if (ViewModel.SelectItems.songid + 1 != ViewModel.AllItems.Count - 1)
                            media2.Source = new Uri("ms-appx:///Assets/" + ViewModel.AllItems.ElementAt(ViewModel.SelectItems.songid + 2).songfilename + ".mp3");

                        media3.Source = new Uri("ms-appx:///Assets/" + ViewModel.SelectItems.songfilename + ".mp3");
                    }

                    GetLrc(ViewModel.AllItems.ElementAt(ViewModel.SelectItems.songid + 1).songname, ViewModel.AllItems.ElementAt(ViewModel.SelectItems.songid + 1).singer);
                    Getimg(ViewModel.AllItems.ElementAt(ViewModel.SelectItems.songid + 1).songname, ViewModel.AllItems.ElementAt(ViewModel.SelectItems.songid + 1).singer);

                    ViewModel.SelectItems = ViewModel.AllItems.ElementAt(ViewModel.SelectItems.songid + 1);
                }
            }
            else
            {
                var i = new MessageDialog("No selecting songs now!").ShowAsync();
            }
        }


        //以下为点击某首歌setting按钮弹出的菜单的函数
        //下面为分享某首歌的文字内容：歌曲名、歌手等
        private void sharemenu_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.GetForCurrentView().DataRequested -= OnShareDataRequested;
            ViewModel.SelectItems = ((MenuFlyoutItem)sender).DataContext as Models.SongItem;
            DataTransferManager.GetForCurrentView().DataRequested += OnShareDataRequested;
            //var i = new MessageDialog(ViewModel.ChosenOne.title).ShowAsync();
            DataTransferManager.ShowShareUI();
        }
        async void OnShareDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var dp = args.Request.Data;
            var deferral = args.Request.GetDeferral();

            var photoFile = await StorageFile.GetFileFromApplicationUriAsync(
                                    new Uri("ms-appx:///Assets/item.jpg"));
            dp.Properties.Title = "Share example";
            dp.Properties.Description = "A demonstration on how to share";
            dp.SetText(ViewModel.SelectItems.songfilename);
            dp.SetStorageItems(new List<StorageFile> { photoFile });

            deferral.Complete();
        }

        private void deletemenu_Click(object sender, RoutedEventArgs e)
        {
            //此函数为删除列表中setting按钮对应的歌曲

            //选中setting按钮对应的歌曲
            ViewModel.SelectItems = ((MenuFlyoutItem)sender).DataContext as Models.SongItem;
            String temp_Songname = ViewModel.SelectItems.songname;

            var db = App.conn2;
            try
            {
                using (var custstmt = db.Prepare("DELETE FROM Songs WHERE Songname = ? AND user = ?"))
                {
                    custstmt.Bind(1, temp_Songname);
                    custstmt.Bind(2, user);
                    custstmt.Step();
                }
            }
            catch (Exception ex)
            {
                var i = new MessageDialog(ex.ToString()).ShowAsync();
            }

            ViewModel.DeleteSongItem();
        }

        

        //以下为根据歌手/歌曲名搜索列表中的歌曲
        private void BtnGetAll_Click(object sender, RoutedEventArgs e)
        {
            string SelectText = "";
            string queryContent = querybox.Text;

            var db = App.conn2;
            try
            {
                using (var statement = db.Prepare("SELECT Songid, Songfilename, Songname, Singer, user FROM Songs WHERE user = ? AND Songname LIKE ? or user = ? AND Singer LIKE ?"))
                {
                    string str = "%" + queryContent + "%";
                    statement.Bind(1, user);
                    statement.Bind(2, str);
                    statement.Bind(3, user);
                    statement.Bind(4, str);
                    while (statement.Step() == SQLiteResult.ROW)
                    {
                        //SelectText += "The query result:\n";
                        SelectText += "Songid：" + (long)statement[0] + " ; ";
                        SelectText += "Songfilename：" + (string)statement[1] + " ; ";
                        SelectText += "Songname：" + (string)statement[2] + " ; ";
                        SelectText += "Singer：" + (string)statement[3] + " ; ";
                        SelectText += "User：" + (string)statement[4] + "\n";
                    }
                }
            }
            catch (Exception ex)
            {
                var i = new MessageDialog(ex.ToString()).ShowAsync();
            }

            if (SelectText == "")
                SelectText += "No record is found.";
            var j = new MessageDialog(SelectText).ShowAsync();
        }

        private void logout_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }

        private void setting_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingPage), user);
        }

    }
}