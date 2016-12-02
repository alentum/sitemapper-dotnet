using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteMapper.CommonModels
{
    public class Link
    {
        [JsonProperty(PropertyName = "s")]
        public int StartPageId { get; set; }

        [JsonProperty(PropertyName = "e")]
        public int EndPageId { get; set; }
    }
}
