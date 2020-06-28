using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace TLExtension
{
    public partial class CustomizedContentPage: ContentPage
    {
        protected bool authorized;

        public CustomizedContentPage()
        {
            authorized = false;
            App.registerAuthorizedEvent(() => { authorized = true; });
        }

        protected override bool OnBackButtonPressed()
        {
            App.openBrowser();
            return true;
        }
    }
}
