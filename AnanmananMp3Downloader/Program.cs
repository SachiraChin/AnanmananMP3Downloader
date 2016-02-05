using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace AnanmananMp3Downloader
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("This application created to download MP3 audio files from Ananmanan.lk site.\nAny content downloaded from this application beongs to Ananmanan.lk.\n");

            Console.WriteLine("Please enter url which contains download links (Artist page): ");
            var page = Console.ReadLine();
            Debug.Assert(page != null, "page != null");

            Console.WriteLine("\nPlease enter song download location (content): ");
            var savePath = Console.ReadLine();
            if (savePath == "") savePath = "content";

            Debug.Assert(savePath != null, "savePath != null");
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            Console.WriteLine("Extracting urls...");
            var hw = new HtmlWeb();
            var doc = hw.Load(page);
            var baseSongPath = "http://www.ananmanan.lk/free-sinhala-mp3/song/";

            var songs =
                (from link in doc.DocumentNode.SelectNodes("//div[@id='content']/div[@class='mp3']/a[@href]")
                 let href = link.Attributes["href"]
                 let href2 = new Uri(new Uri(page), href.Value).AbsoluteUri.ToLower()
                 where href2.StartsWith(baseSongPath)
                 let songId = href2.Replace(baseSongPath, "").Split(new[] { "/" }, StringSplitOptions.None)[0]
                 select new AnanmananSong { Id = songId, Name = WebUtility.HtmlDecode(StripHtml(link.InnerText)) }).ToList();

            songs = songs.GroupBy(s => s.Id).Select(g => g.FirstOrDefault()).ToList();
            Console.WriteLine();
            using (var client = new WebClient())
            {
                foreach (var ananmananSong in songs)
                {
                    Console.WriteLine($"Downloading {ananmananSong.Name}...");
                    var songSavePath = savePath + (savePath.EndsWith("\\") ? "" : "\\") + ananmananSong.Name + ".mp3";

                    if (File.Exists(songSavePath))
                    {
                        Console.WriteLine("Song already exists on directory..");
                        continue;
                    }
                    var songPath = "http://www.ananmanan.lk/free-sinhala-mp3/download.php?id=" + ananmananSong.Id;
                    client.DownloadProgressChanged += (sender, dargs) =>
                    {
                        Console.Write($"\rDownloading {dargs.BytesReceived / 1024}KB of {dargs.TotalBytesToReceive / 1024}KB");
                    };
                    client.DownloadFileAsync(new Uri(songPath), songSavePath);

                    while (client.IsBusy)
                    {
                        Thread.Sleep(500);
                    }
                    Console.WriteLine();
                }
            }
            Console.Read();
        }
        private static string StripHtml(string input)
        {
            return Regex.Replace(input, "<.*?>", String.Empty);
        }
    }
    public class AnanmananSong
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
