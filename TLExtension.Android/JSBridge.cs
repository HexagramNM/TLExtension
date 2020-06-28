using System;
using Android.Webkit;
using Java.Interop;

namespace TLExtension.Droid
{
    public class JSBridge : Java.Lang.Object
    {
        readonly WeakReference<TLExtensionWebViewRenderer> renderer;

        public JSBridge(TLExtensionWebViewRenderer inputRenderer)
        {
            renderer = new WeakReference<TLExtensionWebViewRenderer>(inputRenderer);
        }

        [JavascriptInterface]
        [Export("invokeAction")]
        public void InvokeAction(string data)
        {
            TLExtensionWebViewRenderer instanceRenderer;

            if (renderer != null && renderer.TryGetTarget(out instanceRenderer))
            {
                ((TLExtensionWebView)instanceRenderer.Element).InvokeAction(data);
            }
        }
    }
}
