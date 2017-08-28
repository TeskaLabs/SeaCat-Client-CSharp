using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
using seacat_winrt_client;
using seacat_winrt_client.Utils;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace seacat_demoapp {
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
            } catch (Exception e) {
                return false;
            }
            return true;
        }

        public MainPage() {
            this.InitializeComponent();

            new Task(() => {
                //DeleteSeacatDirAsync().Wait();
                SeaCatClient.Initialize();
                SeaCatClient.GetReactor().isReadyHandle.WaitOne();
                Logger.Debug("Seacat", "====== SEACAT IS READY ======");

                new Task(DownloadUrl).Start();

            }).Start();
        }

        protected async void DownloadUrl() {

            var postString = "{\"userId\": 1, \"id\": 1, \"title\": \"HELLO WORLD\", \"body\": \"Hello Post message\"}";

            /*
                var realTest = new HttpClient();
                var realMsg = await realTest.GetStringAsync("http://jsonplaceholder.typicode.com/posts/1");

                var realPost = await realTest.PostAsync(new Uri("http://jsonplaceholder.typicode.com/posts"), new StringContent(postString));

                realPost.EnsureSuccessStatusCode();
                string responseBody = await realPost.Content.ReadAsStringAsync();
            */

            using (var client = SeaCatClient.Open("http://jsonplaceholder.seacat/posts/1")) {
                for (int i = 0; i < 10; i++)
                {
                    GetAsync(client);
                    PostAsync(client, postString);
                }
            }

            bool dummy = false;
        }

        protected async void GetAsync(HttpClient client) {
            var getResp = await client.GetAsync("http://jsonplaceholder.seacat/posts/1");
            var strResp = await getResp.Content.ReadAsStringAsync();

            IEnumerable<string> handlerIds = new List<string>();
            getResp.Content.Headers.TryGetValues("HANDLER-ID", out handlerIds);

            Logger.Debug($"===== {handlerIds.First()} RESPONSE BODY::", strResp);
        }

        protected async void PostAsync(HttpClient client, string requestMsg) {
            var msg = await client.PostAsync("http://jsonplaceholder.seacat/posts", new StringContent(requestMsg));

            IEnumerable<string> handlerIds = new List<string>();
            msg.Content.Headers.TryGetValues("HANDLER-ID", out handlerIds);

            var stringMsg = await msg.Content.ReadAsStringAsync();
            Logger.Debug($"===== {handlerIds.First()} RESPONSE BODY::", stringMsg);
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
