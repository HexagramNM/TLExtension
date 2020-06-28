using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Timers;
using System.Threading.Tasks;
using Xamarin.Forms;
using Android.Content.Res;
using Xamarin.Forms.StyleSheets;
using CoreTweet;
using System.Windows.Input;
using System.Net.Http;
using PCLStorage;
using CoreTweet.Core;

namespace TLExtension
{
    //ツイッター内のページ遷移だと、Navigateイベントが反応しないため、自作
    public delegate void TwitterHTMLChangedEventHandler(string url, string HTML);

    public class TLExtensionWebView : WebView
    {
        public Action reloadAction = null;
        public Action clearHistoryAction = null;
        public Action<String> imageDownloadBloadcastAction = null;
        public ActivityIndicator indicator;
        Action<string> action;
        public string DCIMPath;
        HttpClient httpClient;
        //Timer CRTVTimer;
        string CRTVjs;
        Timer HTMLGetterTimer;

        public bool invoked { get; set; } = false;

        public static readonly BindableProperty UriProperty = BindableProperty.Create(
            propertyName: "Uri",
            returnType: typeof(string),
            declaringType: typeof(TLExtensionWebView),
            defaultValue: default(string));

        public string Uri
        {
            get { return (string)GetValue(UriProperty); }
            set { SetValue(UriProperty, value); }
        }

        public static readonly BindableProperty EnterFullScreenCmmandProperty =
            BindableProperty.Create(
                propertyName: "EnterFullScreenCommand",
                returnType: typeof(ICommand),
                declaringType: typeof(TLExtensionWebView),
                defaultValue: new Command(async (view) => await DefaultEnterAsync((View)view))
                );

        public ICommand EnterFullScreenCommand
        {
            get => (ICommand)GetValue(EnterFullScreenCmmandProperty);
            set => SetValue(EnterFullScreenCmmandProperty, value);
        }

        public static readonly BindableProperty ExitFullScreenCmmandProperty =
            BindableProperty.Create(
                propertyName: "ExitFullScreenCommand",
                returnType: typeof(ICommand),
                declaringType: typeof(TLExtensionWebView),
                defaultValue: new Command(async (view) => await DefaultExitAsync())
                );

        public ICommand ExitFullScreenCommand
        {
            get => (ICommand)GetValue(ExitFullScreenCmmandProperty);
            set => SetValue(ExitFullScreenCmmandProperty, value);
        }

        public TLExtensionWebView(ActivityIndicator i_indicator)
        {
            twitterHTMLChanged = new TwitterHTMLChangedEventHandler((string url, string HTML) => { });

            httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            HTMLGetterTimer = new Timer(100);
            HTMLGetterTimer.Elapsed += (object s, ElapsedEventArgs e) => { getTwitterHTMLAndUrl(); };

            AssetManager assets = Forms.Context.Assets;
            
            using (StreamReader sr = new StreamReader(assets.Open("CRTV_main.js")))
            {
                CRTVjs = sr.ReadToEnd();
            }
            indicator = i_indicator;

            App.registerAuthorizedEvent(() => {
                App.registerStopEvent(() => { HTMLGetterTimer.Stop(); });
                App.registerRestartEvent(() => { HTMLGetterTimer.Start(); });
                HTMLGetterTimer.Start();
            });
        }

        //webview上のページに対してjavascriptを実行するためのメソッド
        public void RegisterAction(Action<string> callback)
        {
            action = callback;
        }

        public void Cleanup()
        {
            action = null;
        }

        public void InvokeAction(string data)
        {
            if (action == null || data == null)
            {
                return;
            }
            action.Invoke(data);
            Cleanup();
            invoked = true;
        }
        //webview上のページに対してjavascriptを実行するためのメソッド　ここまで

        //リロードや履歴の消去
        public void Reload()
        {
            //キーボードがもとに戻らないときの対策
            IsVisible = false;
            reloadAction?.Invoke();
            IsVisible = true;
        }

        public void clearHistory()
        {
            clearHistoryAction?.Invoke();
        }
        //リロードや履歴の消去　ここまで

        //urlを経由したメディアダウンロード
        public async Task<bool> downloadMediaFromTwitter(string downloadUrl, string saveFileName)
        {
            byte[] imageData;
            string saveFolderName = "TLExtensionMedia";
            using (HttpResponseMessage httpResponse = await httpClient.GetAsync(downloadUrl))
            {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    imageData = await httpResponse.Content.ReadAsByteArrayAsync();
                    IFolder DCIMFolder = await FileSystem.Current.GetFolderFromPathAsync(DCIMPath);
                    IFolder saveFolder;
                    ExistenceCheckResult exist = await DCIMFolder.CheckExistsAsync(saveFolderName);
                    if (exist == ExistenceCheckResult.FolderExists)
                    {
                        saveFolder = await DCIMFolder.GetFolderAsync(saveFolderName);
                    }
                    else
                    {
                        saveFolder = await DCIMFolder.CreateFolderAsync(saveFolderName, CreationCollisionOption.ReplaceExisting);
                    }
                    IFile file = await saveFolder.CreateFileAsync(saveFileName, CreationCollisionOption.ReplaceExisting);
                    using (System.IO.Stream stream = await file.OpenAsync(PCLStorage.FileAccess.ReadAndWrite))
                    {
                        stream.Write(imageData, 0, imageData.Length);
                    }
                    System.IO.FileInfo fileInfo = new System.IO.FileInfo(file.Path);
                    DateTime nowTime = DateTime.Now;
                    fileInfo.CreationTime = nowTime;
                    fileInfo.LastWriteTime = nowTime;
                    fileInfo.LastAccessTime = nowTime;
                    fileInfo.Refresh();

                    //メディアへの通知
                    imageDownloadBloadcastAction?.Invoke(file.Path);
                    return true;
                    
                }
                return false;
            }
        }

