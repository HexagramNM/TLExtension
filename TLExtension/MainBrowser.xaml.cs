using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Timers;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.StyleSheets;
using CoreTweet;
using System.Windows.Input;
using CoreTweet.Core;

namespace TLExtension
{

    public partial class MainBrowser : ContentPage
    {
        public TLExtensionWebView web;
        private ActivityIndicator loadingIndicator;
        private Button imageDownloadButton;

        bool isVideo;
        string detectImageUrl;
        //ビデオのあるtweetIdを入れる
        string detectVideoUrl;
        string detectMediaFileName;
        Image detectImageSource;
        bool isDownloading;

        private void makeUI()
        {
            StackLayout buttonLayout = new StackLayout();
            buttonLayout.Orientation = StackOrientation.Horizontal;
            Button reloadButton = new Button();
            reloadButton.HeightRequest = 50;
            reloadButton.Text = "Reload";
            reloadButton.HorizontalOptions = LayoutOptions.FillAndExpand;
            reloadButton.IsEnabled = true;
            reloadButton.Pressed += reloadButtonPressed;
            imageDownloadButton = new Button();
            imageDownloadButton.HeightRequest = 50;
            imageDownloadButton.Image = 
            imageDownloadButton.Text = "Download Last Image →";
            imageDownloadButton.HorizontalOptions = LayoutOptions.FillAndExpand;
            imageDownloadButton.IsEnabled = false;
            imageDownloadButton.Pressed += imageDownloadButtonPressed;
            detectImageSource = new Image();
            detectImageSource.HeightRequest = 50;
            detectImageSource.WidthRequest = 50;

            buttonLayout.Children.Add(reloadButton);
            buttonLayout.Children.Add(imageDownloadButton);
            buttonLayout.Children.Add(detectImageSource);

            loadingIndicator = new ActivityIndicator();
            loadingIndicator.HeightRequest = 50;
            loadingIndicator.Color = new Color(0, 190.0 / 255.0, 1.0);
            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;

            web = new TLExtensionWebView(loadingIndicator);
            web.VerticalOptions = LayoutOptions.FillAndExpand;

            StackLayout layout = new StackLayout();
            layout.Orientation = StackOrientation.Vertical;
            layout.HorizontalOptions = LayoutOptions.FillAndExpand;
            layout.Children.Add(buttonLayout);
            layout.Children.Add(loadingIndicator);
            layout.Children.Add(web);
            Content = layout;
            
        }

        public MainBrowser()
        {
            InitializeComponent();
            makeUI();
 
            detectImageUrl = "";
            detectVideoUrl = "";
            detectMediaFileName = "";
            isDownloading = false;
            isVideo = false;
            web.twitterHTMLChanged += (string url, string HTML) =>
            {
                detectImage(url, HTML);
            };
        }

        private void getTweetImageInfo(string currentDetectUrl, string currentDetectHTML)
        {
            int imagePageNum = -1;

            int.TryParse(currentDetectUrl.Substring(currentDetectUrl.Length - 1, 1), out imagePageNum);
            if (imagePageNum != -1)
            {
                String keyImageUrlSentence = "https://pbs.twimg.com/media/";
                String tmpPageHTML = String.Copy(currentDetectHTML);
                int imageUrlIndex = -1;
                for (int idx = 0; idx < imagePageNum - 1; idx++)
                {
                    for (int tmpCount = 0; tmpCount < 2; tmpCount++)
                    {
                        imageUrlIndex = tmpPageHTML.IndexOf(keyImageUrlSentence);
                        if (imageUrlIndex != -1)
                        {
                            tmpPageHTML = tmpPageHTML.Substring(imageUrlIndex + keyImageUrlSentence.Length);
                        }
                    }
                }
                string currentImageUrl = TLExtensionWebView.getSubstringBetweenStartAndEnd(tmpPageHTML,
                    keyImageUrlSentence, "&quot", true, false);
                if (currentImageUrl != "")
                {
                    currentImageUrl = currentImageUrl.Replace("&amp;", "&");
                    if (detectImageUrl != currentImageUrl)
                    {
                        String tweetId = TLExtensionWebView.getSubstringBetweenStartAndEnd(currentDetectUrl, "/status/", "/", false, false);
                        string extension = TLExtensionWebView.getSubstringBetweenStartAndEnd(currentImageUrl, "format=", "&", false, false);
                        if (tweetId != "" && extension != "")
                        {
                            detectMediaFileName = tweetId + "_" + imagePageNum.ToString() + "." + extension;
                        }
                        else
                        {
                            detectMediaFileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jpg";
                        }
                        detectImageUrl = currentImageUrl;
                    }
                }
            }
            isVideo = false;
        }

