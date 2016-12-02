using SiteMapper.ServerDAL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SiteMapper.ServerEngine
{
    public class MappingCrawler : IDisposable
    {
        private ISiteRepository _siteRepository;

        private int _maxCapacity;
        public int MaxCapacity
        {
            get { return _maxCapacity; }
            set 
            {
                if ((value < 1) || (value > 10000))
                {
                    throw new ArgumentOutOfRangeException("MaxCapacity must be between 1 and 1000");
                }

                _maxCapacity = value; 
            }
        }

        private Dictionary<string, Task> _siteTasks;
        private Dictionary<string, CancellationTokenSource> _siteCancellationSources;
        private object _listsLock = new object();
        private CancellationTokenSource _cancelSource;

        public MappingCrawler(ISiteRepository siteRepository)
        {
            if (siteRepository == null)
            {
                throw new ArgumentNullException("siteRepository is null");
            }

            _siteRepository = siteRepository;
            MaxCapacity = 10;
        }

        public void Start()
        {
            if (_siteTasks != null)
            {
                throw new InvalidOperationException("Crawler is already started");
            }

            _siteTasks = new Dictionary<string, Task>();
            _cancelSource = new CancellationTokenSource();
            _siteCancellationSources = new Dictionary<string, CancellationTokenSource>();
        }

        public void Stop()
        {
            if (_siteTasks != null)
            {
                _cancelSource.Cancel();

                Task[] currentTasks;
                lock (_listsLock)
                {
                    currentTasks = _siteTasks.Values.ToArray();
                }

                Task.WaitAll(currentTasks);
                _siteTasks = null;

                foreach (var source in _siteCancellationSources.Values)
                {
                    source.Dispose();
                }

                _cancelSource.Dispose();
                _cancelSource = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }

        public bool ProcessSite(string domain)
        {
            if (_siteTasks == null)
            {
                throw new InvalidOperationException("Crawler is not started");
            }

            lock (_listsLock)
            {
                if (!_siteTasks.ContainsKey(domain))
                {
                    var source = new CancellationTokenSource();
                    _siteCancellationSources[domain] = source;
                    var linkedSources = CancellationTokenSource.CreateLinkedTokenSource(source.Token, _cancelSource.Token);

                    _siteTasks[domain] =
                            Task.Run(() => CrawlSite(domain, linkedSources.Token))
                            .ContinueWith(t =>
                            {
                                lock (_listsLock) 
                                { 
                                    _siteTasks.Remove(domain);
                                    _siteCancellationSources.Remove(domain);
                                    linkedSources.Dispose();
                                    source.Dispose();
                                };
                            });
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        // Returns number of sites that can be processed
        public int GetRemainingSiteCapacity()
        {
            return Math.Max(0, MaxCapacity - _siteTasks.Count);
        }

        private void CrawlSite(string domain, CancellationToken cancelToken)
        {
            if (_siteTasks == null)
            {
                throw new InvalidOperationException("Crawler is not started");
            }

            try
            {
                var siteCrawler = new SiteCrawler(domain, _siteRepository, cancelToken);
                siteCrawler.Crawl();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception in MappingCrawler.CrawlSite: " + ex.ToString());
            }
        }

        public void DeleteSite(string domain)
        {
            lock (_listsLock)
            {
                if ((_siteTasks != null) && _siteCancellationSources.ContainsKey(domain))
                {
                    _siteCancellationSources[domain].Cancel();
                }
            }
        }
    }
}