        //urlを経由したメディアダウンロード　ここまで

        //フルスクリーンを実現するためのメソッド
        private static async Task DefaultEnterAsync(View view)
        {
            var page = new ContentPage
            {
                Content = view
            };
            await Application.Current.MainPage.Navigation.PushModalAsync(page);
        }

        private static async Task DefaultExitAsync()
        {
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }
        //フルスクリーンを実現するためのメソッド　ここまで

        //認証
        public void authorizeAPI(String cKey, String cSecret)
        {
            App.s = OAuth.Authorize(cKey, cSecret);
            Navigating += authorizingNavigatedEvent;
            Navigated += authorizedNavigatedEvent;
            Source = App.s.AuthorizeUri.ToString();
        }

        private void AuthProcess()
        {
            string authSource = "";
            RegisterAction(data => { authSource = data; });
            Eval("javascript:invokeCSharpAction(document.documentElement.outerHTML);");
            while (!invoked) ;
            invoked = false;
            int codeIndex = authSource.IndexOf("<code>");
            if (codeIndex >= 0)
            {
                String code = authSource.Substring(codeIndex + 6, 7);
                Source = "https://mobile.twitter.com/home/";
                App.authorizing(code);
            }
        }

        private void authorizingNavigatedEvent(object source, WebNavigatingEventArgs e)
        {
            if (e.Url == @"https://api.twitter.com/oauth/authorize")
            {
                IsVisible = false;
                indicator.IsVisible = true;
                indicator.IsRunning = true;
            }
        }

        private void authorizedNavigatedEvent(object source, WebNavigatedEventArgs e)
        {
            if (e.Url == @"https://api.twitter.com/oauth/authorize")
            {
                AuthProcess();
            }
            else if (e.Url == @"https://mobile.twitter.com/home/")
            {
                indicator.IsVisible = false;
                indicator.IsRunning = false;
                IsVisible = true;
                clearHistory();
                Navigating -= authorizingNavigatedEvent;
                Navigated -= authorizedNavigatedEvent;
            }
        }
        //認証ここまで

        //ツイッター内リンク管理
        private string previousHTML = "";
        private string currentHTML = "";
        public string twitterHTML { get { return currentHTML; } }

        private string previousUrl = "";
        private string currentUrl = "";
        public string twitterUrl { get { return currentUrl; } }

        public TwitterHTMLChangedEventHandler twitterHTMLChanged;

        public static string getSubstringBetweenStartAndEnd(string source, string start, string end, bool includeStart, bool includeEnd)
        {
            int startIndex = source.IndexOf(start);
            if (startIndex != -1)
            {
                string sourceWithEnd = source.Substring(startIndex + (includeStart ? 0 : start.Length));
                int endIndex = sourceWithEnd.IndexOf(end);
                if (endIndex != -1)
                {
                    string result = sourceWithEnd.Substring(0, endIndex + (includeEnd ? end.Length : 0));
                    return result;
                }
            }
            return "";
        }

        void getTwitterHTMLAndUrl()
        {
            if (invoked)
            {
                invoked = false;
                String keyUrlSentence = "\"og:url\" content=\"";
                currentUrl = getSubstringBetweenStartAndEnd(currentHTML, keyUrlSentence, "\"", false, false);

                if (currentUrl != previousUrl)
                {
                    previousUrl = currentUrl;
                    if (currentUrl.Contains("/status/") && currentUrl.Contains("/photo/"))
                    {
                        //画像の場合
                        App.SetEnableSwipePaging(false);
                    }
                    else
                    {
                        App.SetEnableSwipePaging(true);
                    }
                }

                if (currentHTML != previousHTML)
                {
                    previousHTML = currentHTML;
                    Device.BeginInvokeOnMainThread(() => {
                        Eval(CRTVjs);
                        Eval("showCitedRTLink();");
                    });
                    twitterHTMLChanged(currentUrl, currentHTML);
                }
            }
            else
            {
                RegisterAction(data => { currentHTML = data; });
                Device.BeginInvokeOnMainThread(() =>
                {
                    Eval("javascript:invokeCSharpAction(document.documentElement.outerHTML);");
                });
            }
        }
        //ツイッター内リンク管理ここまで

    }
}