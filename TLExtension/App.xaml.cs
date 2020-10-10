//スマートフォン用のリンクのスタイル
//https://gray-code.com/html_css/setting-style-of-link-for-tap-by-smartphone-or-tablet/

//iconの付け方
//http://www.kurigohan.com/article/20180209_xamarin_forms_icon.html

//javascriptを起動して、ページのhtmlを取得する処理の情報
//https://github.com/xamarin/xamarin-forms-samples/blob/master/CustomRenderers/HybridWebView/Droid/HybridWebViewRenderer.cs

//Xamarin.Formsでの通知のやり方
//https://itblogdsi.blog.fc2.com/blog-entry-145.html

//Xamarin.Formsでのクラス間のやりとり
//https://forums.xamarin.com/discussion/92429/how-to-update-label-text-from-another-class

//キーボードが表示されたときに、下が隠れないようにWebViewを縮めて、スクロールで見れるようにする方法
//https://qiita.com/amay077/items/6fcdec829a96bc604532

//Urlから画像のByte情報を取得する方法（HttpClient）
//https://stackoverflow.com/questions/41337487/how-to-download-image-from-url-and-save-it-to-a-local-sqlite-database

//PCLStorageを用いた画像の保存
//https://www.c-sharpcorner.com/article/local-file-storage-using-xamarin-form/

//DCIMのパス取得
//https://forums.xamarin.com/discussion/175085/i-need-to-save-ad-in-image-in-dcim

//別ブラウザでURLを開く方法
//https://itblogdsi.blog.fc2.com/blog-entry-171.html

//FormsWebChromeClient関連（フルスクリーンとOpenDocumentはFormsWebChromeClientクラスを使うので
//競合する。うまく、同一クラス内でまとめる必要あり。）

//フルスクリーンの情報
//https://github.com/mhaggag/XFAndroidFullScreenWebView

/* フルスクリーン関連の著作権表示
Copyright (c) 2018 Muhammad Haggag

Released under the MIT License

https://github.com/mhaggag/XFAndroidFullScreenWebView/blob/master/LICENSE.md

*/

//HTMLの<input>をクリックし、OpenDocumentを呼び出す際のIntentを修正する方法
//（読み込むべきファイルタイプの変更）
//https://github.com/kwmt/WebViewInputSample

//キャッシュの削除
//https://www.project-respite.com/no-cached-webview/

//アプリリンクのやり方（Twitterリンクをアプリに関連付ける方法）
//https://qiita.com/HisakoIsaka/items/1fe496741d47d5b1dfdd
//https://chomado.com/programming/c-sharp/xamarin-android-launch-with-url-scheme/

using Android.Content.Res;
using CoreTweet;
using CoreTweet.Core;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
namespace TLExtension
{
    public delegate void StartEventHandler();

    public delegate void StopEventHandler();

    public delegate void RestartEventHandler();

    public delegate void AuthorizedEventHandler();

    //DependencyServiceから利用する
    public interface INotificationService
    {
        //iOS用の登録
        void Regist();
        //通知する
        void On(string title, string body, int id);
        //通知を解除する
        void Off(int id);
    }

    public partial class App : Xamarin.Forms.Application
    {
        public static OAuth.OAuthSession s = null;
        public static Tokens t = null;

        //Consumer Key, Consumer Secretを作成し、ここに入力してください。
        private static String cKey = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
        private static String cSecret = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
        private static String accessTokenFilePath = "/accessSetting.txt";
        private static App currentApp = null;

        private StartEventHandler started;
        private StopEventHandler stopped;
        private RestartEventHandler restarted;
        private AuthorizedEventHandler authorized;

        private TabbedPage1 thisTabbedPage;
        private List<View> stackSettingView;

