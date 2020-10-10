using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using Android.Webkit;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android;

namespace TLExtension.Droid
{
    [Activity(Label = "TLExtension", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    [IntentFilter(new[] {Intent.ActionView},
        Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault },
        DataScheme = "https",
        DataHost = "mobile.twitter.com",
        DataPathPrefix = "",
        AutoVerify = true
        )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private App app;
        public IValueCallback intentCallback;

        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            TLExtensionWebChromeClient.mainActivity = this;

            global::Xamarin.Forms.Forms.Init(this, bundle);
            app = new App();
            LoadApplication(app);

            app.On<Xamarin.Forms.PlatformConfiguration.Android>().
                UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Resize);

            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) != (int)Permission.Granted
                && ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) != (int)Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage }, 26);
            }
            else if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) != (int)Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.ReadExternalStorage }, 26);
            }
            else if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) != (int)Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.WriteExternalStorage }, 26);
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == 26)
            {
                for (int i = 0; i < grantResults.Length; i++)
                {
                    if (grantResults[i] != Permission.Granted)
                    {
                        Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                    }
                }
            }
            else
            {
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent resultData)
        {
            if (requestCode == TLExtensionWebChromeClient.REQUEST_IMAGE_CODE)
            {
                if (resultCode == Result.Ok)
                {
                    intentCallback.OnReceiveValue(new Android.Net.Uri[] { resultData.Data });
                    intentCallback = null;
                }
                else if (resultCode == Result.Canceled)
                {
                    intentCallback.OnReceiveValue(null);
                    intentCallback = null;
                }
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            Intent intent = this.Intent;
            if (Intent.ActionView.Equals(intent.Action))
            {
                Android.Net.Uri uri = intent.Data;
                if (uri != null)
                {
                    app.setStartLink(uri.ToString());
                }
            }
        }
    }

}

