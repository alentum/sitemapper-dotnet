using SiteMapper.CommonModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace SiteMapper.Client
{
    public interface IMappingClient : IDisposable
    {
        Site GetSite(string domain, bool includeContents, long? contentsTimeStamp = null);
        IEnumerable<Site> GetSites();
        bool DeleteSite(string domain);
        bool UpdateSiteInfo(SiteInfo info);
    }
}
