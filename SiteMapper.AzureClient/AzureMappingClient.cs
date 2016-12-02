using SiteMapper.AzureDAL;
using SiteMapper.Client;
using SiteMapper.CommonModels;
using SiteMapper.ServerDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SiteMapper.AzureClient
{
    public class AzureMappingClient : IMappingClient
    {
        private AzureSiteRepository _siteRepository;

        private int _refreshPeriodInDays = 7;
        public int RefreshPeriodInDays
        {
            get { return _refreshPeriodInDays; }
        }

        public AzureMappingClient()
        {
            _siteRepository = new AzureSiteRepository();
        }

        public void Dispose()
        {
        }

        public Site GetSite(string domain, bool includeContents, long? contentsTimeStamp = null)
        {
            domain = SiteInfo.NormalizeDomain(domain);

            if (!SiteInfo.IsValidDomain(domain))
            {
                throw new ArgumentException("Invalid domain");
            }

            var site = _siteRepository.GetSite(domain, includeContents, contentsTimeStamp);

            bool needToProcess = site == null;
            // Need to process as info is too old
            needToProcess = needToProcess || ((site != null) && (DateTime.UtcNow - site.Info.StatusTime > TimeSpan.FromDays(RefreshPeriodInDays)));
            // Need to process as there was a connection or robots.txt error
            needToProcess = needToProcess || ((site != null) && 
                                                ((site.Info.Status == SiteStatus.ConnectionProblem) || (site.Info.Status == SiteStatus.RobotsTxtProblem)) &&
                                                (DateTime.UtcNow - site.Info.StatusTime > TimeSpan.FromMinutes(10)));
            // Need to process as processing seems to be interrupted
            needToProcess = needToProcess || ((site != null) && ((site.Info.Status == SiteStatus.Added) || (site.Info.Status == SiteStatus.Processing)) &&
                                                (DateTime.UtcNow - site.Info.StatusTime > TimeSpan.FromHours(1)));

            if ((site != null) && !site.Info.RefreshEnabled)
            {
                needToProcess = false;
            }

            if (needToProcess)
            {
                site = new Site();
                site.Info.Domain = domain;

                if (_siteRepository.SaveSite(site, true))
                {
                    _siteRepository.QueueSiteForProcessing(domain);
                }
                else
                {
                    site = null;
                }
            }

            return site;
        }

        public IEnumerable<Site> GetSites()
        {
            return _siteRepository.GetSites();
        }

        public bool DeleteSite(string domain)
        {
            return _siteRepository.RemoveSite(domain);
        }

        public bool UpdateSiteInfo(SiteInfo info)
        {
            return _siteRepository.UpdateSiteInfo(info);
        }
    }
}
