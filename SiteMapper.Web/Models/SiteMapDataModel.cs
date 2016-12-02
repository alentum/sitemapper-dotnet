using SiteMapper.CommonModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SiteMapper.Web.Models
{
    public class SiteMapDataModel
    {
        public string domain { get; set; }
        public string status { get; set; }
        public bool processing { get; set; }
        public long? contentsTimeStamp { get; set; }
        public List<object> nodes { get; set; }
        public List<object> links { get; set; }

        private const int maxNodesToShow = 200;

        private string GetFolder(string fileName)
        {
            int i = fileName.LastIndexOf("/");
            if (i >= 0)
            {
                return fileName.Substring(0, i + 1);
            }
            else
            {
                return fileName;
            }
        }

        private string GetSiteStatus(Site site)
        {
            // Problem with this domain
            if (site == null)
            {
                return "Cannot get information on this domain";
            }

            if ((site.Info.Status == SiteStatus.Processing) || (site.Info.Status == SiteStatus.Added))
            {
                return "Processing: " + site.Info.Progress.ToString() + "%";
            }

            return null;
        }

        public SiteMapDataModel(Site site)
        {
            // Domain & status
            domain = site.Info.Domain;
            status = GetSiteStatus(site) ?? site.Info.StatusDescription;
            processing = (site.Info.Status == SiteStatus.Processing) || (site.Info.Status == SiteStatus.Added);
            if (site.Info.StatusTime != null)
            {
                contentsTimeStamp = site.Info.StatusTime.Value.ToBinary();
            }

            // Nodes & links
            if ((site.Contents == null) || (site.Contents.Links == null) || (site.Contents.Pages == null))
            {
                site.Contents = new SiteContents();
                site.Contents.Pages = new List<Page>();
                site.Contents.Links = new List<Link>();
            }

            if (site.Contents.Pages.Count == 0)
            {
                // Error with processing, no pages available
                if (!processing)
                {
                    return;
                }

                site.Contents.Pages.Add(new Page
                {
                    Id = 0,
                    URL = "http://" + domain + "/",
                    Status = PageStatus.Processed,
                    HttpStatus = 0,
                    DistanceFromRoot = 0
                });
            }

            nodes = new List<object>();
            links = new List<object>();
            var pages = new List<Page>();

            #region Preparing top pages to show

            int level = 0;
            do
            {
                var currentLevelPages = new List<Page>();

                foreach (var page in site.Contents.Pages)
                {
                    if ((page.DistanceFromRoot == level) ||
                        ((level > 20) && ((page.DistanceFromRoot > 20) || (page.DistanceFromRoot < 0))))
                    {
                        currentLevelPages.Add(page);
                    }
                }

                if (pages.Count + currentLevelPages.Count <= maxNodesToShow)
                {
                    pages.AddRange(currentLevelPages);
                }
                else
                {
                    if (pages.Count < maxNodesToShow / 2)
                    {
                        int i = 0;
                        while (pages.Count < maxNodesToShow)
                        {
                            pages.Add(currentLevelPages[i]);
                            i++;
                        }
                    }

                    break;
                }

                level++;
            }
            while (pages.Count < site.Contents.Pages.Count);

            #endregion

            #region Preparing nodes

            var pageIndexes = new Dictionary<int, int>();
            var pageGroupes = new Dictionary<string, int>();
            int index = 0;
            int groupCount = 0;

            foreach (var page in pages)
            {
                string path = GetFolder(page.URL);

                int group;
                if (!pageGroupes.TryGetValue(path, out group))
                {
                    group = groupCount;
                    pageGroupes[path] = group;
                    groupCount++;
                }

                string errorInfo = GetErrorByHttpStatus(page.HttpStatus);

                if (errorInfo != null)
                {
                    nodes.Add(new
                    {
                        title = page.Title,
                        url = page.URL,
                        group = group,
                        error = errorInfo
                    });
                }
                else
                {
                    nodes.Add(new
                    {
                        title = page.Title,
                        url = page.URL,
                        group = group
                    });
                }

                pageIndexes[page.Id] = index;
                index++;
            }

            #endregion

            #region Links

            foreach (var link in site.Contents.Links)
            {
                int startNodeIndex, endNodeIndex;

                if (pageIndexes.TryGetValue(link.StartPageId, out startNodeIndex) &&
                    pageIndexes.TryGetValue(link.EndPageId, out endNodeIndex))
                {
                    var mapLink = new
                    {
                        source = startNodeIndex,
                        target = endNodeIndex
                    };

                    links.Add(mapLink);
                }
            }

            #endregion

            if ((status == null) && (pages.Count < site.Contents.Pages.Count))
            {
                status = "Top " + pages.Count.ToString() + " pages are shown";
            }
        }

        private string GetErrorByHttpStatus(int? httpStatus)
        {
            if (httpStatus == null)
            {
                return "Cannot retrieve the page";
            }

            if (httpStatus < 400)
            {
                return null;
            }

            switch (httpStatus)
            {
                case 400: return "400 Bad Request";
                case 401: return "401 Unauthorized";
                case 402: return "402 Payment Required";
                case 403: return "403 Forbidden";
                case 404: return "404 Not Found";
                case 405: return "405 Method Not Allowed";
                case 406: return "406 Not Acceptable";
                case 407: return "407 Proxy Authentication Required";
                case 408: return "408 Request Timeout";
                case 409: return "409 Conflict";
                case 410: return "410 Gone";
                case 411: return "411 Length Required";
                case 412: return "412 Precondition Failed";
                case 413: return "413 Request Entity Too Large";
                case 414: return "414 Request-URI Too Long";
                case 415: return "415 Unsupported Media Type";
                case 416: return "416 Requested Range Not Satisfiable";
                case 417: return "417 Expectation Failed";
                case 500: return "500 Internal Server Error";
                case 501: return "501 Not Implemented";
                case 502: return "502 Bad Gateway";
                case 503: return "503 Service Unavailable";
                case 504: return "504 Gateway Timeout";
                case 505: return "505 HTTP Version Not Supported";
                case 511: return "511 Network Authentication Required";
                default: return "Unknown error";
            }
        }
    }
}