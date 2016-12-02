using System;
using System.Globalization;

namespace RobotsTxt
{
    internal class CrawlDelayRule : Rule
    {
        public long Delay { get; private set; } // milliseconds

        public CrawlDelayRule(String userAgent, Line line, int order)
            : base(userAgent, order)
        {
            double delay = 0;
            Double.TryParse(line.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out delay);
            Delay = (long)(delay * 1000);
        }
    }
}