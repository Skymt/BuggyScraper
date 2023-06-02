using System.Collections.Concurrent;

namespace Scraper
{
    public interface IScraper
    {
        Task<int> Init();
        Task Process(string url);
        void ProcessAll(int threadCount);
    }
    public class Scraper : IScraper
    {
        readonly ConcurrentQueue<string> _workload = new();
        readonly ConcurrentDictionary<string, bool> _process = new();

        readonly IClient _client; readonly IParser _parser;
        
        public Scraper(IClient client, IParser parser) => (_client, _parser) = (client, parser);
        
        public async Task<int> Init()
        {
            await Process("index.html");
            return _workload.Count;
        }
        public async Task Process(string url)
        {
            if (_client.CanParse(url))
            {
                var content = await _client.DownloadText(url);
                var quotedStrings = _parser.FindQuotedStrings(content);
                foreach(var link in quotedStrings.Where(_client.CanDownload))
                {
                    var absolutePath = _client.ToAbsolutePath(url, link);
                    if(_process.TryAdd(absolutePath, false))
                        _workload.Enqueue(absolutePath);
                }
                await _client.SaveText(url, content);
            }
            else
            {
                var content = await _client.DownloadFile(url);
                await _client.SaveFile(url, content);
            }
            _process[url] = true;
        }
        public void ProcessAll(int threadCount)
        {
            var threads = Enumerable.Range(1, threadCount).Select(workerThread);
            async Task workerThread(int threadId)
            {
                while(true)
                {
                    if (_workload.TryDequeue(out var link))
                    {
                        try
                        {
                            await Process(link);
                            Log($"Thread {threadId} processed {link}", ConsoleColor.Green);
                        }
                        catch
                        {
                            Log($"Thread {threadId} failed to process {link}", ConsoleColor.Red);
                        }
                        continue;
                    }
                    await Task.Delay(100);
                    if (!_workload.TryDequeue(out var retryLink)) break;
                    await Process(retryLink);
                    Log($"Thread {threadId} was delayed but processed {retryLink}", ConsoleColor.DarkYellow);
                }
                Log($"Thread {threadId} has exited due to lack of work", ConsoleColor.DarkGreen);
            }

            Task.WaitAll(threads.ToArray());
        }

        static readonly object consoleLock = new();
        static void Log(string message, ConsoleColor color)
        {
            lock (consoleLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
            }
        }

        public IDisposable ProgressReporter() => new WorkloadReporter(_process);
        class WorkloadReporter : IDisposable
        {
            readonly Timer timer;
            public WorkloadReporter(ConcurrentDictionary<string, bool> process) 
            {
                int currentCompleted = 0;
                timer = new(_ =>
                {
                    var newCurrentCompleted = process.Where(kvp => kvp.Value).Count();
                    if (newCurrentCompleted != currentCompleted)
                    {
                        currentCompleted = newCurrentCompleted;
                        Log($"Current workload: {process.Count:0000}, Completed: {currentCompleted}", ConsoleColor.Cyan);
                    }
                }, null, 50, 100);

            }

            public void Dispose() => timer.Dispose();
        }
    }
}
