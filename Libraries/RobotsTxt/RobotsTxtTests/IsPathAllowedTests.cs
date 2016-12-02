using System;
using NUnit.Framework;
using RobotsTxt;

namespace RobotsTxtTests
{
    [TestFixture]
    class IsPathAllowedTests
    {
        private string nl = Environment.NewLine;

        [Test, Category("IsPathAllowed")]
        public void IsPathAllowed_EmptyUserAgent_ThrowsArgumentException(
            [Values("", " ")]string userAgent, // white space considered empty
            [Values("")]string path)
        {
            string s = "User-agent: *" + nl + "Disallow: /";
            Robots r = new Robots(s);
            Assert.Throws<ArgumentException>(() => r.IsPathAllowed(userAgent, path));
        }

        [Test, Category("IsPathAllowed")]
        public void IsPathAllowed_RuleWithoutUserAgent_True()
        {
            string s = "Disallow: /";
            Robots r = Robots.Load(s);
            Assert.True(r.IsPathAllowed("*", "/foo"));
        }

        [Test, Category("IsPathAllowed")]
        public void IsPathAllowed_WithoutRules_True(
            [Values("*", "some robot")] string userAgent,
            [Values("", "/", "/file.html", "/directory/")] string path)
        {
            Robots r = new Robots(String.Empty);
            Assert.True(r.IsPathAllowed(userAgent, path));
        }

        [Test, Category("IsPathAllowed")]
        public void IsPathAllowed_WithoutAccessRule_True(
            [Values("*", "some robot")] string userAgent,
            [Values("", "/", "/file.html", "/directory/")] string path)
        {
            string s = "User-agent: *" + nl + "Crawl-delay: 5";
            Robots r = new Robots(s);
            Assert.True(r.IsPathAllowed(userAgent, path));
        }

        [Test, Category("IsPathAllowed")]
        public void IsPathAllowed_NoRulesForRobot_True(
            [Values("", "/", "/file.html", "/directory/")] string path)
        {
            string s = "User-agent: Slurp" + nl + "Disallow: /";
            Robots r = new Robots(s);
            Assert.True(r.IsPathAllowed("some robot", path));
        }

        [Test, Category("IsPathAllowed"), Description("User-agent string match should be case-insensitive.")]
        public void IsPathAllowed_NoGlobalRules_False(
            [Values("Slurp", "slurp", "Exabot", "exabot")] string userAgent,
            [Values("", "/", "/file.html", "/directory/")] string path)
        {
            string s = "User-agent: Slurp" + nl + "Disallow: /" + nl + "User-agent: Exabot" + nl + "Disallow: /";
            Robots r = new Robots(s);
            Assert.False(r.IsPathAllowed(userAgent, path));
        }

        [Test, Category("IsPathAllowed")]
        public void IsPathAllowed_UserAgentStringCaseInsensitive_False(
            [Values("Slurp", "slurp", "Exabot", "exabot", "FigTree/0.1 Robot libwww-perl/5.04")] string userAgent)
        {
            string s = 
@"User-agent: Slurp
Disallow: /
User-agent: Exabot
Disallow: /
User-agent: Exabot
Disallow: /
User-agent: figtree
Disallow: /";
            Robots r = Robots.Load(s);
            Assert.False(r.IsPathAllowed(userAgent, "/dir"));
        }

        [Test, Category("IsPathAllowed")]
        public void IsPathAllowed_OnlyDisallow_False(
            [Values("/help", "/help.ext", "/help/", "/help/file.ext", "/help/dir/", "/help/dir/file.ext")] string path)
        {
            string s = @"User-agent: *" + nl + "Disallow: /help";
            Robots r = new Robots(s);
            Assert.False(r.IsPathAllowed("*", path));
        }

        [Test, Category("IsPathAllowed")]
        public void IsPathAllowed_AllowAndDisallow_True(
            [Values("foo", "/dir/file.ext", "/dir/file.ext1")]string path)
        {
            string s = @"User-agent: *" + nl + "Allow: /dir/file.ext" + nl + "Disallow: /dir/";
            Robots r = new Robots(s);
            Assert.True(r.IsPathAllowed("*", path));
        }

