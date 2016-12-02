using SiteMapper.CommonModels;
using SiteMapper.ServerEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace SiteMapper.ServerHost
{
    public class SiteController: ApiController
    {
        public static MappingEngine Engine { get; set; }

        // GET site/www.domain.com
        [HttpGet]
        public Site GetSite(string domain, bool includeContents = false, long? contentsTimeStamp = null)
        {
            Trace.TraceInformation("Received request: GetSite, domain=" + domain + ", includeContents=" + includeContents.ToString() +
                ((contentsTimeStamp != null) ? ", contentsTimeStamp=" + contentsTimeStamp.Value.ToString() : ""));

            var site = Engine.GetSite(domain, includeContents, contentsTimeStamp, true);

            if (site == null)
            {
                new HttpResponseException(new HttpResponseMessage(HttpStatusCode.Conflict));
            }

            return site;
        }

        // GET site
        [HttpGet]
        public IEnumerable<Site> GetSites()
        {
            return Engine.GetSites();
        }

        // DELETE site/www.domain.com
        [HttpDelete]
        public void DeleteSite(string domain)
        {
            if (!Engine.DeleteSite(domain))
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
        }
    }
}
