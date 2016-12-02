using Microsoft.WindowsAzure.Storage.Table;
using SiteMapper.CommonModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteMapper.AzureDAL
{
    class SiteInfoEntity : TableEntity
    {
        public const string StaticPartitionKey = "info";

        public int Progress { get; set; }
        public int Status { get; set; }
        public string StatusDescription { get; set; }
        public DateTime StatusTime { get; set; }
        public int PageCount { get; set; }
        public int LinkCount { get; set; }
        public bool RefreshEnabled { get; set; }

        public SiteInfoEntity()
        {
            PartitionKey = StaticPartitionKey;
            RefreshEnabled = true;
            Status = (int)SiteStatus.Added;
            StatusTime = DateTime.UtcNow;
        }

        public SiteInfoEntity(SiteInfo info)
        {
            PartitionKey = StaticPartitionKey;
            RowKey = info.Domain;
            Progress = info.Progress;
            Status = (int)info.Status;
            StatusDescription = info.StatusDescription;
            StatusTime = info.StatusTime.HasValue ? info.StatusTime.Value : DateTime.MinValue;
            PageCount = info.PageCount;
            LinkCount = info.LinkCount;
            RefreshEnabled = info.RefreshEnabled;
        }

        public SiteInfo ToSiteInfo()
        {
            var info = new SiteInfo();

            info.Domain = RowKey;
            info.Progress = Progress;
            info.Status = (SiteStatus)Status;
            info.StatusDescription = StatusDescription;
            info.StatusTime = (StatusTime != DateTime.MinValue) ? StatusTime : (DateTime?)null;
            info.PageCount = PageCount;
            info.LinkCount = LinkCount;
            info.RefreshEnabled = RefreshEnabled;

            return info;
        }
    }
}
