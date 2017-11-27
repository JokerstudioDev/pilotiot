using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Xamarin.Forms;

namespace iothub
{
    public partial class App : Application
    {
        public static App Instance;
        private string DeviceConnectionString;
        private DeviceClient Client;


        public App()
        {
            if (Instance == null) Instance = this;

            Microsoft.AppCenter.AppCenter.Start("ios=ea44e600-db4c-4e06-ad2d-ee74111122f9;",
                   typeof(Analytics), typeof(Crashes));
            
            MessagingCenter.Subscribe<App, string>(this, "loginsuccess", async (sender, username) => {
                await connectIoTHub(username);
                await connectDirectMethod();
            });

            login("kritsada@perfenterprise.com");

            InitializeComponent();
            MainPage = new iothubPage();
        }

        private void login(string username)
        {
            MessagingCenter.Send<App, string>(this, "loginsuccess", username);
        }

        public async Task connectIoTHub(string username)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var result = await client.GetAsync($"https://pilotnotihub.azurewebsites.net/api/Hub/{username}");
                    string resultContent = await result.Content.ReadAsStringAsync();
                    var content = JsonConvert.DeserializeObject<dynamic>(resultContent);
                    DeviceConnectionString = $"HostName=pilot-iothub.azure-devices.net;DeviceId={content.deviceId};SharedAccessKey={content.sharedKey}";
                }

                Client = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Amqp_Tcp_Only);

                TwinCollection reportedProperties;
                reportedProperties = new TwinCollection();
                reportedProperties["username"] = username;
                reportedProperties["lastLogin"] = DateTime.UtcNow;
                await Client.UpdateReportedPropertiesAsync(reportedProperties);
                await ReportAppState("Active");
                Device.BeginInvokeOnMainThread(() => {
                    this.MainPage.DisplayAlert("login status", "login success", "OK");
                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task connectDirectMethod()
        {
            try
            {
                await Client.SetMethodHandlerAsync("HandleNotification", HandleNotification, null);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private Task<MethodResponse> HandleNotification(MethodRequest methodRequest, object userContext)
        {
            var notificationContent = Newtonsoft.Json.JsonConvert.DeserializeObject<PushNotiModel>(methodRequest.DataAsJson);
            HandleNotificationMessage(notificationContent);
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"status\":\"success\"}"), 200));
        }

        public void HandleNotificationMessage(PushNotiModel pushnotiModel)
        {
            Device.BeginInvokeOnMainThread(() => {
                this.MainPage.DisplayAlert(pushnotiModel.Title, pushnotiModel.Message, "OK");
            });
        }

        public async Task ReportAppState(string appState)
        {
            try
            {
                //using (var client = new HttpClient())
                //{
                //    var result = await client.GetAsync($"https://pilotnotihub.azurewebsites.net/api/Hub/{Username}/{appState}");
                //    string resultContent = await result.Content.ReadAsStringAsync();
                //    var content = JsonConvert.DeserializeObject<dynamic>(resultContent);
                //}
                if (Client != null)
                {
                    var reportedProperties = new TwinCollection();
                    reportedProperties["app_state"] = appState;
                    await Client.UpdateReportedPropertiesAsync(reportedProperties);
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
            ReportAppState("Sleep");
        }

        protected override async void OnResume()
        {
            await Client.OpenAsync();
            await ReportAppState("Active");
        }
    }
}
