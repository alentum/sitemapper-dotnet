using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteMapper.CommonModels
{
    public class Site
    {
        public SiteInfo Info { get; set; }
        public SiteContents Contents { get; set; }

        public Site()
        {
            Info = new SiteInfo();
        }
    }
}
