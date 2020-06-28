using Android.Graphics;
using Android.Webkit;
using System;
using Xamarin.Forms;
using Android.Content;

[assembly: Dependency(typeof(WebBrowserService))]
public class WebBrowserService
{
    public void Open(Uri uri)
    {
        Forms.Context.StartActivity(
            new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(uri.AbsoluteUri)));
    }
}

namespace TLExtension.Droid
{
    public class JavascriptWebViewClient : WebViewClient
    {
        string _javascript;
        TLExtensionWebViewRenderer _renderer;
        bool openBrowser;

        public JavascriptWebViewClient(string javascript, TLExtensionWebViewRenderer renderer)
        {
            _javascript = javascript;
            _renderer = renderer ?? throw new ArgumentNullException("renderer");
            openBrowser = false;
        }

        public override void OnPageStarted(Android.Webkit.WebView view, string url, Bitmap favicon)
        {
            if (url.Contains("twitter.com")) {
                base.OnPageStarted(view, url, favicon);
                var args = new WebNavigatingEventArgs(WebNavigationEvent.NewPage, new UrlWebViewSource { Url = url }, url);
                _renderer.Element.SendNavigating(args);
            }
            else
            {
                view.GoBack();
                if (!openBrowser)
                {
                    DependencyService.Get<WebBrowserService>().Open(new Uri(url));
                    openBrowser = true;
                }
            }
           
        }

        public override void OnPageFinished(Android.Webkit.WebView view, string url)
        {
            base.OnPageFinished(view, url);
            view.EvaluateJavascript(_javascript, null);
            var source = new UrlWebViewSource { Url = url };
            var args = new WebNavigatedEventArgs(WebNavigationEvent.NewPage, source, url, WebNavigationResult.Success);
            _renderer.Element.SendNavigated(args);
            openBrowser = false;
        }
    }
}