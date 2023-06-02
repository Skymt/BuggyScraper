namespace Scraper
{
    public interface IClient
    {
        bool CanDownload(string url);
        bool CanParse(string url);
        Task<string> DownloadText(string url);
        Task<byte[]> DownloadFile(string url);
        Task SaveText(string path, string content);
        Task SaveFile(string path, byte[] data);
        string ToAbsolutePath(string basePath, string relativePath);
    }
    public abstract class BaseClient : IClient
    {
        static readonly string[] _textExtensions = new[] { ".html", ".css", ".js" };
        static readonly string[] _fileExtension = new[] { ".jpeg", ".ico", ".eot", ".woff", ".ttf", ".svg" };

        readonly protected string _destination;
        public BaseClient(string destination) => _destination = destination;

        public string ToAbsolutePath(string basePath, string relativePath)
        {
            if (relativePath.StartsWith('/')) return relativePath;
            if (!basePath.Contains('/')) return relativePath;

            var baseParts = basePath.Split('/')[..^1];
            var relativeParts = relativePath.Split('/');
            var parentOffset = relativeParts.TakeWhile(p => p == "..").Count();

            var absoluteParts = baseParts[..^parentOffset].Concat(relativeParts[parentOffset..]);
            var absolutePath = string.Join('/', absoluteParts);
            return absolutePath;
        }
        public virtual bool CanDownload(string candidateUrl)
        {
            if (candidateUrl.Contains("//")) return false;
            if (candidateUrl.Contains(' ')) return false;
            if (!candidateUrl.Contains('.')) return false;
            if (candidateUrl.EndsWith('.')) return false;

            var extension = candidateUrl[candidateUrl.LastIndexOf('.')..];
            //if(!_fileExtension.Contains(extension)) return false;
            //if(!_textExtensions.Contains(extension)) return false;
            return true;
        }
        public virtual bool CanParse(string url)
        {
            var extension = url[url.LastIndexOf('.')..];
            return _textExtensions.Contains(extension);
        }
        public virtual async Task SaveFile(string url, byte[] data)
        {
            FileInfo localFile = new(Path.Combine(_destination, url));
            localFile.Directory?.Create();
            await File.WriteAllBytesAsync(localFile.FullName, data);
        }
        public virtual async Task SaveText(string url, string content)
        {
            FileInfo localFile = new(Path.Combine(_destination, url));
            localFile.Directory?.Create();
            await File.WriteAllTextAsync(localFile.FullName, content);
        }
        public abstract Task<byte[]> DownloadFile(string url);
        public abstract Task<string> DownloadText(string url);
    }

    internal sealed class BooksToScrapeClient : BaseClient
    {
        public BooksToScrapeClient(string destination) : base(destination) { }
        static readonly HttpClient _client = new() { BaseAddress = new("http://books.toscrape.com/") };
        public override async Task<byte[]> DownloadFile(string url) => await _client.GetByteArrayAsync(url);
        public override async Task<string> DownloadText(string url) => await _client.GetStringAsync(url);
    }
    internal sealed class CachedBooksToScrapeClient : BaseClient
    {
        static readonly HttpClient _client = new() { BaseAddress = new("http://books.toscrape.com/") };
        public CachedBooksToScrapeClient(string destination) : base(destination) { }
        public override async Task<byte[]> DownloadFile(string url)
        {
            var localFile = Path.Combine(_destination, url);
            if (File.Exists(localFile))
                return await File.ReadAllBytesAsync(localFile);
            return await _client.GetByteArrayAsync(url);
        }
        public override async Task<string> DownloadText(string url) 
        {
            var localFile = Path.Combine(_destination, url);
            if (File.Exists(localFile))
                return await File.ReadAllTextAsync(localFile);
            return await _client.GetStringAsync(url); 
        }
    }
}
