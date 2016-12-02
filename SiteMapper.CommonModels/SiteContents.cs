using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteMapper.CommonModels
{
    public class SiteContents
    {
        public List<Page> Pages { get; set; }
        public List<Link> Links { get; set; }

        public string SerializeToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static SiteContents DeserializeFromJson(string st)
        {
            return JsonConvert.DeserializeObject<SiteContents>(st);
        }
    }
}
