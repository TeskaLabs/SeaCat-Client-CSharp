# SeaCat-Client-CSHARP
TeskaLabs SeaCat client for CSharp.
For now, WinRT and Windows Phone 8.1 are supported.

## HTTP client example

```C#

  SeaCatClient.Initialize("mobi.seacat.test", null, "wp8", ApplicationData.Current.LocalFolder.Path);
  SeaCatClient.Reactor.IsReadyHandle.WaitOne();
  var client = SeaCatClient.Open();
  var responseAsync = client.GetAsync("http://jsonplaceholder.seacat/posts/1/comments");

```	