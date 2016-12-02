using SiteMapper.AzureDAL;
using SiteMapper.ServerDAL;
using SiteMapper.ServerEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SiteMapper.AzureWorker
{
    class AzureServerHost
    {
        public MappingEngine Engine { get; private set; }
        private AzureSiteRepository _siteRepository;

        bool _stopping;
        bool _stopped;

        public AzureServerHost()
        {
            _siteRepository = new AzureSiteRepository();

            ThreadPool.SetMinThreads(100, 4);
            ServicePointManager.DefaultConnectionLimit = 300;

            Engine = new MappingEngine(_siteRepository);

            Engine.MaxCapacity = 5;

            _stopping = false;
            _stopped = false;
        }

        public void Run()
        {
            while (!_stopping)
            {
                if (_siteRepository.GetProcessQueueSize() > 0)
                {
                    int sitesToAdd = Engine.GetRemainingSiteCapacity();

                    while (sitesToAdd > 0)
                    {
                        var domain = _siteRepository.GetNextSiteForProcessing();

                        if (!String.IsNullOrEmpty(domain))
                        {
                            if (Engine.ProcessSite(domain))
                            {
                                sitesToAdd--;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                Thread.Sleep(1000);
            }

            _stopped = true;
        }

        public void Close()
        {
            if ((Engine != null) && !_stopped)
            {
                _stopping = true;

                while (!_stopped)
                {
                    Thread.Sleep(1000);
                }

                Engine.Dispose();
                Engine = null;
            }
        }
    }
}
