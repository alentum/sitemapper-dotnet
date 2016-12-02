using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteMapper.CommonModels
{
    public class SiteInfo
    {
        public string Domain { get; set; }
        public int Progress { get; set; }
        public SiteStatus Status { get; set; }
        public string StatusDescription { get; set; }
        public DateTime? StatusTime { get; set; }
        public int PageCount { get; set; }
        public int LinkCount { get; set; }
        public bool RefreshEnabled { get; set; }

        public SiteInfo()
        {
            Status = SiteStatus.Added;
            StatusTime = DateTime.UtcNow;
            RefreshEnabled = true;
        }

        public virtual string SerializeToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static SiteInfo DeserializeFromJson(string st)
        {
            return JsonConvert.DeserializeObject<SiteInfo>(st);
        }

        public static bool IsValidDomain(string domain)
        {
            if (!domain.Contains('.'))
            {
                return false;
            }

            return Uri.CheckHostName(domain) != UriHostNameType.Unknown;
        }

        public static string NormalizeDomain(string domain)
        {
            domain = domain.Trim(' ', '/').ToLower();

            int i = domain.IndexOf("://");
            if (i != -1)
            {
                domain = domain.Substring(i + 3);
            }

            return domain;
        }
    }
}
