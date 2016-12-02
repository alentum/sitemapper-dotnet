using Microsoft.WindowsAzure.ServiceRuntime;
using SiteMapper.AzureClient;
using SiteMapper.AzureDAL;
using SiteMapper.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SiteMapper.Web
{
    public static class CommonFactory
    {
        public static IMappingClient CreateMappingClient()
        {
            string baseUri;

            if (RoleEnvironment.IsAvailable) // Check if running in Azure
            {
                return new AzureMappingClient();
            }
            else
            {
                baseUri = "http://localhost:9000/";

                return new WebMappingClient(baseUri);
            }
        }
    }
}