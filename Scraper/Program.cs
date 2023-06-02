using Scraper;

Console.WriteLine("Initializing...");

var client = new BooksToScrapeClient(@"D:\Temp");
//var client = new CachedBooksToScrapeClient(@"D:\Temp");

var scraper = new Scraper.Scraper(client, new Parser());
var initialBatch = await scraper.Init();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"Index.html scraped, found {initialBatch} initial files to process. Press any key to start!");
Console.CursorVisible = false; Console.ReadKey(true);

using (scraper.ProgressReporter()) 
    scraper.ProcessAll(threadCount: 32);

Console.ResetColor();
Console.WriteLine("Processing done! Open scraped site? (Y/N)");

if (Console.ReadKey(true).Key == ConsoleKey.Y)
    System.Diagnostics.Process.Start("explorer.exe", Path.Combine(@"D:\Temp", "index.html"));
