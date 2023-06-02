using System.Text;

namespace Scraper
{
    public interface IParser
    {
        IEnumerable<string> FindQuotedStrings(string input);
    }
    public sealed class Parser : IParser
    {
        static readonly char[] QuotationChars = new[] { '"', '\'' };
        public IEnumerable<string> FindQuotedStrings(string input)
        {
            StringBuilder buffer = new(); char? stringStartChar = null;
            foreach (var c in input)
            {
                if (QuotationChars.Contains(c))
                {
                    if (stringStartChar == null) stringStartChar = c;
                    else
                    {
                        yield return buffer.ToString();
                        buffer.Clear(); stringStartChar = null;
                    }
                }
                else if (stringStartChar != null) buffer.Append(c);
            }
        }
    }
}
