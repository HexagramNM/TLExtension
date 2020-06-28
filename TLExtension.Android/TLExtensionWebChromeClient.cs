using Android.Graphics;
using Android.Webkit;
using System;
using Android.Content;
using Android.Views;
using Xamarin.Forms.Platform.Android;


namespace TLExtension.Droid
{

    public class EnterFullScreenRequestedEventArgs : EventArgs
    {
        public View View { get; }
        public EnterFullScreenRequestedEventArgs(View view)
        {
            View = view;
        }

    }

    public class TLExtensionWebChromeClient : FormsWebChromeClient
    {
        static public MainActivity mainActivity;

        public const int REQUEST_IMAGE_CODE = 5556;

        public event EventHandler<EnterFullScreenRequestedEventArgs> EnterFullScreenRequested;

        public event EventHandler ExitFullScreenRequested;

        public TLExtensionWebChromeClient()
        {
        }

        public override bool OnShowFileChooser(Android.Webkit.WebView webView, IValueCallback filePathCallback, FileChooserParams fileChooserParams)
        {
            Intent intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("*/*");
            intent.PutExtra(Intent.ExtraMimeTypes, fileChooserParams.GetAcceptTypes());
            mainActivity.intentCallback = filePathCallback;
            mainActivity.StartActivityForResult(intent, REQUEST_IMAGE_CODE);

            return true;
        }

        public override void OnHideCustomView()
        {
            ExitFullScreenRequested?.Invoke(this, EventArgs.Empty);
        }

        public override void OnShowCustomView(View view, ICustomViewCallback callback)
        {
            EnterFullScreenRequested?.Invoke(this, new EnterFullScreenRequestedEventArgs(view));
        }
    }
}