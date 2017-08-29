using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SeaCatCSharpClient;
using SeaCatCSharpClient.Utils;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace SeaCatCSharpWP81Demo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page {
        public MainPage() {
            this.InitializeComponent();

            // redirect logger into textview
            Logger.SetDelegate((string tag, string msg) => {
                Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    MainText.Text += $"{tag}:: {msg}\n";
                    MainScroll.ScrollToVerticalOffset(MainScroll.ScrollableHeight);
                });
            });

            TaskHelper.CreateTask("MainPage", () => {
                // initialize seacat
                SeaCatClient.Initialize("mobi.seacat.test", null, "wp8", ApplicationData.Current.LocalFolder.Path);
                SeaCatClient.SetLogMask(LogFlag.DEBUG_GENERIC);
                SeaCatClient.Reactor.IsReadyHandle.WaitOne();
                Logger.Debug("Seacat", "====== SEACAT IS READY ======");

                // start download task
                TaskHelper.CreateTask("Download", DownloadUrl).Start();

            }).Start();
        }

        private async void DownloadUrl() {
            // open client
            var client = SeaCatClient.Open();
            int idCounter = 1;

            while (true) {
                // get request
                var getTask = new Task(() => GetAsync(client, idCounter++));
                getTask.Start();
                await getTask;
                await Task.Delay(2000);

                // post request
                var postTask = new Task(() => PostAsync(client, idCounter++));
                postTask.Start();
                await getTask;
                await Task.Delay(2000);
            }

            // dispose client at last
            client.Dispose();
        }

        private async void GetAsync(HttpClient client, int id) {
            var getResp = await client.GetAsync("http://jsonplaceholder.seacat/posts/" + id.ToString() + "/comments");
            var strResp = await getResp.Content.ReadAsStringAsync();

            // print id of handler and response body
            IEnumerable<string> handlerIds = new List<string>();
            getResp.Content.Headers.TryGetValues("HANDLER-ID", out handlerIds);
            var handlerId = handlerIds != null ? handlerIds.FirstOrDefault() : "--";
            Logger.Debug($"===== {handlerId} RESPONSE BODY::", strResp);

        }

        private async void PostAsync(HttpClient client, int id) {

            var postString = "{\"userId\": 1, \"id\": " + id +
                             ", \"title\": \"HELLO WORLD\", \"body\": \"Hello Post message\"}";

            Logger.Debug(":HTTP:", $"Post: {postString}");
            var msg = await client.PostAsync("http://jsonplaceholder.seacat/posts", new StringContent(postString));

            IEnumerable<string> handlerIds = new List<string>();
            msg.Content.Headers.TryGetValues("HANDLER-ID", out handlerIds);

            // print id of handler and response body
            var stringMsg = await msg.Content.ReadAsStringAsync();
            var handlerId = handlerIds != null ? handlerIds.FirstOrDefault() : "--";
            Logger.Debug($"===== {handlerId} RESPONSE BODY::", stringMsg);
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e) {

        }
    }
}
