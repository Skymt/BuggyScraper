# BookScraper

BookScraper is live coding assignment for Backend candidates to be used as an interactive segment in our technical interviews. This is instruction is for the interviewers.

## Instruktioner

Open BookScraper.sln and present the code to interview the candidate. Explain to the candidate that he must debug the program for errors. Describe that BookScraper is a web scraper that will download the site http://books.toscrape.com. 
Let the candidate run the program and give him time to explore. Give tips and guidance. Refer to the page http://books.toscrape.com which should serve as the finished result when downloading it locally.

- Small formatting errors in Program.cs file. Lines 5 and 13 are missing spaces.
- Line 9 in Program.cs where candidate can enter numbers of threads. The more threads, the faster execution. For example 32 threads will make the program run for one minute.
- Hint for the candidate that the thread is always number 1. You can see this in the console window. This is because of row 39 in the Scraper.cs file that set threads to always 1.
- After the candidate has run the program. Ask him to look at the results and compare with http://books.toscrape.com. He will see that there is a lack of style and images.
  Hint that he can open the web developer window and look at the console tab for errors. He will then see a lot of broken links errors.
  - First error is on line 11 of Scraper.cs. Where ".css " has a space that causes the code to miss fetching css files (which causes no styling on the page).
  - The second error is on line 12 where it should be ".jpg" instead of ".jpeg". Which causes it to miss the JPEG images to be downloaded from the site.
  - The last error is with FontAwesome files. This because of the file contain a version query string "%3Fv=3.2.1". This must be removed so the file can be downloaded and stored locally.