        public App()
        {
            currentApp = this;
            started = new StartEventHandler(() => { });
            stopped = new StopEventHandler(() => { });
            restarted = new RestartEventHandler(() => { });
            authorized = new AuthorizedEventHandler(() => { });
            stackSettingView = new List<View>();

            InitializeComponent();

            thisTabbedPage = new TabbedPage1();
            thisTabbedPage.On<Xamarin.Forms.PlatformConfiguration.Android>().SetIsSwipePagingEnabled(true);

            MainPage = thisTabbedPage;

            //通知（App側で一括管理）
            DependencyService.Get<INotificationService>().Regist();

            //すでに認証しているか確認
            string aToken = "";
            string aSecret = "";
            string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + accessTokenFilePath;
            if (File.Exists(path))
            {
                StreamReader readFile = new StreamReader(path, Encoding.GetEncoding("utf-16"));
                aToken = readFile.ReadLine();
                aSecret = readFile.ReadLine();
                readFile.Close();
                t = CoreTweet.Tokens.Create(cKey, cSecret, aToken, aSecret);
                try
                {
                    UserResponse result = t.Account.VerifyCredentials();
                    t.UserId = (long)result.Id;
                    t.ScreenName = result.ScreenName;
                }
                catch (Exception e)
                {
                    t = null;
                }
            }

            if (t == null)
            {
                //認証していない場合は認証を行う。
                (getContentPage("MainBrowser") as MainBrowser).web.authorizeAPI(cKey, cSecret);
            }
            else
            {
                (getContentPage("MainBrowser") as MainBrowser).web.twitterStart();
                authorized();
            }
        }

        public void setStartLink(string uri)
        {
            (getContentPage("MainBrowser") as MainBrowser).web.startLink = uri;
            if (t == null)
            {
                //認証していない場合は認証を行う。
                (getContentPage("MainBrowser") as MainBrowser).web.authorizeAPI(cKey, cSecret);
            }
            else
            {
                (getContentPage("MainBrowser") as MainBrowser).web.twitterStart();
            }
        }

        public static void addCustomSetting(View view)
        {
            currentApp.stackSettingView.Add(view);
        }

        public static List<View> getCurrentCustomSetting()
        {
            return currentApp.stackSettingView;
        }

        public static void registerStartEvent(StartEventHandler action)
        {
            currentApp.started += action;
        }

        public static void registerStopEvent(StopEventHandler action)
        {
            currentApp.stopped += action;
        }

        public static void registerRestartEvent(RestartEventHandler action)
        {
            currentApp.restarted += action;
        }

        public static void SetEnableSwipePaging (bool value) {
            currentApp.thisTabbedPage.On<Xamarin.Forms.PlatformConfiguration.Android>().SetIsSwipePagingEnabled(value);
        }

        public static Page getContentPage(string className)
        {
            Page result = null;
            foreach (Page page in currentApp.thisTabbedPage.Children)
            {
                if (page.GetType().Name == className)
                {
                    result = page;
                    break;
                }
            }
            return result;
        }

        public static void openUrl(string url)
        {
            (getContentPage("MainBrowser") as MainBrowser).web.Source = url;
            openBrowser();
        }

        public static void openBrowser()
        {
            currentApp.thisTabbedPage.CurrentPage = getContentPage("MainBrowser");
        }

        public static void registerAuthorizedEvent(AuthorizedEventHandler action)
        {
            currentApp.authorized += action;
        }

        public static void authorizing(String code)
        {
            t = s.GetTokens(code);
            string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + accessTokenFilePath;
            StreamWriter writeFile = new StreamWriter(path, false, Encoding.GetEncoding("utf-16"));
            writeFile.WriteLine(t.AccessToken);
            writeFile.WriteLine(t.AccessTokenSecret);
            writeFile.Close();
            currentApp.authorized();
        }

        public static void reauthorize()
        {
            (getContentPage("MainBrowser") as MainBrowser).web.authorizeAPI(cKey, cSecret);
            openBrowser();
        }

        public static void notify(string title, string body, int id)
        {
            DependencyService.Get<INotificationService>().On(title, body, id);
        }

        protected override void OnStart ()
		{
            // Handle when your app starts
            started();
        }

		protected override void OnSleep ()
		{
            // Handle when your app sleeps
            stopped();
        }

		protected override void OnResume ()
		{
            // Handle when your app resumes
            restarted();
        }
	}
}
