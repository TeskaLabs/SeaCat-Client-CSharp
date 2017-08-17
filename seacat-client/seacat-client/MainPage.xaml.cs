using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.ApplicationModel;
using System.Reflection;
using seacat_wp_client;
using System.Net.Http;
using seacat_wp_client.Core;
using System.Threading.Tasks;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace seacat_client
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public async Task<bool> DeleteSeacatDirAsync()
        {
            try
            {
                var allf = await ApplicationData.Current.LocalFolder.GetFoldersAsync();
                var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(".seacat");
                var files = await folder.GetFilesAsync();

                foreach (var file in files)
                {
                    await file.DeleteAsync();
                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;

            new Task(() =>
            {
                DeleteSeacatDirAsync().Wait();
                SeaCatClient.Initialize();
            }).Start();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

        }
    }
}
