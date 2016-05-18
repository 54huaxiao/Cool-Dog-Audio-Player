using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media.Imaging;

namespace App7.ViewModels
{
    class SongItemViewModel
    {
        private ObservableCollection<Models.SongItem> allItems = new ObservableCollection<Models.SongItem>();
        public ObservableCollection<Models.SongItem> AllItems { get { return this.allItems; } }

        private Models.SongItem selectItems = default(Models.SongItem);
        public Models.SongItem SelectItems { get { return this.selectItems; } set { this.selectItems = value; } }


        public SongItemViewModel()
        {
            var db = App.conn2;
            string _songfilename, _songname, _singer;
            long _songid;
            string login_username = NewPage.user;
            try
            {
                using (var statement = db.Prepare("SELECT Songid, Songfilename, Songname, Singer, user FROM Songs"))
                {
                    while (statement.Step() == SQLiteResult.ROW)
                    {
                        if (login_username == (string)statement[4])
                        {
                            _songid = (long)statement[0];
                            _songfilename = (string)statement[1];
                            _songname = (string)statement[2];
                            _singer = (string)statement[3];
                            this.allItems.Add(new Models.SongItem((int)_songid, _songfilename, _songname, _singer, null));
                        }

                        //this.AllItems.ElementAt(this.AllItems.Count - 1).songid = (int)_songid;
                    }
                }
            }
            catch (Exception ex)
            {
                var i = new MessageDialog(ex.ToString()).ShowAsync();
            }
        }

        public void AddSongItem(int _songid, string _songfilename, string _songname, string _singer, BitmapImage _singer_photo)
        {
            this.allItems.Add(new Models.SongItem(_songid, _songfilename, _songname, _singer, _singer_photo));
        }

        public void DeleteSongItem()
        {
            string login_username = NewPage.user;
            int temp = this.SelectItems.songid;
            this.allItems.Remove(this.SelectItems);
            this.selectItems = null;

            var db = App.conn2;

            for (int i = temp; i < AllItems.Count; i++)
            {
                AllItems.ElementAt(i).songid = i;
                try
                {
                    using (var cust = db.Prepare("UPDATE Songs SET Songid = ? WHERE Songname = ? AND user = ?"))
                    {
                        cust.Bind(1, i);
                        cust.Bind(2, AllItems.ElementAt(i).songname);
                        cust.Bind(3, login_username);
                        cust.Step();
                    }
                }
                catch (Exception ex)
                {
                    var ii = new MessageDialog(ex.ToString()).ShowAsync();
                }
            }
        }
    }
}
