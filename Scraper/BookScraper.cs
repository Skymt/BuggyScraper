using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper
{
    internal static class Scraper
    {
        public static string Destination = @"C:\Temp";
        static readonly ConcurrentQueue<string> _workload = new();
        static readonly ConcurrentDictionary<string, bool> _process = new();
        static readonly HttpClient _client = new() { BaseAddress = new("http://books.toscrape.com/") };
        static readonly string[] _textExtensions = new[] { ".html", ".css", ".js" };
        static readonly string[] _fileExtension = new[] { ".jpg", ".ico", ".eot", ".woff", ".ttf", ".svg" };

        //Links to fontawesome files contain a version query string, percent encoded.
        //This must be purged for site to load from local file system
        static readonly string fontAwesomeFix = "%3Fv=3.2.1";

        /// <summary>
        /// Scrape index.html and return the amount of "interesting" links.
        /// </summary>
        /// <remarks>
        /// Calling this is optional. <see cref="Process(int)"/> will work regardless.
        /// </remarks>
        /// <returns>The initial amount of files that should be processed</returns>
        public static async Task<int> Init()
        {
            _workload.Enqueue("index.html");
            await ProcessWorkItem(-1);
            return _workload.Count;
        }

        /// <summary>
        /// Start threaded processing of the workload.
        /// </summary>
        /// <remarks>
        /// If <see cref="Init"/> has not been called, root index.html will be evaluated as first item!
        /// </remarks>
        /// <param name="threads">The amount of threads to do work.</param>
        public static void Process(int threads = 1)
        {
            // Ensure threads is set to valid value
            if (threads < 1) throw new ArgumentOutOfRangeException(nameof(threads));

            if (!_workload.Any())
            {
                _workload.Enqueue("index.html");
                ProcessWorkItem().Wait();
            }

            int currentCompleted = 1;

            using var _ = new Timer(_ =>
            {
                var newCurrentCompleted = _process.Where(kvp => kvp.Value).Count();
                if (newCurrentCompleted != currentCompleted)
                {
                    currentCompleted = newCurrentCompleted;
                    Log($"Current workload: {_workload.Count:0000}, Completed: {currentCompleted}", ConsoleColor.Red);
                }
            }, null, 50, 100);

            Task.WaitAll(Enumerable.Range(1, threads).Select(worker).ToArray());
            Console.ResetColor();
            return;

            static async Task worker(int threadId)
            {
                int retries = 3;
                do
                {
                    if (await ProcessWorkItem(threadId))
                        retries = 3;
                    else
                    {
                        Log($"No work found for thread {threadId}... retrying ({retries--})", ConsoleColor.DarkGreen);
                        await Task.Delay(500);
                    }
                } while (retries > 0);
                Log($"Thread {threadId} exited due to lack of work.", ConsoleColor.Green);
            }
        }

        static async Task<bool> ProcessWorkItem(int threadId = 0)
        {
            if (_workload.TryDequeue(out var filePath))
            {
                FileInfo localFile = new(Path.Combine(Destination, filePath));
                localFile.Directory?.Create();

                if (_textExtensions.Contains(localFile.Extension)) await DownloadText(filePath, localFile.FullName);
                else if (_fileExtension.Contains(localFile.Extension)) await DownloadFile(filePath, localFile.FullName);
                else if (localFile.FullName.EndsWith(fontAwesomeFix)) await DownloadFile(filePath, localFile.FullName);

                Log($"Thread {threadId} processed {filePath}", ConsoleColor.White);
                return _process[filePath] = true;
            }
            return false;
        }
        static async Task DownloadText(string filePath, string localFilePath)
        {
            var content = await _client.GetStringAsync(filePath);
            Scrape(filePath, content);
            content = content.Replace(fontAwesomeFix, string.Empty);
            await File.WriteAllTextAsync(localFilePath, content);
        }
        static async Task DownloadFile(string filePath, string localFilePath)
        {
            localFilePath = localFilePath.Replace(fontAwesomeFix, string.Empty);

            var content = await _client.GetByteArrayAsync(filePath);
            await File.WriteAllBytesAsync(localFilePath, content);
        }
        static void Scrape(string filePath, string fileContent)
        {
            int newJobsCounter = 0;
            foreach (var quotedString in scanForQuotedStrings())
            {
                if (isValidFile(quotedString))
                {
                    var mergedPath = mergePath(quotedString);
                    if (!_process.ContainsKey(mergedPath) && _process.TryAdd(mergedPath, false))
                    {
                        _workload.Enqueue(mergedPath);
                        newJobsCounter++;
                    }
                }
            }
            if (newJobsCounter > 0) Log($"Added {newJobsCounter} file(s) to process!", ConsoleColor.Yellow);
            return;

            IEnumerable<string> scanForQuotedStrings()
            {
                var buffer = string.Empty; var reading = false;
                var scanChars = new[] { '"', '\'' };
                foreach (var c in fileContent)
                {
                    if (scanChars.Contains(c))
                    {
                        if (reading)
                        {
                            yield return buffer;
                            buffer = string.Empty;
                        }
                        reading = !reading;
                    }
                    else if (reading) buffer += c;
                }
            }
            static bool isValidFile(string s)
            {
                if (s.Contains("//")) return false;
                if (s.Contains(' ')) return false;
                if (!s.Contains('.')) return false;

                if (s.EndsWith(fontAwesomeFix)) return true;
                var extension = s[s.LastIndexOf('.')..];
                if (_textExtensions.Contains(extension)) return true;
                if (_fileExtension.Contains(extension)) return true;
                return false;
            }
            string mergePath(string s)
            {
                if (s.StartsWith('/')) return s;
                if (!filePath.Contains('/')) return s;

                var basePath = filePath.Split('/')[..^1];
                var relativePath = s.Split('/');
                var parents = relativePath.TakeWhile(p => p == "..").Count();

                var newPath = basePath[..^parents].Concat(relativePath[parents..]);
                return string.Join('/', newPath);
            }
        }

        static void Log(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.Write(Environment.NewLine);
        }
    }

}
