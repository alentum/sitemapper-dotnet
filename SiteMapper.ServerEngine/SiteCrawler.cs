using HtmlAgilityPack;
using RobotsTxt;
using SiteMapper.CommonModels;
using SiteMapper.ServerDAL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SiteMapper.ServerEngine
{
    public class SiteCrawler
    {
        public int DesiredNumberOfPages { get; set; }
        //Crawl delay in milliseconds
        public int CrawlDelay { get; set; }
        public int MaxSimultaneousRequests { get; set; }

        public const string UserAgent = "Mozilla/5.0 (compatible; VSMCrawler; http://www.visualsitemapper.com/crawler/)";
        public const string UserAgentForRobotsTxt = "VSMCrawler";

        private string _domain;
        private string _rawDomain;
        public string Domain { get { return _domain; } }
        
        private ISiteRepository _siteRepository;
        private CancellationToken _cancelToken;

        private Site _site;
        private List<Task> _pageTasks;
        private Dictionary<Page, HashSet<string>> _foundLinks;
        private Dictionary<string, Page> _pageCache;
        private object _listsLock = new object();
        private int _processedPages;
        private int _lastProcessedPages;
        private int _lastSavedPages;
        private string _processingProblemDescription;
        private readonly string _binaryExtensions = "arc arj bin com csv dll exe gz pdf rar tar txt zip bz2 cab msi gif jpg jpeg png mpeg mpg iso js css";
        private Robots _robots;
        private bool _cannotProcessRoot;
        private bool _connectionProblem;
        private bool _robotsTxtProblem;

        public SiteCrawler(string domain, ISiteRepository siteRepository, CancellationToken cancelToken)
        {
            Trace.TraceInformation("Crawler: Starting processing domain " + domain);

            if (!SiteInfo.IsValidDomain(domain))
            {
                throw new ArgumentException("Invalid domain");
            }

            if (siteRepository == null)
            {
                throw new ArgumentNullException("siteRepository is null");
            }

            if (cancelToken == null)
            {
                throw new ArgumentNullException("cancelToken is null");
            }

            _domain = domain;
            _rawDomain = _domain.StartsWith("www.") ? _domain.Substring(4) : _domain;

            _siteRepository = siteRepository;
            _cancelToken = cancelToken;

            DesiredNumberOfPages = 220;
            CrawlDelay = 100;
            MaxSimultaneousRequests = 20;

            _binaryExtensions = " " + _binaryExtensions + " ";
        }

        public void Crawl()
        {
            _site = new Site();
            _site.Info.Domain = _domain;
            _site.Info.Status = SiteStatus.Processing;
            _site.Contents = new SiteContents();
            _site.Contents.Pages = new List<Page>();
            _site.Contents.Links = new List<Link>();
            _pageTasks = new List<Task>();
            _pageCache = new Dictionary<string,Page>();
            _foundLinks = new Dictionary<Page,HashSet<string>>();
            _processingProblemDescription = null;
            _processedPages = 0;
            _lastProcessedPages = 0;
            _lastSavedPages = 0;
            _cannotProcessRoot = false;
            _connectionProblem = false;
            _robotsTxtProblem = false;

            SaveSite();

            // Retrieve robots.txt
            RetrieveRobotsTxt();

            if (_cancelToken.IsCancellationRequested)
            {
                _siteRepository.RemoveSite(Domain);
                return;
            }

            // Add initial page
            AddPage("http://" + _domain + "/");

            Task[] taskArr;

            while (!_cancelToken.IsCancellationRequested)
            {
                if (!AddPagesForProcessing())
                {
                    break;
                }

                lock (_listsLock)
                {
                    taskArr = _pageTasks.ToArray(); 
                }

                Task.WaitAny(taskArr);

                if (!_cancelToken.IsCancellationRequested)
                {
                    lock (_listsLock)
                    {
                        if (_processedPages > _lastProcessedPages + 20)
                        {
                            SaveSite();
                            _lastProcessedPages = _processedPages;
                        }
                    }
                }
            }

            lock (_listsLock)
            {
                taskArr = _pageTasks.ToArray();
            }

            Task.WaitAll(taskArr);

            if (_cancelToken.IsCancellationRequested)
            {
                _siteRepository.RemoveSite(Domain);
                Trace.TraceInformation("Crawler: Cancellation requested, deleting site " + _domain);
            }
            else
            {
                if (_connectionProblem)
                {
                    _site.Info.Status = SiteStatus.ConnectionProblem;
                }
                else if (_robotsTxtProblem)
                {
                    _site.Info.Status = SiteStatus.RobotsTxtProblem;
                }
                else
                {
                    _site.Info.Status = (_processingProblemDescription == null) ? SiteStatus.Processed : SiteStatus.ProcessedWithProblems;
                }

                _site.Info.StatusDescription = _processingProblemDescription;
                var savedSite = SaveSite();

                Trace.TraceInformation(String.Format("Crawler: finished processing domain {0}, saved {1} pages and {2} links",
                                        _domain, savedSite.Info.PageCount, savedSite.Info.LinkCount));
            }
        }

        private bool AddPagesForProcessing()
        {
            bool added = false;
            List<Page> pagesToAdd;
            int pagesToAddCount;

            if ((_lastSavedPages >= DesiredNumberOfPages) || (_processedPages >= DesiredNumberOfPages * 2))
            {
                return false;
            }

            lock (_listsLock)
            {
                pagesToAddCount = MaxSimultaneousRequests -_pageTasks.Count;

                if (pagesToAddCount > 0)
                {
                    pagesToAdd = _site.Contents.Pages
                        .Where(p => p.Status == PageStatus.Unprocessed)
                        .OrderBy(p => p.DistanceFromRoot)
                        .Take(pagesToAddCount)
                        .ToList();
                }
                else
                {
                    return true;
                }
            }

            foreach (var page in pagesToAdd)
            {
                lock (_listsLock)
                {
                    var pageToProcess = page;
                    pageToProcess.Status = PageStatus.Processing;

                    var task = Task.Run(() => ProcessSinglePage(pageToProcess));
                    _pageTasks.Add(task);
                    task.ContinueWith(t =>
                    {
                        lock (_listsLock) { _pageTasks.Remove(task); };
                    });
                }

                added = true;

                Thread.Sleep(CrawlDelay);
            }
            
            if (added)
            {
                return true;
            }
            else
            {
                // Check if some tasks exist, so more page may be added in the future
                lock (_listsLock)
                {
                    return _pageTasks.Count > 0;
                }
            }
        }

        private void ProcessSinglePage(Page page)
        {
            if (_cancelToken.IsCancellationRequested)
                return;

            if (page.Status != PageStatus.Processing)
                return;

            Interlocked.Increment(ref _processedPages);

            if (_processedPages > DesiredNumberOfPages * 2)
            {
                return;
            }

            Uri pageUrl;

            try
            {
                pageUrl = new Uri(page.URL, UriKind.Absolute);
            }
            catch
            {
                return;
            }

            if ((_robots != null) && !_robots.IsPathAllowed(UserAgentForRobotsTxt, pageUrl.AbsolutePath))
            {
                page.Status = PageStatus.UnprocessedBecauseOfRobotsTxt;
                if (page.Id == 0) // Root
                {
                    _cannotProcessRoot = true;
                    _robotsTxtProblem = true;
                    _processingProblemDescription = "Cannot process the site because of the robots.txt settings";
                }
                return;
            }

            List<string> links = null;

            #region Retrieving page

            // Getting page
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(pageUrl);
            request.Method = "GET";
            request.AllowAutoRedirect = false;
            request.UserAgent = UserAgent;
            request.Timeout = 40 * 1000;
            HttpWebResponse response = null;

            using (_cancelToken.Register(() => request.Abort()))
            {
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException ex)
                {
                    if (ex.Response != null)
                    {
                        page.HttpStatus = (int)((HttpWebResponse)ex.Response).StatusCode;
                    }

                    page.Status = PageStatus.Error;
                    if (response != null)
                        response.Close();

                    if (page.Id == 0) // Root
                    {
                        _cannotProcessRoot = true;
                        _processingProblemDescription = "Cannot get the home page of this site";
                    }

                    return;
                }
                catch
                {
                    page.Status = PageStatus.Error;

                    if (response != null)
                        response.Close();

                    if (page.Id == 0) // Root
                    {
                        _cannotProcessRoot = true;
                        _connectionProblem = true;
                        _processingProblemDescription = "Cannot get the home page of this site";
                    }

                    return;
                }
            }

            using (response)
            {
                if (_cancelToken.IsCancellationRequested)
                    return;

                // Parsing page
                string title = String.Empty;
                page.HttpStatus = (int)response.StatusCode;

                //Redirects
                if ((page.HttpStatus == 301) || (page.HttpStatus == 302) || (page.HttpStatus == 303) || (page.HttpStatus == 307) || (page.HttpStatus == 308))
                {
                    links = new List<string>();

                    string location = response.Headers["location"];

                    if (!String.IsNullOrWhiteSpace(location))
                    {
                        var link = GetAbsoluteURL(pageUrl, location);

                        if ((page.Id == 0) && IsExternalLink(link)) // Root
                        {
                            _cannotProcessRoot = true;
                            _processingProblemDescription = "Home page of this site is redirected to another domain (" + link + ")";
                        }

                        if (!String.IsNullOrEmpty(link))
                        {
                            links.Add(link);
                        }
                    }
                }
                else
                {
                    try
                    {
                        // Success
                        if ((page.HttpStatus >= 200) && (page.HttpStatus < 300))
                        {
                            if (!response.ContentType.StartsWith("text/html", StringComparison.InvariantCultureIgnoreCase) &&
                                !response.ContentType.StartsWith("application/xhtml+xml", StringComparison.InvariantCultureIgnoreCase))
                            {
                                page.Status = PageStatus.Binary;
                                response.Close();

                                if (page.Id == 0) // Root
                                {
                                    _cannotProcessRoot = true;
                                    _processingProblemDescription = "Cannot get the home page of this site";
                                }

                                return;
                            }

                            links = GetLinksAndTitleFromHtmlDocument(response.GetResponseStream(), pageUrl, response.CharacterSet, out title);
                        }
                        // Error
                        else if (page.Id == 0) // Root
                        {
                            _cannotProcessRoot = true;
                            _processingProblemDescription = "Cannot get the home page of this site";
                        }
                    }
                    catch
                    {
                        links = new List<string>();
                    }
                }

                // Adding links
                if ((links != null) && (links.Count > 0))
                {
                    var linkHash = _foundLinks[page];
                    foreach (var link in links)
                    {
                        try
                        {
                            var uri = new Uri(link, UriKind.Absolute);
                            if (!IsExternalLink(link) && !IsBinaryExtension(Path.GetExtension(uri.AbsoluteUri)))
                            {
                                linkHash.Add(link);
                            }
                        }
                        catch
                        {
                        }
                    }

                    ProcessAddedLinks(page, linkHash);
                }

                page.Status = PageStatus.Processed;
                page.Title = title;
            }

            #endregion
        }

        private string NormalizeUrlForDomain(string url)
        {
            var uri = new Uri(url);
            var st = uri.Host.ToLower();

            if (st == _domain)
            {
                return url;
            }

            // The same domain (+/- www)
            if ((st == _rawDomain) || (st.StartsWith("www.") && (st.Substring(4) == _rawDomain)))
            {
                int i = url.IndexOf(st, StringComparison.OrdinalIgnoreCase);
                if (i != -1)
                {
                    url = url.Remove(i, st.Length);
                    url = url.Insert(i, _domain);
                }
            }

            return url;
        }

        private void ProcessAddedLinks(Page page, HashSet<string> links)
        {
            foreach (var link in links)
            {
                var linkedPage = GetPage(link);

                if (linkedPage == page)
                {
                    continue;
                }

                if (linkedPage == null)
                {
                    linkedPage = AddPage(link);
                    linkedPage.DistanceFromRoot = page.DistanceFromRoot + 1;
                }
                else
                {
                    lock (_listsLock)
                    {
                        if (linkedPage.DistanceFromRoot > page.DistanceFromRoot + 1)
                        {
                            linkedPage.DistanceFromRoot = page.DistanceFromRoot + 1;
                        }
                    }
                }

                lock (_listsLock)
                {
                    var linkToAdd = new Link();
                    linkToAdd.StartPageId = page.Id;
                    linkToAdd.EndPageId = linkedPage.Id;

                    _site.Contents.Links.Add(linkToAdd);
                }
            }
        }

        private bool IsBinaryExtension(string ext)
        {
            ext = ext.ToLower().Trim(' ', '.');

            return !String.IsNullOrEmpty(ext) && _binaryExtensions.Contains(ext);
        }

        private string GetCharsetFromHtmlStream(Stream htmlStream)
        {
            //HTML Agility Pack reads only <meta http-equiv="content-type" ... /> while it is also necessary to read <meta charset ... />

            try
            {
                HtmlDocument doc = new HtmlDocument();
                doc.Load(htmlStream);

                var metas = doc.DocumentNode.SelectNodes("//head/meta");
                foreach (var meta in metas)
                {
                    string charset;

                    //<meta charset="utf-8"> 
                    var attr = meta.Attributes["charset"];
                    if (attr != null)
                    {
                        charset = attr.Value;
                        if (!String.IsNullOrWhiteSpace(charset))
                        {
                            return charset;
                        }
                    }

                    //<meta http-equiv="content-type" content="text/html; charset=utf-8">
                    attr = meta.Attributes["http-equiv"];
                    if ((attr != null) && (String.Compare(attr.Value, "content-type", true) == 0))
                    {
                        var content = meta.Attributes["content"];
                        if (content != null)
                        {
                            var pairs = content.Value.Split();
                            foreach (var pair in pairs)
                            {
                                string st = pair.Trim();

                                int i = st.IndexOf('=');
                                if (i > 0)
                                {
                                    string name = st.Substring(0, i).Trim();
                                    string value = st.Substring(i + 1).Trim();

                                    if (String.Compare(name, "charset", true) == 0)
                                    {
                                        if (!String.IsNullOrWhiteSpace(value))
                                        {
                                            return value;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        private List<string> GetLinksAndTitleFromHtmlDocument(Stream htmlStream, Uri baseUri, string characterSet, out string title)
        {
            var links = new HashSet<string>();
            string currentUrl = baseUri.ToString();
            title = String.Empty;

            var stream = new MemoryStream();
            htmlStream.CopyTo(stream);

            try
            {
                stream.Seek(0, SeekOrigin.Begin);
                string docCharset = GetCharsetFromHtmlStream(stream);

                stream.Seek(0, SeekOrigin.Begin);
                var doc = new HtmlDocument();

                Encoding encoding = null;

                try
                {
                    encoding = Encoding.GetEncoding(docCharset ?? characterSet);
                }
                catch
                {
                }

                doc.Load(stream, encoding ?? Encoding.UTF8);

                var baseNode = doc.DocumentNode.SelectSingleNode("//head/base");
                if (baseNode != null)
                {
                    var href = baseNode.Attributes["href"];
                    if ((href != null) && !String.IsNullOrWhiteSpace(href.Value))
                    {
                        string st = href.Value.Trim(' ', '/');

                        if (!st.StartsWith("http://") && !st.StartsWith("https://"))
                            st = "http://" + st;

                        try
                        {
                            baseUri = new Uri(st, UriKind.Absolute);
                        }
                        catch
                        {
                        }
                    }
                }

                var titleNode = doc.DocumentNode.SelectSingleNode("//head/title");
                if (titleNode != null)
                {
                    title = WebUtility.HtmlDecode(titleNode.InnerText);
                }

                var rawUrls = new List<string>();

                rawUrls.AddRange(from link in doc.DocumentNode.Descendants()
                                 where link.Name == "a" &&
                                     link.Attributes["href"] != null
                                 select link.Attributes["href"].Value);

                var frames = doc.DocumentNode.SelectNodes("//frameset/frame");
                if (frames != null)
                {
                    rawUrls.AddRange(from frame in frames
                                     where frame.Attributes["src"] != null
                                     select frame.Attributes["src"].Value);
                }

                foreach (var url in rawUrls)
                {
                    if (url.Trim().StartsWith("javascript", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    try
                    {
                        string link = WebUtility.HtmlDecode(url.Trim());

                        link = GetAbsoluteURL(baseUri, link);

                        if ((link == currentUrl) || String.IsNullOrEmpty(link))
                            continue;

                        int iHash = link.IndexOf('#');
                        if (iHash != -1)
                            link = link.Substring(0, iHash);

                        if (((link.StartsWith("http://") || (link.StartsWith("https://")) && !links.Contains(link))))
                            links.Add(link);
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
                links.Clear();
            }

            return new List<string>(links);
        }

        private string GetAbsoluteURL(Uri baseUri, string link)
        {
            //Protocol relative URL
            if (link.StartsWith("//"))
            {
                return baseUri.Scheme + ":" + link;
            }
            else
            {
                try
                {
                    return (new Uri(baseUri, link)).ToString();
                }
                catch
                {
                    return null;
                }
            }
        }

        private Page AddPage(string url)
        {
            lock (_listsLock)
            {
                Page page;

                if (!_pageCache.TryGetValue(url, out page))
                {
                    page = new Page();
                    page.URL = url;
                    page.Id = _site.Contents.Pages.Count;
                    page.Status = PageStatus.Unprocessed;
                    _pageCache[url] = page;
                    _foundLinks[page] = new HashSet<string>();
                    _site.Contents.Pages.Add(page);
                }

                return page;
            }
        }

        private Page GetPage(string url)
        {
            Page page;

            lock (_listsLock)
            {
                if (_pageCache.TryGetValue(url, out page))
                {
                    return page;
                }
                else
                {
                    return null;
                }
            }
        }

        private Site SaveSite()
        {
            Site site = new Site();
            site.Info.Domain = _site.Info.Domain;
            site.Info.Progress = (_site.Info.Status == SiteStatus.Processing) ? Math.Min(99, (_processedPages * 100 / (DesiredNumberOfPages * 2))) : 100;
            site.Info.Status = _site.Info.Status;
            site.Info.StatusDescription = _site.Info.StatusDescription;
            site.Info.StatusTime = DateTime.UtcNow;

            if (_cannotProcessRoot)
            {
                _siteRepository.SaveSite(site, true);
                return site;
            }

            // Creating contents
            site.Contents = new SiteContents();

            Dictionary<int, Page> idsToPages = new Dictionary<int, Page>();
            Dictionary<string, Page> urlsToPages = new Dictionary<string, Page>();

            // Adding pages to dictionaries
            site.Contents.Pages = new List<Page>();
            foreach (var page in _site.Contents.Pages)
            {
                if ((page.Status == PageStatus.Processed) || (page.Status == PageStatus.Error))
                {
                    idsToPages[page.Id] = page;
                    urlsToPages[page.URL] = page;
                }
            }

            // Adding links
            HashSet<long> linkHash = new HashSet<long>();
            foreach (var link in _site.Contents.Links)
            {
                if (idsToPages.ContainsKey(link.StartPageId) && idsToPages.ContainsKey(link.EndPageId))
                {
                    int id1 = link.StartPageId, id2 = link.EndPageId;

                    var page = idsToPages[id1];
                    string st = NormalizeUrlForDomain(page.URL);
                    if ((st != page.URL) && (urlsToPages.ContainsKey(st)))
                    {
                        id1 = urlsToPages[st].Id;
                    }

                    page = idsToPages[id2];
                    st = NormalizeUrlForDomain(page.URL);
                    if ((st != page.URL) && (urlsToPages.ContainsKey(st)))
                    {
                        id2 = urlsToPages[st].Id;
                    }

                    if (id1 != id2)
                    {
                        linkHash.Add(((long)id2 << 32) | (uint)id1);
                    }
                }
            }

            site.Contents.Links = new List<Link>();
            foreach (var pair in linkHash)
            {
                site.Contents.Links.Add(new Link { StartPageId = (int)(pair & uint.MaxValue), EndPageId = (int)(pair >> 32) });
            }

            // Adding page from this domain
            foreach (var page in idsToPages.Values)
            {
                string st = NormalizeUrlForDomain(page.URL);
                if (st == page.URL)
                {
                    site.Contents.Pages.Add(page);
                }
            }

            // Adding non-duplicate pages from the domain with/without www
            foreach (var page in idsToPages.Values)
            {
                string st = NormalizeUrlForDomain(page.URL);
                if (st != page.URL)
                {
                    if (!urlsToPages.ContainsKey(st))
                    {
                        var pageToAdd = page.Clone();
                        pageToAdd.URL = st;
                        site.Contents.Pages.Add(pageToAdd);
                    }
                    else
                    {
                        var existingPage = urlsToPages[st];
                        if (String.IsNullOrEmpty(existingPage.Title) && !String.IsNullOrEmpty(page.Title))
                        {
                            site.Contents.Pages.Remove(existingPage);
                            var pageToAdd = page.Clone();
                            pageToAdd.Id = existingPage.Id;
                            pageToAdd.URL = st;
                            site.Contents.Pages.Add(pageToAdd);
                        }
                    }
                }
            }

            site.Info.PageCount = site.Contents.Pages.Count;
            site.Info.LinkCount = site.Contents.Links.Count;

            _lastSavedPages = site.Info.PageCount;

            site.Info.Progress = (_site.Info.Status == SiteStatus.Processing) ? Math.Min(99, (site.Contents.Pages.Count * 100 / DesiredNumberOfPages)) : 100;

            _siteRepository.SaveSite(site, true);

            return site;
        }

        private void RetrieveRobotsTxt()
        {
            _robots = null;

            var request = (HttpWebRequest)WebRequest.Create("http://" + _domain + "/robots.txt");
            request.Method = "GET";
            request.AllowAutoRedirect = true;
            request.UserAgent = UserAgent;
            request.Timeout = 30 * 1000;
            HttpWebResponse response = null;

            using (_cancelToken.Register(() => request.Abort()))
            {
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch
                {
                    return;
                }
            }

            using (response)
            {
                if (_cancelToken.IsCancellationRequested)
                    return;

                try
                {
                    using (var streamReader = new StreamReader(response.GetResponseStream(), true))
                    {
                        _robots = new Robots(streamReader.ReadToEnd());
                    }
                }
                catch
                {
                    _robots = null;
                }
            }
        }

        private string GetRawDomain(string url)
        {
            try
            {
                var uri = new Uri(url);
                var st = uri.Host.ToLower();

                if (st.StartsWith("www."))
                {
                    st = st.Substring(4);
                }

                return st;
            }
            catch
            {
                return url;
            }
        }

        private bool IsExternalLink(string link)
        {
            var domain = GetRawDomain(link);

            // The same domain (+/- www)
            if (domain == _rawDomain)
            {
                return false;
            }

            // Subdomain
            int i = domain.LastIndexOf("." + _rawDomain);
            if ((i > 0) && (i == domain.Length - _rawDomain.Length - 1))
            {
                return false;
            }

            // External
            return true;
        }
    }
}
