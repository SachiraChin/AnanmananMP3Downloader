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
        private static CookieContainer _cookies = new CookieContainer();
        static void Main(string[] args)
        {
            Console.WriteLine("This application created to download MP3 audio files from Ananmanan.lk or topsinhalamp3.com site.\nAny content downloaded from this application beongs to Ananmanan.lk or topsinhalamp3.com.\n");

            Console.WriteLine("Please enter url which contains download links (Artist page): ");
            var page = Console.ReadLine();
            Debug.Assert(page != null, "page != null");

            Console.WriteLine("\nPlease enter song download location (content): ");
            var savePath = Console.ReadLine();
            if (savePath == "") savePath = "content";

            Debug.Assert(savePath != null, "savePath != null");
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            if (page.ToLower().Contains("ananmanan.lk"))
            {
                DownloadFromAnanmanan(page, savePath);
            }
            else if (page.ToLower().Contains("topsinhalamp3.com"))
            {
                DownloadFromTopSinhalaMp3(page, savePath);
            }
            else
            {
                Console.WriteLine("Web site not supported.");
            }
            Console.Read();
        }

        private static void DownloadFromTopSinhalaMp3(string page, string savePath)
        {
            Console.WriteLine("Extracting urls...");
            var hw = new HtmlWeb();
            hw.UseCookies = true;
            hw.PreRequest = request =>
            {
                request.CookieContainer = _cookies;
                return true;
            };
            var baseSongPagePath = "http://www.topsinhalamp3.com/songs/";
            var baseSongDownloadPath = "http://www.topsinhalamp3.com/music-downloads/";
            var currentPage = page;
            var allSongs = new List<SinhalaSong>();
            var pageCount = 0;
            do
            {
                Console.WriteLine("Extracting songs from page: " + currentPage);
                var doc = hw.Load(currentPage);
                var songs =
                    (from link in doc.DocumentNode.SelectNodes("//a[@href]")
                     let href2 = new Uri(new Uri(page), link.Attributes["href"].Value).AbsoluteUri.ToLower()
                     where href2.StartsWith(baseSongPagePath)
                     select new SinhalaSong { Url = href2, Name = WebUtility.HtmlDecode(StripHtml(link.InnerText)) }).ToList();

                allSongs.AddRange(songs);
                pageCount++;
                var nextPageAnchor = doc.DocumentNode.SelectNodes("//a[starts-with(., 'Next ')]");
                if (nextPageAnchor == null || nextPageAnchor.Count == 0)
                    break;
                currentPage = new Uri(new Uri(page), nextPageAnchor[0].Attributes["href"].Value).AbsoluteUri.ToLower();
                Console.WriteLine("Sleeping for 3 seconds...");
                Thread.Sleep(3000);

            } while (true);

            Console.WriteLine();
            Console.WriteLine($"{allSongs.Count} song urls extracted from {pageCount} pages");
            Console.WriteLine("Extracting song ids from song urls.");
            var downloaded = 0;
            for (var i = 0; i < allSongs.Count; i++)
            {
                Console.WriteLine();
                Console.WriteLine();

                var song = allSongs[i];
                Console.WriteLine($"Downloading {song.Name} ({i + 1}/{allSongs.Count})...");
                var songSavePath = savePath + (savePath.EndsWith("\\") ? "" : "\\") + song.Name + ".mp3";

                if (File.Exists(songSavePath))
                {
                    Console.WriteLine("Song already exists on directory..");
                    downloaded++;
                    continue;
                }
                
                Console.WriteLine("Sleeping for 3 seconds...");
                Thread.Sleep(3000);
                Console.WriteLine("Downloading song page");
                var doc = hw.Load(song.Url);
                var songUrl =
                    (from link in doc.DocumentNode.SelectNodes("//a[@href]")
                     let href2 = new Uri(new Uri(page), link.Attributes["href"].Value).AbsoluteUri.ToLower()
                     where href2.StartsWith(baseSongDownloadPath)
                     select href2).FirstOrDefault();

                if (songUrl == null)
                {
                    Console.WriteLine($"Song id not found for {song.Name}");
                    continue;
                }
                Console.WriteLine("Sleeping for 3 seconds...");
                Thread.Sleep(3000);
                Console.WriteLine("Downloading dummy page");
                DownloadFile(songUrl, "", true);
                var queryString = songUrl.Substring(songUrl.IndexOf('?')).Split('#')[0];
                var queryParams = System.Web.HttpUtility.ParseQueryString(queryString);

                song.Id = queryParams["id"];
                Console.WriteLine($"{song.Name}: {song.Id}");
                var songDownloadUrl = "http://www.topsinhalamp3.com/downloads/get-mp3.php?type=mp3&mp3id=" + song.Id;
                Console.WriteLine("Sleeping for 3 seconds...");
                Thread.Sleep(3000);
                DownloadFile(songDownloadUrl, songSavePath);
                downloaded++;
            }

            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine($"{downloaded} songs downloaded from {allSongs.Count} songs");

        }

        private static void DownloadSong(string name, string url, string savePath)
        {
        }

        private static void DownloadFile(string url, string path, bool tempSaveAndDelete = false)
        {
            var sw = new Stopwatch();
            sw.Start();
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "GET";
            //webRequest.Timeout = 3000;
            webRequest.CookieContainer = _cookies;

            //long total = 0;
            //long received = 0;

            try
            {
                using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
                {
                    var resStram = webResponse.GetResponseStream();
                    if (resStram == null) return;

                    if (tempSaveAndDelete)
                    {
                        using (var reader = new StreamReader(resStram))
                        {
                            var html = reader.ReadToEnd();
                            sw.Stop();
                            Console.WriteLine($"{sw.Elapsed.ToString("g")} => {html.Length / 1024}KB");
                        }
                    }
                    else
                    {
                        using (var stream = File.Create(path))
                            resStram.CopyTo(stream);
                        sw.Stop();
                        Console.WriteLine($"{sw.Elapsed.ToString("g")} => {new FileInfo(path).Length / 1024}KB");
                    }
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                Console.WriteLine($"{sw.Elapsed.ToString("g")} => Error");
                Console.WriteLine(ex.Message);
            }
        }

        private static void DownloadFromAnanmanan(string page, string savePath)
        {
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
                 select new SinhalaSong { Id = songId, Name = WebUtility.HtmlDecode(StripHtml(link.InnerText)) }).ToList();

            songs = songs.GroupBy(s => s.Id).Select(g => g.FirstOrDefault()).ToList();
            Console.WriteLine();
            using (var client = new WebClient())
            {
                foreach (var song in songs)
                {
                    Console.WriteLine($"Downloading {song.Name}...");
                    var songSavePath = savePath + (savePath.EndsWith("\\") ? "" : "\\") + song.Name + ".mp3";

                    if (File.Exists(songSavePath))
                    {
                        Console.WriteLine("Song already exists on directory..");
                        continue;
                    }
                    var songPath = "http://www.ananmanan.lk/free-sinhala-mp3/download.php?id=" + song.Id;
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
        }

        private static string StripHtml(string input)
        {
            return Regex.Replace(input, "<.*?>", String.Empty);
        }
    }
    public class SinhalaSong
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
    }
}
