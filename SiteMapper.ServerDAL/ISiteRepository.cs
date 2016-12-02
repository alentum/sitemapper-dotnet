using SiteMapper.CommonModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteMapper.ServerDAL
{
    public interface ISiteRepository
    {
        bool SaveSite(Site site, bool overwriteExisting = false);
        bool UpdateSiteInfo(SiteInfo siteInfo);
        Site GetSite(string domain, bool includeContents = true, long? contentsTimeStamp = null);
        List<string> GetDomains();
        List<Site> GetSites();
        bool SiteExists(string domain);
        bool RemoveSite(string domain);
        bool RemoveAllSites();
    }
}