        private void getProfileImageInfo(string currentDetectUrl, string currentDetectHTML, string keyImageUrlSentence)
        {
            String tmpPageHTML = String.Copy(currentDetectHTML);
            
            string currentImageUrl = TLExtensionWebView.getSubstringBetweenStartAndEnd(tmpPageHTML,
                keyImageUrlSentence, "&quot;", true, false);
            if (currentImageUrl != "")
            {
                if (detectImageUrl != currentImageUrl)
                {
                    String userId = "";
                    if (keyImageUrlSentence == "https://pbs.twimg.com/profile_images/")
                    {
                        userId = TLExtensionWebView.getSubstringBetweenStartAndEnd(currentDetectUrl, "/mobile.twitter.com/", "/photo", false, false);
                    }
                    else if (keyImageUrlSentence == "https://pbs.twimg.com/profile_banners/")
                    {
                        userId = TLExtensionWebView.getSubstringBetweenStartAndEnd(currentDetectUrl, "/mobile.twitter.com/", "/header_photo", false, false);
                    }
                    
                    if (userId == "" )
                    {
                        userId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    }
                    if (keyImageUrlSentence == "https://pbs.twimg.com/profile_images/")
                    {
                        detectMediaFileName = userId + "_icon.jpg";
                    }
                    else if (keyImageUrlSentence == "https://pbs.twimg.com/profile_banners/")
                    {
                        detectMediaFileName = userId + "_header.jpg";
                    }
                    else
                    {
                        detectMediaFileName = userId + "_other.jpg";
                    }
                    detectImageUrl = currentImageUrl;
                }
            }
            isVideo = false;
        }

        private void getTweetVideoInfo(string currentDetectUrl, string currentDetectHTML)
        {
            List<String> keyVideoSentenceList = new List<String>();
            int hitKeyVideoSentenceIndex = 0;
            keyVideoSentenceList.Add("https://pbs.twimg.com/ext_tw_video_thumb");
            keyVideoSentenceList.Add("https://pbs.twimg.com/tweet_video_thumb");
            keyVideoSentenceList.Add("https://pbs.twimg.com/amplify_video_thumb");
            keyVideoSentenceList.Add("https://pbs.twimg.com/media");
            int videoThumbnailIndex = -1;
            //本体のツイートの最初のクラスと、そのツイートの日時を示すクラスの間に制限する。
            //こうすることで、リプライにぶら下がってる動画は無視できるようになる。
            string targetTweetArticleHTML = TLExtensionWebView.getSubstringBetweenStartAndEnd(currentDetectHTML,
                "css-1dbjc4n r-psjefw", "css-1dbjc4n r-1h1bdhe", false, false);
            if (!targetTweetArticleHTML.Contains("埋め込み動画"))
            {
                //そのツイートに動画はない
                return;
            }
            foreach (String key in keyVideoSentenceList)
            {
                videoThumbnailIndex = targetTweetArticleHTML.IndexOf(key);
                if (videoThumbnailIndex != -1)
                {
                    break;
                }
                hitKeyVideoSentenceIndex++;
            }
            int idIndex = currentDetectUrl.IndexOf("/status/");
            if (idIndex != -1)
            {
                String tweetId = currentDetectUrl.Substring(idIndex + "/status/".Length);
                if (videoThumbnailIndex != -1)
                {
                    String videoThumbnailSentence = targetTweetArticleHTML.Substring(videoThumbnailIndex);
                    int videoCloseIndex1 = videoThumbnailSentence.IndexOf("?");
                    int videoCloseIndex2 = videoThumbnailSentence.IndexOf("\"");
                    int videoThumbnailCloseIndex = (videoCloseIndex1 < videoCloseIndex2 ? videoCloseIndex1 : videoCloseIndex2);
                    if (videoThumbnailCloseIndex != -1)
                    {
                        string currentImageUrl = videoThumbnailSentence.Substring(0, videoThumbnailCloseIndex);
                        if (!currentImageUrl.Contains(".jpg"))
                        {
                            currentImageUrl = currentImageUrl + ".jpg";
                        }
                        if (detectImageUrl != currentImageUrl)
                        {
                            detectImageUrl = currentImageUrl;
                            detectMediaFileName = tweetId + "_video.mp4";
                            //URL取得のためにTweetIDを取得しておく
                            detectVideoUrl = tweetId;
                        }
                    }
                    isVideo = true;
                }
            }
        }

