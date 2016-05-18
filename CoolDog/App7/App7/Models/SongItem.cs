using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace App7.Models
{
    class SongItem
    {
        public int songid;

        public string songfilename { get; set; }

        public string songname { get; set; }

        public string singer { get; set; }

        public BitmapImage singer_photo { get; set; }

        public ImageSource ImagePath { get; set; }

        //public bool? completed { get; set; }

        public SongItem(int _songid, string _songfilename, string _songname, string _singer, BitmapImage _singer_photo)
        {
            songid = _songid;
            songfilename = _songfilename;
            songname = _songname;
            singer = _singer;
            singer_photo = _singer_photo;
            //completed = _com;

            //if (photo != null)
            //    ImagePath = photo;
            //else
            //{
            //    photo = new BitmapImage();
            //    photo.DecodePixelWidth = 600;
            //    photo.UriSource = new Uri("ms-appx:///Assets/background.jpg");
            //    ImagePath = photo;
            //}
        }

        
    }
}
