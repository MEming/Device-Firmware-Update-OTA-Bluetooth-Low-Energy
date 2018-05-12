using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content.Res;
using System.IO;

namespace DFU.Droid
{
    [Activity(Label = "DFU", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private string hexFile;

        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);
            int APILEVEL = Convert.ToInt16(Build.VERSION.SdkInt);
            if (APILEVEL >= 21)
            {
                Window window = this.Window;
                window.ClearFlags(WindowManagerFlags.TranslucentStatus);
                window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
                window.SetStatusBarColor(Android.Graphics.Color.Rgb(37, 50, 56));
            }
            AssetManager assets = this.Assets;
            using (StreamReader sr = new StreamReader(assets.Open("sde.txt")))
            {
                hexFile = sr.ReadToEnd();
            }
            LoadApplication(new App(hexFile));
        }
    }
}

