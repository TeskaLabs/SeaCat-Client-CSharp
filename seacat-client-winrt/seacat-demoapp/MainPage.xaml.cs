using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace seacat_demoapp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
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

                var client = SeaCatClient.Open("http://jsonplaceholder.typicode.com/posts/1");
                new Task(DownloadUrl).Start();

            }).Start();
        }

        protected async void DownloadUrl()
        {
            var client = SeaCatClient.Open("http://jsonplaceholder.typicode.com/posts/1");
            var msg = await client.GetStringAsync("http://jsonplaceholder.typicode.com/posts/1");
            bool dummy = false;
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
