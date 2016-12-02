using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;
using SiteMapper.CommonModels;
using SiteMapper.ServerDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SiteMapper.AzureDAL
{
    public class AzureSiteRepository : ISiteRepository
    {
        private string _siteContentsContainterName = "sitecontents";
        private string _siteInfoTableName = "siteinfo";
        private string _siteProcessingQueueName = "processsites";
        private string _siteFileExt = ".json";

        public AzureSiteRepository()
        {
            // Create the contents container if it doesn't already exist.
            var containter = GetContentsBlobContainer();
            containter.CreateIfNotExists();

            // Create the info table if it doesn't already exist.
            var table = GetInfoTable();
            table.CreateIfNotExists();

            // Create the process queue if it doesn't already exist.
            var queue = GetProcessQueue();
            queue.CreateIfNotExists();
        }

        private CloudBlobContainer GetContentsBlobContainer()
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            blobClient.RetryPolicy = new LinearRetry(TimeSpan.FromMilliseconds(5), 3);

            // Retrieve a reference to a container. 
            return blobClient.GetContainerReference(_siteContentsContainterName);
        }

        private CloudTable GetInfoTable()
        {
            // Retrieve the storage account from the connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            tableClient.RetryPolicy = new LinearRetry(TimeSpan.FromMilliseconds(5), 3);

            // Retrieve a table
            return tableClient.GetTableReference(_siteInfoTableName);
        }

        private CloudQueue GetProcessQueue()
        {
            // Retrieve the storage account from the connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the queue client.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a queue.
            return queueClient.GetQueueReference(_siteProcessingQueueName);
        }

        private bool SaveSiteContents(string domain, SiteContents siteContents, bool overwrite)
        {
            if (!SiteInfo.IsValidDomain(domain))
            {
                return false;
            }

            var container = GetContentsBlobContainer();
            var blob = container.GetBlockBlobReference(domain + _siteFileExt);

            if (!overwrite && blob.Exists())
            {
                return false;
            }

            try
            {
                blob.UploadText(siteContents.SerializeToJson());
            }
            catch
            {
                return false;
            }

            return true;
        }

        private bool DeleteSiteContents(string domain)
        {
            if (!SiteInfo.IsValidDomain(domain))
            {
                return false;
            }

            var container = GetContentsBlobContainer();
            var blob = container.GetBlockBlobReference(domain + _siteFileExt);

            try
            {
                blob.Delete();
            }
            catch
            {
                return false;
            }

            return true;
        }

        private SiteContents LoadSiteContents(string domain)
        {
            if (!SiteInfo.IsValidDomain(domain))
            {
                return null;
            }

            var container = GetContentsBlobContainer();
            var blob = container.GetBlockBlobReference(domain + _siteFileExt);

            if (!blob.Exists())
            {
                return null;
            }

            try
            {
                string st = blob.DownloadText();
                return SiteContents.DeserializeFromJson(st);
            }
            catch
            {
                return null;
            }
        }

        private bool SaveSiteInfo(SiteInfo siteInfo, bool overwrite)
        {
            if (siteInfo == null)
            {
                return false;
            }

            if (!SiteInfo.IsValidDomain(siteInfo.Domain))
            {
                return false;
            }

            var table = GetInfoTable();
            TableOperation operation;
            var entity = new SiteInfoEntity(siteInfo);

            if (overwrite)
            {
                operation = TableOperation.InsertOrReplace(entity);
            }
            else
            {
                operation = TableOperation.Insert(entity);
            }

            try
            {
                var res = table.Execute(operation);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private SiteInfo LoadSiteInfo(string domain)
        {
            if (!SiteInfo.IsValidDomain(domain))
            {
                return null;
            }

            var table = GetInfoTable();
            TableOperation operation = TableOperation.Retrieve<SiteInfoEntity>(SiteInfoEntity.StaticPartitionKey, domain);

            try
            {
                var result = table.Execute(operation);
                var entity = result.Result as SiteInfoEntity;

                if (entity == null)
                {
                    return null;
                }

                return entity.ToSiteInfo();
            }
            catch
            {
                return null;
            }
        }

        private bool DeleteSiteInfo(string domain)
        {
            if (!SiteInfo.IsValidDomain(domain))
            {
                return false;
            }

            var table = GetInfoTable();

            try
            { 
                // Create a retrieve operation
                TableOperation retrieveOperation = TableOperation.Retrieve<SiteInfoEntity>(SiteInfoEntity.StaticPartitionKey, domain);

                // Execute the operation.
                TableResult retrievedResult = table.Execute(retrieveOperation);

                // Assign the result to a CustomerEntity.
                var deleteEntity = (SiteInfoEntity)retrievedResult.Result;

                // Create the Delete TableOperation.
                if (deleteEntity != null)
                {
                    TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

                    // Execute the operation.
                    table.Execute(deleteOperation);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool SaveSite(Site site, bool overwriteExisting = false)
        {
            if (!SaveSiteInfo(site.Info, overwriteExisting))
            {
                return false;
            }

            if (site.Contents != null)
            {
                return SaveSiteContents(site.Info.Domain, site.Contents, overwriteExisting);
            }
            else
            {
                return true;
            }
        }

        public bool UpdateSiteInfo(SiteInfo siteInfo)
        {
            if (SiteExists(siteInfo.Domain))
            {
                return SaveSiteInfo(siteInfo, true);
            }
            else
            {
                return false;
            }
        }

        public Site GetSite(string domain, bool includeContents = true, long? contentsTimeStamp = null)
        {
            var site = new Site();
            site.Info = LoadSiteInfo(domain);

            if (site.Info == null)
            {
                return null;
            }

            if (includeContents && ((contentsTimeStamp == null) || (site.Info.StatusTime == null) || 
                (contentsTimeStamp.Value != site.Info.StatusTime.Value.ToBinary())))
            {
                site.Contents = LoadSiteContents(domain);
            }

            return site;
        }

        public List<string> GetDomains()
        {
            var domains = new List<string>();
            var table = GetInfoTable();

            // Construct the query operation
            var query = new TableQuery<SiteInfoEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, SiteInfoEntity.StaticPartitionKey));

            foreach (var entity in table.ExecuteQuery(query))
            {
                domains.Add(entity.RowKey);
            }

            return domains;
        }

        public List<Site> GetSites()
        {
            var sites = new List<Site>();
            var table = GetInfoTable();

            // Construct the query operation
            var query = new TableQuery<SiteInfoEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, SiteInfoEntity.StaticPartitionKey));

            foreach (var entity in table.ExecuteQuery(query))
            {
                var site = new Site();
                site.Info = entity.ToSiteInfo();

                sites.Add(site);
            }

            return sites;
        }

        public bool SiteExists(string domain)
        {
            if (!SiteInfo.IsValidDomain(domain))
            {
                return false;
            }

            return LoadSiteInfo(domain) != null;
        }

        public bool RemoveSite(string domain)
        {
            if (!SiteInfo.IsValidDomain(domain))
            {
                return false;
            }

            if (!DeleteSiteInfo(domain))
                return false;

            DeleteSiteContents(domain);

            return true;
        }

        public bool RemoveAllSites()
        {
            try
            {
                var containter = GetContentsBlobContainer();
                containter.Delete();

                var table = GetInfoTable();
                table.Delete();
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool QueueSiteForProcessing(string domain)
        {
            try
            {
                var queue = GetProcessQueue();

                var message = new CloudQueueMessage(domain);
                queue.AddMessage(message);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public int GetProcessQueueSize()
        {
            try
            {
                var queue = GetProcessQueue();
                queue.FetchAttributes();

                return queue.ApproximateMessageCount ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        public string GetNextSiteForProcessing()
        {
            try
            {
                var queue = GetProcessQueue();
                CloudQueueMessage retrievedMessage = queue.GetMessage();

                if (retrievedMessage == null)
                {
                    return null;
                }

                queue.DeleteMessage(retrievedMessage);

                return retrievedMessage.AsString;
            }
            catch
            {
                return null;
            }
        }
    }
}
