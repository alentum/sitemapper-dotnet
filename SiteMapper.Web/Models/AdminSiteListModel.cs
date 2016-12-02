using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SiteMapper.Web.Models
{
    public class AdminSiteListModel
    {
        public IEnumerable<AdminSiteModel> Sites { get; set; }
        public string SortOrder { get; set; }
    }
}