        private void detectImage(string currentDetectUrl, string currentDetectHTML)
        {
            string previousImageUrl = detectImageUrl;
            if (currentDetectUrl.Contains("/status/") && currentDetectUrl.Contains("/photo/"))
            {
                //画像の場合
                getTweetImageInfo(currentDetectUrl, currentDetectHTML);
            }
            else if (currentDetectUrl.EndsWith("/photo"))
            {
                //アイコンの場合
                getProfileImageInfo(currentDetectUrl, currentDetectHTML, "https://pbs.twimg.com/profile_images/");
            }
            else if (currentDetectUrl.EndsWith("/header_photo"))
            {
                //ヘッダ画像の場合
                getProfileImageInfo(currentDetectUrl, currentDetectHTML, "https://pbs.twimg.com/profile_banners/");
            }
            else if (currentDetectUrl.Contains("/status/"))
            {
                //動画の場合
                getTweetVideoInfo(currentDetectUrl, currentDetectHTML);
            }
            Device.BeginInvokeOnMainThread(() =>
            {
                if (detectImageUrl != previousImageUrl && detectImageUrl != "")
                {
                    detectImageSource.Source = new UriImageSource
                    {
                        Uri = new Uri(detectImageUrl),
                        CachingEnabled = false
                    };
                }
                imageDownloadButton.IsEnabled = (detectImageUrl != "" && !isDownloading);

                if (isVideo)
                {
                    imageDownloadButton.Text = (isDownloading ? "Downloading Video →" : "Download Last Video →");
                }
                else
                {
                    imageDownloadButton.Text = (isDownloading ? "Downloading Image →" : "Download Last Image →");
                }
            });
        }

        private void reloadButtonPressed(object sender, EventArgs args)
        {
            if (web != null)
            {
                web.Reload();
            }
        }

        private string getVideoUrlFromTweetId(string tweetId)
        {
            long tweetIdLong;
            long.TryParse(tweetId, out tweetIdLong);
            List<long> tweetIdList = new List<long>();
            tweetIdList.Add(tweetIdLong);
            ListedResponse<Status> statusList = App.t.Statuses.Lookup(tweetIdList, false, true);
            Status targetStatus = statusList[0];
            VideoVariant[] videoInfoList = targetStatus.ExtendedEntities.Media[0].VideoInfo.Variants;
            int tmpBitrate = -1;
            string result = "";
            foreach (VideoVariant videoInfo in videoInfoList)
            {
                if (videoInfo.ContentType == "video/mp4")
                {
                    if (videoInfo.Bitrate > tmpBitrate)
                    {
                        tmpBitrate = (int)videoInfo.Bitrate;
                        result = videoInfo.Url;
                    }
                }
            }
            return result;
        }    

        private async Task downloadImageAsync()
        {
            string downloadUrl = "";
            isDownloading = true;
            Device.BeginInvokeOnMainThread(() =>
            {
                imageDownloadButton.IsEnabled = false;
                imageDownloadButton.Text = (isVideo ? "Downloading Video →" : "Downloading Image →");
            });
            try
            {
                if (isVideo)
                {
                    downloadUrl = getVideoUrlFromTweetId(detectVideoUrl);
                }
                else
                {
                    downloadUrl = detectImageUrl;
                }
                if (await web.downloadMediaFromTwitter(downloadUrl, detectMediaFileName))
                {

                    await DisplayAlert("Successed to download image", "Successed to download the last media!", "OK");
                }
                else
                {
                    await DisplayAlert("Failed to download media", "Failed to download the last media...", "OK");
                }
            }
            catch (Exception e)
            {
                await DisplayAlert("Failed to download media", "Failed to download the last media...", "OK");
            }
            finally
            {
                isDownloading = false;
                Device.BeginInvokeOnMainThread(() =>
                {
                    imageDownloadButton.IsEnabled = true;
                    imageDownloadButton.Text = (isVideo ? "Download Last Video →": "Download Last Image →");
                });
            }
        }

        private async void imageDownloadButtonPressed(object sender, EventArgs args)
        {
            if (detectImageUrl != "")
            {
                bool answer = await DisplayAlert("Download image", "Would you like to download the last image?", "Yes", "No");
                if (answer)
                {
                    await downloadImageAsync();
                }
            }
        }
        
        protected override bool OnBackButtonPressed()
        {
            web.GoBack();
            return true;
        }

        
    }
}
