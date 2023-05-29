Console.WriteLine("Initializing...");
Scraper.Scraper.Destination = @"D:\Temp";
int initialBatch = await Scraper.Scraper.Init();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"Index.html scraped, found {initialBatch} initial files to process. Press any key to start!");
Console.CursorVisible = false; Console.ReadKey(true);

Scraper.Scraper.Process(threads: 16);
Console.ResetColor();
Console.WriteLine("Processing done! Open scraped site? (Y/N)");

if (Console.ReadKey(true).Key == ConsoleKey.Y)
    System.Diagnostics.Process.Start("explorer.exe", Path.Combine(Scraper.Scraper.Destination, "index.html"));
