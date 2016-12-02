using SiteMapper.CommonModels;
using SiteMapper.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SiteMapper.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(SiteAddressModel siteAddress)
        {
            if (ModelState.IsValid)
            {
                if ((siteAddress.Address == null) || !SiteInfo.IsValidDomain(SiteInfo.NormalizeDomain(siteAddress.Address)))
                {
                    ModelState.AddModelError("", "Invalid domain");
                    return View(siteAddress);
                }

                return RedirectToAction("ShowMap", "Map", new { domain = SiteInfo.NormalizeDomain(siteAddress.Address) });
            }

            return View(siteAddress);
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Crawler()
        {
            return View();
        }
    }
}