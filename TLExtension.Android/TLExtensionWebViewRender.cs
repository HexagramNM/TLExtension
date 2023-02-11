using TLExtension;
using TLExtension.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Android.Content;
using Android.Views;
using System;
using System.IO;
using Android.Provider;
using Java.IO;
using Android.OS;
using PCLStorage;
using static System.Net.WebRequestMethods;

[assembly: ExportRenderer(typeof(TLExtensionWebView), typeof(TLExtensionWebViewRenderer))]
namespace TLExtension.Droid
{
    public class TLExtensionWebViewRenderer : WebViewRenderer
    {
        const string JavascriptFunction = "function invokeCSharpAction(data){jsBridge.invokeAction(data);} " +
            "function autoTweet(text) { if (document.body.getElementsByTagName(\"textarea\")[0].value == \"\") {document.body.getElementsByTagName(\"textarea\")[0].value = text;}}";
        Context _context;
        private TLExtensionWebView _webView;
        const string saveMediaFolderName = "TLExtensionMedia";
        private JavascriptWebViewClient _webJSClient;
        private TLExtensionWebChromeClient webChromeClient;

        public TLExtensionWebViewRenderer(Context context) : base(context)
        {
            _context = context;
            webChromeClient = new TLExtensionWebChromeClient();
            webChromeClient.EnterFullScreenRequested += OnEnterFullScreenRequested;
            webChromeClient.ExitFullScreenRequested += OnExitFullScreenRequested;
            _webJSClient = new JavascriptWebViewClient($"javascript: {JavascriptFunction}", this);
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.WebView> e)
        {
            base.OnElementChanged(e);
            if (Control != null)
            {
                Control.ClearCache(true);
                Control.Settings.SetAppCacheEnabled(false);
                Control.Settings.CacheMode = Android.Webkit.CacheModes.NoCache;

                //この設定が無いと外部リンクが正常に開かれなくなる。
                Control.Settings.SetSupportMultipleWindows(false);
            }
            if (e.OldElement != null)
            {
                Control.RemoveJavascriptInterface("jsBridge");
                ((TLExtensionWebView)Element).Cleanup();
            }
            if (e.NewElement != null)
            {
                Control.SetWebViewClient(_webJSClient);
                Control.SetWebChromeClient(webChromeClient);
                Control.AddJavascriptInterface(new JSBridge(this), "jsBridge");
            }
            _webView = (TLExtensionWebView)e.NewElement;
            _webView.reloadAction = new Action(() => { Control.Reload(); });
            _webView.clearHistoryAction = new Action(() => { Control.ClearHistory(); });
            _webView.clearCacheAction = new Action(() => { Control.ClearCache(true); });
            _webView.saveMediaAction = saveMedia;
            _webView.switchKeepOn = new Action<bool>((bool state) =>
            {
                if (state) {
                    (_context as Android.App.Activity).Window.AddFlags(WindowManagerFlags.KeepScreenOn);
                }
                else
                {
                    (_context as Android.App.Activity).Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
                }
            });
            
        }

        private void saveMedia(string fileName, byte[] data)
        {
            if (Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Q)
            {
                saveMediaForOldAndroid(fileName, data);
            }
            else
            {
                saveMediaForAndroid10(fileName, data);
            }
        }

