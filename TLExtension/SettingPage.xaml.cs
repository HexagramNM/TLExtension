using CoreTweet;
using CoreTweet.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TLExtension
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SettingPage : CustomizedContentPage
    {
        private Entry runningName;

        private string nameSettingPath;

        private Button buttonName;

        private Timer updateNameTimer;

        private float interval = 1000.0f * 60.0f * 2.0f;

        private DateTimeOffset lastUpdateDateTime;

        public StackLayout customSettingLayout;

        private void initializeUI()
        {
            StackLayout verticalLayout = new StackLayout();
            verticalLayout.Orientation = StackOrientation.Vertical;
            Button buttonAuth = new Button();
            buttonAuth.Text = "API再認証";
            buttonAuth.Pressed += (s, e) => { App.reauthorize(); };
            Label labelSpace1 = new Label();
            labelSpace1.Text = " ";
            Label labelName = new Label();
            labelName.Text = "アカウント名";
            labelName.TextColor = Color.FromHex("#222222");
            runningName = new Entry();
            runningName.TextColor = Color.FromHex("#000000");
            buttonName = new Button();
            buttonName.Text = "適用";
            runningName.IsEnabled = false;
            buttonName.IsEnabled = false;
            buttonName.Pressed += pushApplyButton;
            Label labelSpace2 = new Label();
            labelSpace2.Text = " ";

            customSettingLayout = new StackLayout();
            customSettingLayout.Orientation = StackOrientation.Vertical;

            List <View> customSettingList = App.getCurrentCustomSetting();
            foreach (View view in customSettingList)
            {
                customSettingLayout.Children.Add(view);
            }

            verticalLayout.Children.Add(buttonAuth);
            verticalLayout.Children.Add(labelSpace1);
            verticalLayout.Children.Add(labelName);
            verticalLayout.Children.Add(runningName);
            verticalLayout.Children.Add(buttonName);
            verticalLayout.Children.Add(labelSpace2);
            verticalLayout.Children.Add(customSettingLayout);
            verticalLayout.BackgroundColor = Color.FromHex("#FFFFFF");
            Content = verticalLayout;
        }

        public SettingPage()
        {
            InitializeComponent();

            initializeUI();

            updateNameTimer = new Timer(interval);
            updateNameTimer.Elapsed += (s, e) =>
            {
                updateName();
            };

            App.registerAuthorizedEvent(() =>
                {
                    string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                    nameSettingPath = path + "/settingTLExtension_" + App.t.UserId.ToString() + ".txt";
                    if (File.Exists(nameSettingPath))
                    {
                        StreamReader readFile = new StreamReader(nameSettingPath, Encoding.GetEncoding("utf-16"));
                        runningName.Text = readFile.ReadLine();
                        readFile.Close();
                    }
                    else
                    {
                        Setting userData;
                        userData = App.t.Account.Settings();
                        UserResponse userDataWithName = App.t.Users.Show(new { screen_name = userData.ScreenName });
                        runningName.Text = userDataWithName.Name;
                        saveNameSetting();
                    }
                    runningName.IsEnabled = true;
                    buttonName.IsEnabled = true;
                    updateName();
                    updateNameTimer.Start();
                });

            App.registerStopEvent(() => 
            {
                if (authorized)
                {
                    updateNameTimer.Stop();
                }
            });
            App.registerRestartEvent(() => 
            {
                if (authorized)
                {
                    if (DateTimeOffset.Now - lastUpdateDateTime >= TimeSpan.FromMilliseconds(interval))
                    {
                        updateName();
                        updateNameTimer.Interval = interval;
                    }
                    updateNameTimer.Start();
                }
            });
        }

        //アカウント名設定周り
        private void updateName()
        {
            lastUpdateDateTime = DateTimeOffset.Now;
            App.t.Account.UpdateProfile(runningName.Text + " (" + DateTime.Now.ToString("MM/dd_HH:mm") + ")");
        }

        private void pushApplyButton(object sender, EventArgs arg)
        {
            if (App.t != null) {
                string previousRunningName = runningName.Text;
                
                if (saveNameSetting())
                {
                    StreamReader readFile = new StreamReader(nameSettingPath, Encoding.GetEncoding("utf-16"));
                    runningName.Text = readFile.ReadLine();
                    readFile.Close();
                    App.t.Account.UpdateProfile(runningName.Text + " (" + DateTime.Now.ToString("MM/dd_HH:mm") + ")");
                    runningName.IsEnabled = false;
                    buttonName.IsEnabled = false;
                    Task buttonTask = new Task(async () =>
                    {
                        await Task.Delay(120 * 1000);
                        runningName.IsEnabled = true;
                        buttonName.IsEnabled = true;
                    });
                }
                else
                {
                    runningName.Text = previousRunningName;
                }
            }
        }

        private bool saveNameSetting()
        {
            if (string.IsNullOrWhiteSpace(runningName.Text))
            {
                return false;
            }
            
            StreamWriter sw = new StreamWriter(nameSettingPath, false, Encoding.GetEncoding("utf-16"));
            sw.WriteLine(runningName.Text);
            sw.Close();
            return true;
        }

        //アカウント名設定周りここまで
    }
}