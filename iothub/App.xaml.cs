using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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

        private async Task connectIoTHub(string username)
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
                Client = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Mqtt_WebSocket_Only);

                await Client.GetTwinAsync();

                TwinCollection reportedProperties, connectivity;
                reportedProperties = new TwinCollection();
                connectivity = new TwinCollection();
                reportedProperties["username"] = username;
                reportedProperties["lastLogin"] = DateTime.UtcNow;
                await Client.UpdateReportedPropertiesAsync(reportedProperties);
                ReportAppState("Active");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
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
                Debug.WriteLine(ex.Message);
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

        public async void ReportAppState(string appState)
        {
            try
            {
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
            ReportAppState("Active");
        }

        protected override void OnSleep()
        {
            ReportAppState("Sleep");
        }

        protected override void OnResume()
        {
            ReportAppState("Active");
        }
    }
}
