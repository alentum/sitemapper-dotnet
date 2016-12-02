using SiteMapper.CommonModels;
using SiteMapper.ServerDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteMapper.ServerEngine
{
    public class MappingEngine : IDisposable
    {
        private ISiteRepository _siteRepository;
        private MappingCrawler _crawler;

        private int _refreshPeriodInDays = 7;
        public int RefreshPeriodInDays
        {
            get { return _refreshPeriodInDays; }
        }

        public int MaxCapacity
        {
            get { return _crawler.MaxCapacity; }
            set { _crawler.MaxCapacity = value; }
        }

        public MappingEngine(ISiteRepository siteRepository)
        {
            if (siteRepository == null)
            {
                throw new ArgumentNullException("siteRepository is null");
            }

            _siteRepository = siteRepository;

            _crawler = new MappingCrawler(_siteRepository);
            _crawler.Start();
        }

        public void Dispose()
        {
            _crawler.Stop();
            _crawler.Dispose();
        }

        public bool SiteExists(string domain)
        {
            return _siteRepository.SiteExists(domain);
        }

        public Site GetSite(string domain, bool includeContents, long? contentsTimeStamp = null, bool createIfNecessary = false)
        {
            domain = SiteInfo.NormalizeDomain(domain);

            if (!SiteInfo.IsValidDomain(domain))
            {
                throw new ArgumentException("Invalid domain");
            }

            var site = _siteRepository.GetSite(domain, includeContents, contentsTimeStamp);

            bool needToCreate = (site == null) && createIfNecessary;
            needToCreate = needToCreate || ((site != null) && (DateTime.UtcNow - site.Info.StatusTime > TimeSpan.FromDays(RefreshPeriodInDays)));
            needToCreate = needToCreate || ((site != null) &&
                                ((site.Info.Status == SiteStatus.ConnectionProblem) || (site.Info.Status == SiteStatus.RobotsTxtProblem)) && 
                                (DateTime.UtcNow - site.Info.StatusTime > TimeSpan.FromMinutes(10)));
            var needToProcess = ((site != null) && ((site.Info.Status == SiteStatus.Added) || (site.Info.Status == SiteStatus.Processing)));

            if ((site != null) && !site.Info.RefreshEnabled)
            {
                needToProcess = false;
            }

            if (needToCreate || needToProcess)
            {
                if (needToCreate)
                {
                    site = new Site();
                    site.Info.Domain = domain;

                    if (_siteRepository.SaveSite(site, true))
                    {
                        _crawler.ProcessSite(domain);
                    }
                    else
                    {
                        site = null;
                    }
                }
                else
                {
                    _crawler.ProcessSite(domain);
                }
            }

            return site;
        }

        public bool ProcessSite(string domain)
        {
            domain = SiteInfo.NormalizeDomain(domain);

            if (!SiteInfo.IsValidDomain(domain))
            {
                throw new ArgumentException("Invalid domain");
            }

            return _crawler.ProcessSite(domain);
        }

        public List<string> GetDomains()
        {
            return _siteRepository.GetDomains();
        }

        public List<Site> GetSites()
        {
            return _siteRepository.GetSites();
        }

        public bool DeleteSite(string domain)
        {
            _crawler.DeleteSite(domain);
            return _siteRepository.RemoveSite(domain);
        }

        public int GetRemainingSiteCapacity()
        {
            return _crawler.GetRemainingSiteCapacity();
        }
    }
}
