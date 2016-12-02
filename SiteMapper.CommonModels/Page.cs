using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteMapper.CommonModels
{
    public class Page
    {
        [JsonProperty(PropertyName = "i")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "u")]
        public string URL { get; set; }

        [JsonProperty(PropertyName = "t")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "d")]
        public int DistanceFromRoot { get; set; }

        [JsonProperty(PropertyName = "h")]
        public int HttpStatus { get; set; }

        [JsonProperty(PropertyName = "s")]
        public PageStatus Status { get; set; }

        public Page Clone()
        {
            return new Page
            {
                Id = this.Id,
                URL = this.URL,
                Title = this.Title,
                DistanceFromRoot = this.DistanceFromRoot,
                HttpStatus = this.HttpStatus,
                Status = this.Status
            };
        }
    }

    public enum PageStatus
    {
        Unprocessed = 0,
        Processed = 1,
        Error = 2,
        UnprocessedBecauseOfRobotsTxt = 3,
        Binary = 4,
        Processing = 5
    }
}
