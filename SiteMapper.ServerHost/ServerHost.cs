using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin.Hosting;
using SiteMapper.ServerEngine;
using SiteMapper.ServerDAL;
using System.Net;
using System.Threading;

namespace SiteMapper.ServerHost
{
    public class MappingServerHost
    {
        public MappingEngine Engine { get; private set; }
        private IDisposable _webApp = null;

        private ISiteRepository _siteRepository;

        public MappingServerHost(ISiteRepository siteRepository)
        {
            if (siteRepository == null)
            {
                throw new ArgumentNullException("siteRepository is null");
            }

            _siteRepository = siteRepository;
        }

        public bool Open(string baseAddress)
        {
            if (_webApp != null)
                return false;

            try
            {
                _webApp = WebApp.Start<MappingServerHostStartup>(url: baseAddress);
            }
            catch
            {
                return false;
            }

            ThreadPool.SetMinThreads(100, 4);
            ServicePointManager.DefaultConnectionLimit = 300;

            Engine = new MappingEngine(_siteRepository);
            SiteController.Engine = Engine;

            return true;
        }

        public void Close()
        {
            if (_webApp != null)
            {
                _webApp.Dispose();
                _webApp = null;
            }

            if (Engine != null)
            {
                Engine.Dispose();
                Engine = null;
            }
        }
    }
}
