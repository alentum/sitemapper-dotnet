using SiteMapper.CommonModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SiteMapper.ServerDAL
{
    public class FileSiteRepository : ISiteRepository
    {
        private string _siteContentsDirName = "SiteContents";
        private string _siteInfoDirName = "SiteInfo";
        private string _siteFileExt = ".json";
        private string _siteContentsDirPath;
        private string _siteInfoDirPath;

        public FileSiteRepository()
        {
            string dir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            _siteContentsDirPath = Path.Combine(dir, _siteContentsDirName);
            _siteInfoDirPath = Path.Combine(dir, _siteInfoDirName);

            if (!Directory.Exists(_siteContentsDirPath))
            {
                Directory.CreateDirectory(_siteContentsDirPath);
            }

            if (!Directory.Exists(_siteInfoDirPath))
            {
                Directory.CreateDirectory(_siteInfoDirPath);
            }
        }

        private string GetFullSiteContentsFileName(string domain)
        {
            return Path.Combine(_siteContentsDirPath, domain) + _siteFileExt;
        }

        private string GetFullSiteInfoFileName(string domain)
        {
            return Path.Combine(_siteInfoDirPath, domain) + _siteFileExt;
        }

        private bool SaveSiteContentsToFile(string domain, SiteContents siteContents, bool overwrite)
        {
            if (!SiteInfo.IsValidDomain(domain))
            {
                return false;
            }

            string fileName = GetFullSiteContentsFileName(domain);

            if (!overwrite && File.Exists(fileName))
            {
                return false;
            }

            int retryCount = 3;

            do
            {
                try
                {
                    using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        using (var writer = new StreamWriter(stream))
                        {
                            writer.Write(siteContents.SerializeToJson());
                        }
                    }

                    return true;
                }
                catch (IOException)
                {
                    retryCount--;

                    if (retryCount == 0)
                    {
                        return false;
                    }

                    Thread.Sleep(2);
                }
                catch
                {
                    return false;
                }
            }
            while (retryCount > 0);

            return true;
        }

        private SiteContents LoadSiteContentsFromFile(string domain)
        {
            if (!SiteInfo.IsValidDomain(domain))
            {
                return null;
            }

            string fileName = GetFullSiteContentsFileName(domain);

            if (!File.Exists(fileName))
            {
                return null;
            }

            int retryCount = 3;

            do
            {
                try
                {
                    using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            string st = reader.ReadToEnd();
                            return SiteContents.DeserializeFromJson(st);
                        }
                    }
                }
                catch (IOException)
                {
                    retryCount--;

                    if (retryCount == 0)
                    {
                        return null;
                    }

                    Thread.Sleep(2);
                }
                catch
                {
                    return null;
                }
            }
            while (retryCount > 0);

            return null;
        }

        private bool SaveSiteInfoToFile(SiteInfo siteInfo, bool overwrite)
        {
            if (siteInfo == null)
            {
                return false;
            }

            if (!SiteInfo.IsValidDomain(siteInfo.Domain))
            {
                return false;
            }

            string fileName = GetFullSiteInfoFileName(siteInfo.Domain);

            if (!overwrite && File.Exists(fileName))
            {
                return false;
            }

            int retryCount = 3;

            do
            {
                try
                {
                    using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        using (var writer = new StreamWriter(stream))
                        {
                            writer.Write(siteInfo.SerializeToJson());
                        }
                    }

                    return true;
                }
                catch (IOException)
                {
                    retryCount--;

                    if (retryCount == 0)
                    {
                        return false;
                    }

                    Thread.Sleep(2);
                }
                catch
                {
                    return false;
                }
            }
            while (retryCount > 0);

            return true;
        }

        private SiteInfo LoadSiteInfoFromFile(string domain)
        {
            if (!SiteInfo.IsValidDomain(domain))
            {
                return null;
            }

            string fileName = GetFullSiteInfoFileName(domain);

            if (!File.Exists(fileName))
            {
                return null;
            }

            int retryCount = 3;

            do
            {
                try
                {
                    using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            string st = reader.ReadToEnd();
                            return SiteInfo.DeserializeFromJson(st);
                        }
                    }
                }
                catch (IOException)
                {
                    retryCount--;

                    if (retryCount == 0)
                    {
                        return null;
                    }

                    Thread.Sleep(2);
                }
                catch
                {
                    return null;
                }
            }
            while (retryCount > 0);

            return null;
        }

        public bool SaveSite(Site site, bool overwriteExisting = false)
        {
            if (!SaveSiteInfoToFile(site.Info, overwriteExisting))
            {
                return false;
            }

            if (site.Contents != null)
            {
                return SaveSiteContentsToFile(site.Info.Domain, site.Contents, overwriteExisting);
            }
            else
            {
                return true;
            }
        }

        public bool UpdateSiteInfo(SiteInfo siteInfo)
        {
            if (File.Exists(GetFullSiteInfoFileName(siteInfo.Domain)))
            {
                return SaveSiteInfoToFile(siteInfo, true);
            }
            else
            {
                return false;
            }
        }

        public Site GetSite(string domain, bool includeContents = true, long? contentsTimeStamp = null)
        {
            var site = new Site();
            site.Info = LoadSiteInfoFromFile(domain);

            if (site.Info == null)
            {
                return null;
            }

            if (includeContents && ((contentsTimeStamp == null) || (site.Info.StatusTime == null) || 
                (contentsTimeStamp.Value != site.Info.StatusTime.Value.ToBinary())))
            {
                site.Contents = LoadSiteContentsFromFile(domain);
            }

            return site;
        }

        public List<string> GetDomains()
        {
            var files = Directory.GetFiles(_siteInfoDirPath, "*" + _siteFileExt);
            var domains = new List<string>();

            foreach (var file in files)
            {
                domains.Add(Path.GetFileNameWithoutExtension(file));
            }

            domains.Sort();

            return domains;
        }

        public List<Site> GetSites()
        {
            var sites = new List<Site>();

            var domains = GetDomains();
            foreach (var domain in domains)
            {
                var site = GetSite(domain, false);
                if (site != null)
                {
                    sites.Add(site);
                }
            }

            sites.Sort((x, y) => x.Info.Domain.CompareTo(y.Info.Domain));

            return sites;
        }

        public bool SiteExists(string domain)
        {
            if (!SiteInfo.IsValidDomain(domain))
            {
                return false;
            }

            return File.Exists(GetFullSiteInfoFileName(domain));
        }

        public bool RemoveSite(string domain)
        {
            if (!SiteInfo.IsValidDomain(domain))
            {
                return false;
            }

            try
            {
                File.Delete(GetFullSiteInfoFileName(domain));
                File.Delete(GetFullSiteContentsFileName(domain));
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool RemoveAllSites()
        {
            try
            {
                var files = Directory.GetFiles(_siteInfoDirPath, "*" + _siteFileExt);
                foreach (var file in files)
                {
                    File.Delete(Path.Combine(_siteInfoDirPath, file));
                }

                files = Directory.GetFiles(_siteContentsDirPath, "*" + _siteFileExt);
                foreach (var file in files)
                {
                    File.Delete(Path.Combine(_siteContentsDirPath, file));
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
