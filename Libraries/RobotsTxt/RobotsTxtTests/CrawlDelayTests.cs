using System;
using NUnit.Framework;
using RobotsTxt;

namespace RobotsTxtTests
{
    [TestFixture]
    class CrawlDelayTests
    {
        private string nl = Environment.NewLine;

        [Test, Category("CrawlDelay")]
        public void CrawlDelay_EmptyUserAgent_ThrowsArgumentException(
            [Values("", " ")] string userAgent // white space considered empty
            )
        {
            Robots r = new Robots(String.Empty);
            Assert.Throws<ArgumentException>(() => r.CrawlDelay(userAgent));
        }

        [Test, Category("CrawlDelay")]
        public void CrawlDelay_NoRules_Zero()
        {
            Robots r = new Robots(String.Empty);
            Assert.AreEqual(0, r.CrawlDelay("*"));
        }

        [Test, Category("CrawlDelay")]
        public void CrawlDelay_NoCrawlDelayRule_Zero()
        {
            string s = @"User-agent: *" + nl + "Disallow: /dir/";
            Robots r = new Robots(s);
            Assert.AreEqual(0, r.CrawlDelay("*"));
        }

        [Test, Category("CrawlDelay")]
        public void CrawlDelay_NoRuleForRobot_Zero()
        {
            string s = @"User-agent: Slurp" + nl + "Crawl-delay: 2";
            Robots r = new Robots(s);
            Assert.AreEqual(0, r.CrawlDelay("Google"));
        }

        [Test, Category("CrawlDelay")]
        public void CrawlDelay_InvalidRule_Zero()
        {
            string s = @"User-agent: *" + nl + "Crawl-delay: foo";
            Robots r = new Robots(s);
            Assert.AreEqual(0, r.CrawlDelay("Google"));
        }

        [Test, Category("CrawlDelay")]
        public void CrawlDelay_RuleWithoutUserAgent()
        {
            string s = "Crawl-delay: 1";
            Robots r = Robots.Load(s);
            Assert.AreNotEqual(1000, r.CrawlDelay("Google"));
            Assert.AreEqual(0, r.CrawlDelay("Google"));
        }

        [Test, Category("CrawlDelay"), Sequential]
        public void CrawlDelay_ValidRule(
            [Values(2000, 2000, 500, 500)] long expected,
            [Values("Google", "google", "Slurp", "slurp")] string userAgent)
        {
            string s = @"User-agent: Google" + nl + "Crawl-delay: 2" + nl +
                "User-agent: Slurp" + nl + "Crawl-delay: 0.5";
            Robots r = new Robots(s);
            Assert.AreEqual(expected, r.CrawlDelay(userAgent));
        }
    }
}