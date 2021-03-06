﻿using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;
using WindowsAzure.Messaging;
using Xamarin.Forms;

namespace iothub.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        private SBNotificationHub Hub { get; set; }
        private NSData deviceToken { get; set; }
        private string username { get; set; }

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            RequestRemoteNotificationPermission();

            MessagingCenter.Subscribe<App, string>(this, "loginsuccess", (sender, username) => {
                this.username = username;
                RegisterRemoteNotification();
            });

            global::Xamarin.Forms.Forms.Init();

            LoadApplication(new App());

            return base.FinishedLaunching(app, options);
        }

        private void RequestRemoteNotificationPermission()
        {
            var pushSettings = UIUserNotificationSettings.GetSettingsForTypes(
                UIUserNotificationType.Alert
                | UIUserNotificationType.Badge
                | UIUserNotificationType.Sound,
                 new NSSet());

            UIApplication.SharedApplication.RegisterUserNotificationSettings(pushSettings);
        }

        private void RegisterRemoteNotification()
        {
                UIApplication.SharedApplication.RegisterForRemoteNotifications();
        }

        public override async void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            Hub = new SBNotificationHub("Endpoint=sb://pilotnoti.servicebus.windows.net/;SharedAccessKeyName=DefaultListenSharedAccessSignature;SharedAccessKey=GtR6FWMaXkLQKQVIxfk5JDn5kU6BL2nzp+PIXxMKwsk=", "pilotnoti");

            NSSet tags = new NSSet(username); // create tags if you want
            Hub.UnregisterAllAsync(deviceToken, (error) =>
            {
                Hub.RegisterNativeAsync(deviceToken, tags, (errorCallback) =>
                {
                    this.deviceToken = deviceToken;
                    if (errorCallback != null)
                        Console.WriteLine("RegisterNativeAsync error: " + errorCallback.ToString());
                });

                if (error != null)
                {
                    Console.WriteLine(error.ToString());
                }
            });
        }

        public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
            NSDictionary aps = userInfo.ObjectForKey(new NSString("aps")) as NSDictionary;
            NSDictionary message = userInfo.ObjectForKey(new NSString("message")) as NSDictionary;

            string alert = string.Empty;
            string content = string.Empty;
            if (userInfo.ContainsKey(new NSString("message")))
            {
                content = (message[new NSString("content")] as NSString).ToString();
                var notificationContent = Newtonsoft.Json.JsonConvert.DeserializeObject<PushNotiModel>(content);
                notificationContent.Title += " by pushnoti";
                App.Instance.HandleNotificationMessage(notificationContent);
            }
        }

        //public override void OnActivated(UIApplication application)
        //{
        //    App.Instance.ReportAppState("Active").GetAwaiter();
        //}

        //public override void OnResignActivation(UIApplication application)
        //{
        //}

        public override void DidEnterBackground(UIApplication application)
        {
            nint taskID = UIApplication.SharedApplication.BeginBackgroundTask(() => { });
            new System.Threading.Tasks.Task(async() => {
                await App.Instance.ReportAppState("Sleep");
                UIApplication.SharedApplication.EndBackgroundTask(taskID);
            }).Start();
        }

        //// not guaranteed that this will run
        //public override void WillTerminate(UIApplication application)
        //{
        //    //App.Instance.ReportAppState("Teminated");
        //}
    }
}
