using SiteMapper.CommonModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SiteMapper.Client
{
    public class WebMappingClient : IMappingClient
    {
        private HttpClient _client;

        public WebMappingClient(string serverAddress)
        {
            if (serverAddress == null)
            {
                throw new ArgumentNullException("serverAddress is null");
            }

            _client = new HttpClient();
            _client.BaseAddress = new Uri(serverAddress);
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public Site GetSite(string domain, bool includeContents, long? contentsTimeStamp = null)
        {
            if (!SiteInfo.IsValidDomain(domain))
            {
                throw new ArgumentException("Invalid domain");
            }

            HttpResponseMessage response = 
                _client.GetAsync("site/" + domain + "?includeContents=" + includeContents.ToString() +
                            ((contentsTimeStamp != null) ? "&contentsTimeStamp=" + contentsTimeStamp.ToString() : "")).Result;

            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsAsync<Site>().Result;
            }

            return null;
        }

        public IEnumerable<Site> GetSites()
        {
            HttpResponseMessage response = _client.GetAsync("site").Result;
            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsAsync<IEnumerable<Site>>().Result;
            }

            return null;
        }

        public bool DeleteSite(string domain)
        {
            if (!SiteInfo.IsValidDomain(domain))
            {
                throw new ArgumentException("Invalid domain");
            }

            HttpResponseMessage response = _client.DeleteAsync("site/" + domain).Result;
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public bool UpdateSiteInfo(SiteInfo info)
        {
            throw new NotImplementedException();
        }
    }
}
