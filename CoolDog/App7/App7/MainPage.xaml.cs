using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

//“空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 上有介绍

namespace App7
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;


            // 在登陆页面永远屏蔽返回按钮
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                AppViewBackButtonVisibility.Collapsed;
            
        }

        // the button for user to login to the CoolDog
        private void user_login(object sender, RoutedEventArgs e)
        {
            using (var statement = App.conn.Prepare("SELECT username, password FROM Users WHERE username LIKE ? AND password LIKE ?"))
            {
                bool judge = false;
                statement.Bind(1, username.Text);
                statement.Bind(2, password.Password);
                while (statement.Step() != SQLitePCL.SQLiteResult.DONE)
                {
                    //var i = new MessageDialog("Login successfully and come back to the CoolDog! ").ShowAsync();
                    judge = true;
                    string user = username.Text;
                    Frame.Navigate(typeof(NewPage), user);
                }
                if (judge == true) return;
                else
                {
                    var i = new MessageDialog("Login fail! ").ShowAsync();
                }
            }
        }

        // the button for user to register a new account in CooDog
        private void user_register(object sender, RoutedEventArgs e)
        {   
            // judge the username if blank or not
            if (username.Text == "")
            {
                var i = new MessageDialog("We're sorry but you can't input no username! ").ShowAsync();
            }
            // judge the length of the password if more than 6 or not
            else if (password.Password.Length < 6)
            {
                var i = new MessageDialog("We're sorry but you should input more than 6 bytes password! ").ShowAsync();
            }
            else
            {
                bool judge = false;
                // judge the username if registered or not
                using (var statement = App.conn.Prepare("SELECT username FROM Users WHERE username LIKE ?"))
                {
                    statement.Bind(1, username.Text);
                    string dialog = "";
                    while (statement.Step() != SQLitePCL.SQLiteResult.DONE)
                    {
                        dialog += "We're sorry but the username " + username.Text + " has registered! ";
                    }
                    if (dialog != "")
                    {
                        // username has registered and register again
                        var i = new MessageDialog(dialog).ShowAsync();
                        username.Text = "";
                        password.Password = "";
                    }
                    else
                    {
                        // register successfully
                        var i = new MessageDialog("Welcome to CoolDog and you're one of us! ").ShowAsync();
                        judge = true;
                    }
                }
                if (judge == true)
                {
                    using (var statement = App.conn.Prepare("INSERT INTO Users (username, password, datetime, nickname, email, phone, qq ,wechat, sex) VALUES(?, ?, ?, ?, ?, ?, ?, ?, ?)"))
                    {
                        statement.Bind(1, username.Text);
                        statement.Bind(2, password.Password);
                        statement.Bind(3, DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss", DateTimeFormatInfo.InvariantInfo));
                        statement.Bind(4, "");
                        statement.Bind(5, "");
                        statement.Bind(6, "");
                        statement.Bind(7, "");
                        statement.Bind(8, "");
                        statement.Bind(9, "");
                        statement.Step();
                        username.Text = "";
                        password.Password = "";
                    }
                }
            }
        }

        private void Mouse_In(object sender, RoutedEventArgs e)
        {
            username.Text = "";
            password.Password = "";
        }
    }
}
