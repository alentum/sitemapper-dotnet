using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using SiteMapper.ServerHost;
using SiteMapper.AzureDAL;

namespace SiteMapper.AzureWorker
{
    public class WorkerRole : RoleEntryPoint
    {
        private AzureServerHost _host;

        public override bool OnStart()
        {
            Trace.TraceInformation("Starting Worker Role", "Information");

            _host = new AzureServerHost();

            return base.OnStart();
        }

        public override void Run()
        {
            _host.Run();
        }

        public override void OnStop()
        {
            Trace.TraceInformation("Stopping Worker Role", "Information");
            _host.Close();

            base.OnStop();
        }
    }
}
