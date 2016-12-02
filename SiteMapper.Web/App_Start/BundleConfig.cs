using System.Web;
using System.Web.Optimization;

namespace SiteMapper.Web
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js",
                        "~/Scripts/jquery.cookie.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/respond.js"));

            bundles.Add(new ScriptBundle("~/bundles/d3").Include(
                      "~/Scripts/d3.js",
                      "~/Scripts/jquery.tipsy.js",
                      "~/Scripts/seedrandom.js"));

            bundles.Add(new ScriptBundle("~/bundles/sitemapper").Include(
                      "~/Scripts/sitemapper.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/non-responsive.css",
                      "~/Content/site.css"));

            bundles.Add(new StyleBundle("~/Content/d3css").Include(
                      "~/Content/tipsy.css"));
        }
    }
}
