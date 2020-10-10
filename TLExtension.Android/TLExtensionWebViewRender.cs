using TLExtension;
using TLExtension.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Android.Content;
using Android.Views;
using System;
using System.IO;

[assembly: ExportRenderer(typeof(TLExtensionWebView), typeof(TLExtensionWebViewRenderer))]
namespace TLExtension.Droid
{
    public class TLExtensionWebViewRenderer : WebViewRenderer
    {
        const string JavascriptFunction = "function invokeCSharpAction(data){jsBridge.invokeAction(data);}";
        Context _context;
        private TLExtensionWebView _webView;
        private TLExtensionWebChromeClient webClient;

        public TLExtensionWebViewRenderer(Context context) : base(context)
        {
            _context = context;
            webClient = new TLExtensionWebChromeClient();
            webClient.EnterFullScreenRequested += OnEnterFullScreenRequested;
            webClient.ExitFullScreenRequested += OnExitFullScreenRequested;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.WebView> e)
        {
            base.OnElementChanged(e);
            if (Control != null)
            {
                Control.ClearCache(true);
                Control.Settings.SetAppCacheEnabled(false);
                Control.Settings.CacheMode = Android.Webkit.CacheModes.NoCache;
            }
            if (e.OldElement != null)
            {
                Control.RemoveJavascriptInterface("jsBridge");
                ((TLExtensionWebView)Element).Cleanup();
            }
            if (e.NewElement != null)
            {
                Control.SetWebViewClient(new JavascriptWebViewClient($"javascript: {JavascriptFunction}", this));
                Control.SetWebChromeClient(webClient);
                Control.AddJavascriptInterface(new JSBridge(this), "jsBridge");
            }
            _webView = (TLExtensionWebView)e.NewElement;
            _webView.reloadAction = new Action(() => { Control.Reload(); });
            _webView.clearHistoryAction = new Action(() => { Control.ClearHistory(); });
            _webView.imageDownloadBloadcastAction = new Action<String>((string imagePath) => {
                _context.SendBroadcast(new Intent(Intent.ActionMediaScannerScanFile, Android.Net.Uri.Parse("file://" + imagePath)));
            }); 
            _webView.DCIMPath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, "DCIM");
            
        }

        //フルスクリーンにするための追加メソッド
        protected override FormsWebChromeClient GetFormsWebChromeClient()
        {
            return webClient;
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
