using SiteMapper.CommonModels;
using SiteMapper.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace SiteMapper.Web.Controllers
{
    [OutputCache(VaryByParam = "*", Duration = 0)]
    #if !DEBUG
    [RequireHttps]
    #endif
    public class AdminController : Controller
    {
        [Authorize]
        public ActionResult Index(string sortOrder)
        {
            using (var client = CommonFactory.CreateMappingClient())
            {
                IEnumerable<Site> siteList = client.GetSites();

                IEnumerable<AdminSiteModel> modelList = null;
                if (siteList != null)
                {
                    modelList = siteList.Select(s => new AdminSiteModel(s));
                }
                else
                {
                    modelList = new List<AdminSiteModel>();
                }

                // Sorting
                ViewBag.DomainSortParm = ((sortOrder == "domain") || String.IsNullOrEmpty(sortOrder)) ? "domain_desc" : "domain";
                ViewBag.ProgressSortParm = (sortOrder == "progress") ? "progress_desc" : "progress";
                ViewBag.StatusSortParm = (sortOrder == "status") ? "status_desc" : "status";
                ViewBag.StatusTimeSortParm = (sortOrder == "statustime") ? "statustime_desc" : "statustime";
                ViewBag.PageCountSortParm = (sortOrder == "pagecount") ? "pagecount_desc" : "pagecount";
                ViewBag.LinkCountSortParm = (sortOrder == "linkcount") ? "linkcount_desc" : "linkcount";
                ViewBag.RefreshEnabledSortParm = (sortOrder == "refreshenabled") ? "refreshenabled_desc" : "refreshenabled";

                switch (sortOrder)
                {
                    case "domain":
                        modelList = modelList.OrderBy(m => m.Domain);
                        break;
                    case "domain_desc":
                        modelList = modelList.OrderByDescending(m => m.Domain);
                        break;
                    case "progress":
                        modelList = modelList.OrderBy(m => m.Progress);
                        break;
                    case "progress_desc":
                        modelList = modelList.OrderByDescending(m => m.Progress);
                        break;
                    case "status":
                        modelList = modelList.OrderBy(m => m.Status);
                        break;
                    case "status_desc":
                        modelList = modelList.OrderByDescending(m => m.Status);
                        break;
                    case "statustime":
                        modelList = modelList.OrderBy(m => m.StatusTime);
                        break;
                    case "statustime_desc":
                        modelList = modelList.OrderByDescending(m => m.StatusTime);
                        break;
                    case "pagecount":
                        modelList = modelList.OrderBy(m => m.PageCount);
                        break;
                    case "pagecount_desc":
                        modelList = modelList.OrderByDescending(m => m.PageCount);
                        break;
                    case "linkcount":
                        modelList = modelList.OrderBy(m => m.LinkCount);
                        break;
                    case "linkcount_desc":
                        modelList = modelList.OrderByDescending(m => m.LinkCount);
                        break;
                    case "refreshenabled":
                        modelList = modelList.OrderBy(m => m.RefreshEnabled);
                        break;
                    case "refreshenabled_desc":
                        modelList = modelList.OrderByDescending(m => m.RefreshEnabled);
                        break;
                    default:
                        modelList = modelList.OrderBy(m => m.Domain);
                        break;
                }

                var model = new AdminSiteListModel
                {
                    Sites = modelList,
                    SortOrder = sortOrder
                };

                return View(model);
            }
        }

        [HttpGet]
        [Authorize]
        public ActionResult Delete(string domain, string sortOrder)
        {
            using (var client = CommonFactory.CreateMappingClient())
            {
                if (!client.DeleteSite(domain))
                    return HttpNotFound();

                return RedirectToAction("Index", new { sortorder = sortOrder });
            }
        }

        [HttpGet]
        [Authorize]
        public ActionResult ChangeRefreshEnabled(string domain, string sortOrder, bool refreshEnabled)
        {
            using (var client = CommonFactory.CreateMappingClient())
            {
                var site = client.GetSite(domain, false);
                if (site == null)
                {
                    return HttpNotFound();
                }

                site.Info.RefreshEnabled = refreshEnabled;
                if (!client.UpdateSiteInfo(site.Info))
                {
                    return HttpNotFound();
                }

                return RedirectToAction("Index", new { sortorder = sortOrder });
            }
        }

        public ViewResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                #pragma warning disable
                bool result = FormsAuthentication.Authenticate(model.UserName, model.Password);
                #pragma warning restore

                if (result)
                {
                    FormsAuthentication.SetAuthCookie(model.UserName, true);
                    return Redirect(returnUrl ?? Url.Action("Index", "Admin"));
                }
                else
                {
                    ModelState.AddModelError("", "Incorrect username or password");
                    return View();
                }
            }
            else
            {
                return View();
            }
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }
    }
}