        private void saveMediaForAndroid10(string fileName, byte[] data)
        {
            ContentValues cv = new ContentValues();
            bool isVideo = false;
            Android.Net.Uri collection = null;
            int currentTimeStamp = (int)(Java.Lang.JavaSystem.CurrentTimeMillis() / 1000);
            int currentTimeStampMills = (int)Java.Lang.JavaSystem.CurrentTimeMillis();

            if (fileName.Contains(".mp4"))
            {
                isVideo = true;
            }

            if (isVideo)
            {
                cv.Put(MediaStore.Video.Media.InterfaceConsts.DisplayName, fileName);
                cv.Put(MediaStore.Video.Media.InterfaceConsts.MimeType, "video/mp4");
                cv.Put(MediaStore.Video.Media.InterfaceConsts.IsPending, 1);
                cv.Put(MediaStore.Video.Media.InterfaceConsts.RelativePath, Android.OS.Environment.DirectoryDcim + "/" + saveMediaFolderName);
                collection = MediaStore.Video.Media.GetContentUri(MediaStore.VolumeExternalPrimary);
            }
            else
            {
                cv.Put(MediaStore.Images.Media.InterfaceConsts.DisplayName, fileName);
                cv.Put(MediaStore.Images.Media.InterfaceConsts.MimeType, "image/jpg");
                cv.Put(MediaStore.Images.Media.InterfaceConsts.IsPending, 1);
                cv.Put(MediaStore.Images.Media.InterfaceConsts.RelativePath, Android.OS.Environment.DirectoryDcim + "/" + saveMediaFolderName);
                collection = MediaStore.Images.Media.GetContentUri(MediaStore.VolumeExternalPrimary);
            }

            ContentResolver resolver = Context.ContentResolver;
            Android.Net.Uri item = resolver.Insert(collection, cv);

            using (System.IO.Stream os = resolver.OpenOutputStream(item))
            {
                os.Write(data, 0, data.Length);
            }

            cv.Clear();
            if(isVideo)
            {
                cv.Put(MediaStore.Video.Media.InterfaceConsts.IsPending, 0);
                cv.Put(MediaStore.Video.Media.InterfaceConsts.DateAdded, currentTimeStamp);
                cv.Put(MediaStore.Video.Media.InterfaceConsts.DateModified, currentTimeStamp);
                cv.Put(MediaStore.Video.Media.InterfaceConsts.DateTaken, currentTimeStampMills);
            }
            else
            {
                cv.Put(MediaStore.Images.Media.InterfaceConsts.IsPending, 0);
                cv.Put(MediaStore.Images.Media.InterfaceConsts.DateAdded, currentTimeStamp);
                cv.Put(MediaStore.Images.Media.InterfaceConsts.DateModified, currentTimeStamp);
                cv.Put(MediaStore.Images.Media.InterfaceConsts.DateTaken, currentTimeStampMills);
            }
            resolver.Update(item, cv, null, null);
        }

        private async void saveMediaForOldAndroid(string fileName, byte[] data)
        {
            string DCIMPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDcim).AbsolutePath;
            IFolder DCIMFolder = await FileSystem.Current.GetFolderFromPathAsync(DCIMPath);
            IFolder saveFolder;
            ExistenceCheckResult exist = await DCIMFolder.CheckExistsAsync(saveMediaFolderName);
            if (exist == ExistenceCheckResult.FolderExists)
            {
                saveFolder = await DCIMFolder.GetFolderAsync(saveMediaFolderName);
            }
            else
            {
                saveFolder = await DCIMFolder.CreateFolderAsync(saveMediaFolderName, CreationCollisionOption.ReplaceExisting);
            }
            IFile file = await saveFolder.CreateFileAsync(saveMediaFolderName, CreationCollisionOption.ReplaceExisting);
            using (System.IO.Stream stream = await file.OpenAsync(PCLStorage.FileAccess.ReadAndWrite))
            {
                stream.Write(data, 0, data.Length);
            }
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(file.Path);
            DateTime nowTime = DateTime.Now;
            fileInfo.CreationTime = nowTime;
            fileInfo.LastWriteTime = nowTime;
            fileInfo.LastAccessTime = nowTime;
            fileInfo.Refresh();
            _context.SendBroadcast(new Intent(Intent.ActionMediaScannerScanFile, Android.Net.Uri.Parse("file://" + file.Path)));
        }

        //フルスクリーンにするための追加メソッド
        protected override FormsWebChromeClient GetFormsWebChromeClient()
        {
            return webChromeClient;
        }

        private void OnEnterFullScreenRequested(object sender, EnterFullScreenRequestedEventArgs eventArgs)
        {
            if (_webView.EnterFullScreenCommand != null && _webView.EnterFullScreenCommand.CanExecute(null))
            {
                _webView.EnterFullScreenCommand.Execute(eventArgs.View.ToView());
            }
        }

        private void OnExitFullScreenRequested(object sender, EventArgs eventArgs)
        {
            if (_webView.ExitFullScreenCommand != null && _webView.ExitFullScreenCommand.CanExecute(null))
            {
                _webView.ExitFullScreenCommand.Execute(null);
            }
        }

    }
}
