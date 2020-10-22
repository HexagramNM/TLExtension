using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Forms;

[assembly: Dependency(typeof(TLExtension.Droid.ClipBoardService))]
namespace TLExtension.Droid
{
    public class ClipBoardService: IClipBoardService
    {
        public void copy(string copyText)
        {
            ClipboardManager clipboard = (ClipboardManager)Forms.Context.GetSystemService(Context.ClipboardService);
            ClipData clip = ClipData.NewPlainText("TLExtension", copyText);
            clipboard.PrimaryClip = clip;
        }
    }
}