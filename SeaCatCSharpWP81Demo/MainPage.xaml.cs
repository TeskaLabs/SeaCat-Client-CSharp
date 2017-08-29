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
        public async Task<bool> DeleteSeacatDirAsync() {
            try {
                var allf = await ApplicationData.Current.LocalFolder.GetFoldersAsync();
                var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(".seacat");
                var files = await folder.GetFilesAsync();

                foreach (var file in files) {
                    await file.DeleteAsync();
                }
            } catch (Exception) {
                return false;
            }
            return true;
        }

        public MainPage() {
            this.InitializeComponent();

            TaskHelper.CreateTask("MainPage", () => {

                //DeleteSeacatDirAsync().Wait();
                SeaCatClient.Initialize("mobi.seacat.test", null, "wp8", ApplicationData.Current.LocalFolder.Path);
                SeaCatClient.SetLogMask(LogFlag.DEBUG_GENERIC);
                SeaCatClient.GetReactor().isReadyHandle.WaitOne();
                Logger.Debug("Seacat", "====== SEACAT IS READY ======");

                TaskHelper.CreateTask("Download", DownloadUrl).Start();

            }).Start();
        }

        private void DownloadUrl() {

            var client = SeaCatClient.Open();

            for (int i = 0; i < 2; i++) {
                GetAsync(client, i * 2);
                PostAsync(client, i * 2 + 1);
            }

            //client.Dispose();
        }

        private async void GetAsync(HttpClient client, int id) {
            var getResp = await client.GetAsync("http://jsonplaceholder.seacat/posts/" + id.ToString() + "/comments");
            var strResp = await getResp.Content.ReadAsStringAsync();

            //var real = await new HttpClient().GetAsync("http://jsonplaceholder.typicode.com/posts/" + id.ToString() + "/comments");

            IEnumerable<string> handlerIds = new List<string>();
            getResp.Content.Headers.TryGetValues("HANDLER-ID", out handlerIds);
            var handlerId = handlerIds != null ? handlerIds.FirstOrDefault() : "--";

            Logger.Debug($"===== {handlerId} RESPONSE BODY::", strResp);

        }

        private async void PostAsync(HttpClient client, int id) {

            var postString = "{\"userId\": 1, \"id\": " + id +
                             ", \"title\": \"HELLO WORLD\", \"body\": \"Hello Post message\"}";

            var msg = await client.PostAsync("http://jsonplaceholder.seacat/posts", new StringContent(postString));

            IEnumerable<string> handlerIds = new List<string>();
            msg.Content.Headers.TryGetValues("HANDLER-ID", out handlerIds);

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
