using Microsoft.WindowsAzure.ServiceRuntime;
using SiteMapper.Client;
using SiteMapper.CommonModels;
using SiteMapper.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SiteMapper.Web.Controllers
{
    public class MapController : Controller
    {
        // GET: /map/domain.com
        public ActionResult ShowMap(string domain)
        {
            if (String.IsNullOrWhiteSpace(domain))
            {
                return HttpNotFound();
            }

            var model = new SiteMapModel();
            model.Domain = domain;

            return View(model);
        }

        [HttpGet]
        [OutputCache(VaryByParam = "*", Duration = 0)]
        public ActionResult MapData(string domain, long? contentsTimeStamp = null)
        {
            if (String.IsNullOrWhiteSpace(domain))
            {
                return HttpNotFound();
            }

            using (var client = CommonFactory.CreateMappingClient())
            {
                var site = client.GetSite(domain, true, contentsTimeStamp);
                if (site == null)
                {
                    return HttpNotFound();
                }

                var model = new SiteMapDataModel(site);

                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }
	}
}