using SiteMapper.CommonModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SiteMapper.Web.Models
{
    public class AdminSiteModel
    {
        public string Domain { get; set; }
        public int Progress { get; set; }
        public string Status { get; set; }

        [Display(Name = "Status Time")]
        public DateTime? StatusTime { get; set; }

        [Display(Name = "Page Count")]
        public int PageCount { get; set; }

        [Display(Name = "Link Count")]
        public int LinkCount { get; set; }

        [Display(Name = "Refresh")]
        public bool RefreshEnabled { get; set; }

        public AdminSiteModel()
        {
        }

        public AdminSiteModel(Site site)
        {
            Domain = site.Info.Domain;
            Progress = site.Info.Progress;
            Status = site.Info.Status.ToString();
            StatusTime = site.Info.StatusTime;
            PageCount = site.Info.PageCount;
            LinkCount = site.Info.LinkCount;
            RefreshEnabled = site.Info.RefreshEnabled;
        }
    }
}