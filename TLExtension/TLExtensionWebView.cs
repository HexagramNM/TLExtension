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
        public Action clearCacheAction = null;
        public Action<string, byte[]> saveMediaAction = null;
        public Action<bool> switchKeepOn = null;
        public ActivityIndicator indicator;
        Action<string> action;
        HttpClient httpClient;
        string CRTVjs;
        string TimelineLoaderJs;
        bool autoUpdatingTimeline;
        Timer HTMLGetterTimer;
        Timer TimelineLoaderTimer;
        public string startLink = "https://mobile.twitter.com/home/";
        private string autoTweet = "";
        private bool isTextAreaExist = false;

        public bool invoked { get; set; } = false;

        public static readonly BindableProperty EnterFullScreenCmmandProperty =
            BindableProperty.Create(
                propertyName: "EnterFullScreenCommand",
                returnType: typeof(ICommand),
                declaringType: typeof(TLExtensionWebView),
                defaultValue: new Command(async (view) => await DefaultEnterAsync((Xamarin.Forms.View)view))
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

            autoUpdatingTimeline = false;
            using (StreamReader sr = new StreamReader(assets.Open("TimelineLoader.js")))
            {
                TimelineLoaderJs = sr.ReadToEnd();
            }
            TimelineLoaderTimer = new Timer(8000);
            TimelineLoaderTimer.Elapsed += (object s, ElapsedEventArgs e) => { updateTimeline(); };
            TimelineLoaderTimer.Start();

            indicator = i_indicator;

            App.registerAuthorizedEvent(() => {
                App.registerStopEvent(() => { HTMLGetterTimer.Stop(); });
                App.registerRestartEvent(() => { HTMLGetterTimer.Start(); });
                HTMLGetterTimer.Start();
            });
            App.registerStopEvent(() => { clearCache(); });
        }

        //webview上のページに対してjavascriptを実行するためのメソッド
        public void RegisterAction(Action<string> callback)
        {
            invoked = false;
            action = callback;
        }

        public void Cleanup()
        {
            invoked = false;
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
        public void CustomizedReload()
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

        public void clearCache()
        {
            clearCacheAction?.Invoke();
        }
        //リロードや履歴の消去　ここまで

        //共有時の自動ツイート入力
        public void autoInputTweet(string tweetText)
        {
            autoTweet = tweetText;
            HTMLGetterTimer.Elapsed += autoInputProcess;
            HTMLGetterTimer.Start();
            Source = @"https://mobile.twitter.com/compose/tweet";
        }

        private void autoInputProcess(object s, ElapsedEventArgs e)
        {
            if (currentUrl == @"https://mobile.twitter.com/compose/tweet")
            {
                if (isTextAreaExist)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        Eval("javascript:autoTweet(\"" + autoTweet + "\");");
                    });
                } 
            }
        }

        //共有時の自動ツイート入力　ここまで

        //urlを経由したメディアダウンロード
        public async Task<bool> downloadMediaFromTwitter(string downloadUrl, string saveFileName)
        {
            using (HttpResponseMessage httpResponse = await httpClient.GetAsync(downloadUrl))
            {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    byte[] imageData = await httpResponse.Content.ReadAsByteArrayAsync();
                    saveMediaAction?.Invoke(saveFileName, imageData);
                    return true;
                }
                return false;
            }
        }

        //urlを経由したメディアダウンロード　ここまで

        //フルスクリーンを実現するためのメソッド
        private static async Task DefaultEnterAsync(Xamarin.Forms.View view)
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
            while (!invoked);
            invoked = false;
            int codeIndex = authSource.IndexOf("<code>");
            if (codeIndex >= 0)
            {
                String code = authSource.Substring(codeIndex + 6, 7);
                Source = startLink;
                App.authorizing(code);
            }
            else
            {
                indicator.IsVisible = false;
                indicator.IsRunning = false;
                IsVisible = true;
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
            else if (e.Url == startLink)
            {
                indicator.IsVisible = false;
                indicator.IsRunning = false;
                IsVisible = true;
                clearHistory();
                if (startLink != "https://mobile.twitter.com/home/")
                {
                    startLink = "https://mobile.twitter.com/home/";
                }
                Navigating -= authorizingNavigatedEvent;
                Navigated -= authorizedNavigatedEvent;
            }
        }

        public void twitterStart()
        {
            Source = startLink;
            if (startLink != "https://mobile.twitter.com/home/")
            {
                startLink = "https://mobile.twitter.com/home/";
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
                String keyUrlStartSentence = "<link href=\"";
                String keyUrlEndSentence = "\"";
                currentUrl = getSubstringBetweenStartAndEnd(currentHTML, keyUrlStartSentence, keyUrlEndSentence, false, false);
                isTextAreaExist = currentHTML.Contains("textarea");

                if (currentUrl != previousUrl)
                {
                    previousUrl = currentUrl;
                    
                    App.ResetSoftwareKeyboardStatus();
                    
                    /*if (currentUrl.Contains("/status/") && currentUrl.Contains("/photo/"))
                    {
                        //画像の場合
                        App.SetEnableSwipePaging(false);
                    }
                    else if (currentUrl.EndsWith("/photo") || currentUrl.EndsWith("/header_photo"))
                    {
                        //アイコンorヘッダ画像の場合
                        App.SetEnableSwipePaging(false);
                    }
                    else
                    {
                        App.SetEnableSwipePaging(true);
                    }*/
                    if (currentUrl.EndsWith("/home") || currentUrl.EndsWith("/home/"))
                    {
                        //ホーム画面のみスワイプ移動を許可する（他の画面だとスワイプするものがなんだかんだある。）
                        App.SetEnableSwipePaging(true);
                    }
                    else
                    {
                        App.SetEnableSwipePaging(false);
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

        //ホーム画面のタイムライン更新
        public void switchAutoUpdatingTimeline(bool state)
        {
            switchKeepOn?.Invoke(state);
            autoUpdatingTimeline = state;
        }

        public void updateTimeline()
        {
            if (autoUpdatingTimeline)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Eval(TimelineLoaderJs);
                    Eval("updateTimeline();");
                });
            }
        }

    }
}