        [Test, Category("IsPathAllowed")]
        public void IsPathAllowed_AllowAndDisallow_False(
            [Values("/dir/file2.ext", "/dir/", "/dir/dir/")] string path)
        {
            string s = @"User-agent: *" + nl + "Allow: /dir/file.ext" + nl + "Disallow: /dir/";
            Robots r = new Robots(s);
            Assert.False(r.IsPathAllowed("*", path));
        }

        [Test, Category("IsPathAllowed"), Sequential]
        public void IsPathAllowed_PathShouldBeCaseSensitive_True(
            [Values("/dir/file.ext", "/dir/file.ext", "/*/file.html", "/*.gif$")] string rule,
            [Values("/dir/File.ext", "/Dir/file.ext", "/a/File.html", "a.GIF")] string path)
        {
            string s = @"User-agent: *" + nl + "Disallow: " + rule;
            Robots r = Robots.Load(s);
            Assert.True(r.IsPathAllowed("*", path));
        }

        [Test, Category("IsPathAllowed")]
        public void IsPathAllowed_DollarWildcard_True(
            [Values("asd", "a.gifa", "a.gif$")] string path)
        {
            string s = @"User-agent: *" + nl + "Disallow: /*.gif$";
            Robots r = Robots.Load(s);
            Assert.True(r.IsPathAllowed("*", path));
        }

        [Test, Category("IsPathAllowed")]
        public void IsPathAllowed_DollarWildcard_False(
            [Values("a.gif", "foo.gif", "b.a.gif", "a.gif.gif")]string path)
        {
            string s = @"User-agent: *" + nl + "Disallow: /*.gif$";
            Robots r = Robots.Load(s);
            Assert.False(r.IsPathAllowed("*", path));
        }

        [TestCase("/*/file.html", "/foo/", Result = true)]
        [TestCase("/*/file.html", "file.html", Result = true)]
        [TestCase("/*/file.html", "/foo/file2.html", Result = true)]
        [TestCase("/*/file.html", "/a/file.html", Result = false)]
        [TestCase("/*/file.html", "/dir/file.html", Result = false)]
        [TestCase("/*/file.html", "//a//file.html", Result = false, Description = "The path should be normalized to \"/a/file.html\"")]
        [TestCase("/*/file.html", "/a/a/file.html", Result = false)]
        [TestCase("/*/file.html", "/a/a/file.htmlz", Result = false)]
        [TestCase("/*/file.html", "///f.html", Result = true, Description = "The path should be normalized to \"/f.html\"")]
        [TestCase("/*/file.html", "/\\/f.html", Result = true)]
        [TestCase("/*/file.html", "/:/f.html", Result = true)]
        [TestCase("/*/file.html", "/*/f.html", Result = true)]
        [TestCase("/*/file.html", "/?/f.html", Result = true)]
        [TestCase("/*/file.html", "/\"/f.html", Result = true)]
        [TestCase("/*/file.html", "/</f.html", Result = true)]
        [TestCase("/*/file.html", "/>/f.html", Result = true)]
        [TestCase("/*/file.html", "/|/f.html", Result = true)]
        [TestCase("/private*/", "/private/", Result = false)]
        [TestCase("/private*/", "/Private/", Result = true)]
        [TestCase("/private*/", "/private/f.html", Result = false)]
        [TestCase("/private*/", "/private/dir/", Result = false)]
        [TestCase("/private*/", "/private/dir/f.html", Result = false)]
        [TestCase("/private*/", "/private1/", Result = false)]
        [TestCase("/private*/", "/Private1/", Result = true)]
        [TestCase("/private*/", "/private1/f.html", Result = false)]
        [TestCase("/private*/", "/private1/dir/", Result = false)]
        [TestCase("/private*/", "/private1/dir/f.html", Result = false)]
        [Test, Category("IsPathAllowed")]
        public bool IsPathAllowed_StarWildcard(string rule, string path)
        {
            string s = @"User-agent: *" + nl + "Disallow: " + rule;
            Robots r = Robots.Load(s);
            return r.IsPathAllowed("*", path);
        }
    }
}