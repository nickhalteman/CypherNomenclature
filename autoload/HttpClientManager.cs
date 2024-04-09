using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

[Autoload(300)]
public static class HttpClientManager
{

    private static System.Net.Http.HttpClient httpClient;
    public static void _Ready()
    {
        httpClient = new System.Net.Http.HttpClient();
    }

    public static async void DownloadStream(string url, Action<Stream> onDownload)
    {
        await httpClient.GetByteArrayAsync(url).ContinueWith(x => {
            if (x.IsCompleted)
            {
                GD.Print("Download Complete");
                onDownload(new MemoryStream(x.Result));
            }
            else
            {
                GD.Print("Download Failed?");
            }
        });
    }



}