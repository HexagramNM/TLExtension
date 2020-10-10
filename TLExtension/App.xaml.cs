//�X�}�[�g�t�H���p�̃����N�̃X�^�C��
//https://gray-code.com/html_css/setting-style-of-link-for-tap-by-smartphone-or-tablet/

//icon�̕t����
//http://www.kurigohan.com/article/20180209_xamarin_forms_icon.html

//javascript���N�����āA�y�[�W��html���擾���鏈���̏��
//https://github.com/xamarin/xamarin-forms-samples/blob/master/CustomRenderers/HybridWebView/Droid/HybridWebViewRenderer.cs

//Xamarin.Forms�ł̒ʒm�̂���
//https://itblogdsi.blog.fc2.com/blog-entry-145.html

//Xamarin.Forms�ł̃N���X�Ԃ̂��Ƃ�
//https://forums.xamarin.com/discussion/92429/how-to-update-label-text-from-another-class

//�L�[�{�[�h���\�����ꂽ�Ƃ��ɁA�����B��Ȃ��悤��WebView���k�߂āA�X�N���[���Ō����悤�ɂ�����@
//https://qiita.com/amay077/items/6fcdec829a96bc604532

//Url����摜��Byte�����擾������@�iHttpClient�j
//https://stackoverflow.com/questions/41337487/how-to-download-image-from-url-and-save-it-to-a-local-sqlite-database

//PCLStorage��p�����摜�̕ۑ�
//https://www.c-sharpcorner.com/article/local-file-storage-using-xamarin-form/

//DCIM�̃p�X�擾
//https://forums.xamarin.com/discussion/175085/i-need-to-save-ad-in-image-in-dcim

//�ʃu���E�U��URL���J�����@
//https://itblogdsi.blog.fc2.com/blog-entry-171.html

//FormsWebChromeClient�֘A�i�t���X�N���[����OpenDocument��FormsWebChromeClient�N���X���g���̂�
//��������B���܂��A����N���X���ł܂Ƃ߂�K�v����B�j

//�t���X�N���[���̏��
//https://github.com/mhaggag/XFAndroidFullScreenWebView

/* �t���X�N���[���֘A�̒��쌠�\��
Copyright (c) 2018 Muhammad Haggag

Released under the MIT License

https://github.com/mhaggag/XFAndroidFullScreenWebView/blob/master/LICENSE.md

*/

//HTML��<input>���N���b�N���AOpenDocument���Ăяo���ۂ�Intent���C��������@
//�i�ǂݍ��ނׂ��t�@�C���^�C�v�̕ύX�j
//https://github.com/kwmt/WebViewInputSample

//�L���b�V���̍폜
//https://www.project-respite.com/no-cached-webview/

//�A�v�������N�̂����iTwitter�����N���A�v���Ɋ֘A�t������@�j
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

    //DependencyService���痘�p����
    public interface INotificationService
    {
        //iOS�p�̓o�^
        void Regist();
        //�ʒm����
        void On(string title, string body, int id);
        //�ʒm����������
        void Off(int id);
    }

    public partial class App : Xamarin.Forms.Application
    {
        public static OAuth.OAuthSession s = null;
        public static Tokens t = null;

        //Consumer Key, Consumer Secret���쐬���A�����ɓ��͂��Ă��������B
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

            //�ʒm�iApp���ňꊇ�Ǘ��j
            DependencyService.Get<INotificationService>().Regist();

            //���łɔF�؂��Ă��邩�m�F
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
                //�F�؂��Ă��Ȃ��ꍇ�͔F�؂��s���B
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
                //�F�؂��Ă��Ȃ��ꍇ�͔F�؂��s���B
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
