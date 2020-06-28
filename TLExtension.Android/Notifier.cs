using Android.App;
using Android.Content;
using Android.Media;
using Xamarin.Forms;
using TLExtension;
using TLExtension.Droid;
using Android.Support.V7.App;
using Android.OS;

[assembly: Dependency(typeof(MyNotificationService))]
public class MyNotificationService : INotificationService
{

    public void Regist()
    {
        //iOS用なので、何もしない
    }

    public void On(string title, string body, int id)
    {
        Context context = Forms.Context;
        Intent intent = new Intent(context, typeof(MainActivity));
        PendingIntent pendingIntent = PendingIntent.GetActivity(context, 0, intent, 0);

        //デフォルトの通知音を取得
        Android.Net.Uri uri = RingtoneManager.GetDefaultUri(RingtoneType.Notification);
        string channelId = "TLExtension";
        Notification.Builder builder = new Notification.Builder(context)
                .SetContentTitle(title)
                .SetContentText(body)
                .SetSmallIcon(Resource.Mipmap.icon);
        Notification notification = builder.Build();

        NotificationManager notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService);

        if (Build.VERSION.SdkInt >= Build.VERSION_CODES.O)
        {
            NotificationChannel notificationChannel = notificationManager.GetNotificationChannel(channelId);
            if (notificationChannel == null)
            {
                NotificationImportance importance = NotificationImportance.High;
                notificationChannel = new NotificationChannel(channelId, "Use in TL Extension", importance);
                notificationChannel.EnableLights(true);
                notificationChannel.EnableVibration(true);
                notificationManager.CreateNotificationChannel(notificationChannel);
            }
            builder = builder.SetChannelId(channelId);
        }
        
        notificationManager.Notify(id, notification);
    }

    public void Off(int id)
    {
        Context context = Forms.Context;
        NotificationManager manager = (NotificationManager)context.GetSystemService(Context.NotificationService);
        manager.Cancel(id);
    }
}