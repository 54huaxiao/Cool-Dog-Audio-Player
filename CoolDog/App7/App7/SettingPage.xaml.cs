using SQLitePCL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上有介绍

namespace App7
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SettingPage : Page
    {
        public static string user = "";
        public SettingPage()
        {
            this.InitializeComponent();
            using (var statement = App.conn.Prepare("SELECT nickname, email, phone, qq, wechat, sex FROM Users WHERE username = ?"))
            {
                statement.Bind(1, NewPage.user);
                while (statement.Step() != SQLiteResult.DONE)
                {
                    nickname.Text = (string)statement[0];
                    email.Text = (string)statement[1];
                    phone.Text = (string)statement[2];
                    qq.Text = (string)statement[3];
                    wechat.Text = (string)statement[4];
                    if (statement[5].ToString() == "male" )
                    {
                        male.IsChecked = true;
                    } else
                    {
                        female.IsChecked = true;
                    }
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            user = e.Parameter.ToString();

            if (rootFrame.CanGoBack)
            {
                // Show UI in title bar if opted-in and in-app backstack is not empty.
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    AppViewBackButtonVisibility.Visible;
            }
            else
            {
                // Remove the UI from the title bar if in-app back stack is empty.
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    AppViewBackButtonVisibility.Collapsed;
            }
        }

        private async void SelectPictureButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".bmp");
            picker.FileTypeFilter.Add(".gif");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read);
                Windows.UI.Xaml.Media.Imaging.BitmapImage bmp = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                bmp.SetSource(stream);
                this.image.Source = bmp;
            }
        }

        private void setPasswordBtn_Click(object sender, RoutedEventArgs e)
        {
            if (password.Password != password_confirm.Password)
            {
                var i = new MessageDialog("Please input the same password for two times! ").ShowAsync();
                password.Password = "";
                password_confirm.Password = "";
            }
            else if (password.Password.ToString().Length < 6)
            {
                var i = new MessageDialog("The length of the password must be bigger than 6! ").ShowAsync();
                password.Password = "";
                password_confirm.Password = "";
            }
            else
            {
                using (var statement = App.conn.Prepare("UPDATE Users SET password = ? WHERE username = ? "))
                {
                    statement.Bind(1, password.Password);
                    statement.Bind(2, user);
                    statement.Step();
                    var i = new MessageDialog("Update the password successfully! ").ShowAsync();
                }
            }
        }

        private void settingBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var statement = App.conn.Prepare("UPDATE Users SET nickname = ?, email = ?, phone = ?, qq = ?, wechat = ?, sex = ? WHERE username = ? "))
            {
                statement.Bind(1, nickname.Text);
                statement.Bind(2, email.Text);
                statement.Bind(3, phone.Text);
                statement.Bind(4, qq.Text);
                statement.Bind(5, wechat.Text);
                if (male.IsChecked == true)
                {
                    statement.Bind(6, "male");
                } else
                {
                    statement.Bind(6, "female");
                }
                statement.Bind(7, user);
                statement.Step();
            }
            Frame.Navigate(typeof(NewPage), user);
        }
    }
}
