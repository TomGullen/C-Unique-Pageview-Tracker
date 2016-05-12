    /// <summary>
    /// Counts unique page views for an entity
    /// Not 100% accurate, only tracks last x people
    /// and cache object is removed from memory every n hours
    /// Copyright (c) 2016 Scirra Ltd
    /// www.scirra.com
    /// </summary>
    public class UniquePageviewHandler
    {
        private HashSet<string> TrackedViews { get; set; }
        private Queue<string> OrderedTrackedViews { get; set; }
        private int MaximumQueueSize { get; set; }

        private UniquePageviewHandler(int maximumQueueSize)
        {
            if (maximumQueueSize <= 0) throw new Exception();
            MaximumQueueSize = maximumQueueSize;
            TrackedViews = new HashSet<string>();
            OrderedTrackedViews = new Queue<string>(maximumQueueSize);
        }

        /// <summary>
        /// Process a page view.  Returns if it's counter as unique or not.
        /// </summary>
        /// <return>If this is a new page view or not</return>
        public bool ProcessPageView(string ipAddress)
        {
            using (MiniProfiler.Current.Step("ProcessPageView"))
            {
                if (TrackedViews.Contains(ipAddress)) return false;

                if (OrderedTrackedViews.Count == MaximumQueueSize)
                {
                    var removedIP = OrderedTrackedViews.Dequeue();
                    TrackedViews.Remove(removedIP);
                }

                OrderedTrackedViews.Enqueue(ipAddress);
                TrackedViews.Add(ipAddress);

                return true;
            }
        }

        private static readonly ConcurrentDictionary<string, object> PageViewHandlerLocks = new ConcurrentDictionary<string, object>();
        /// <summary>
        /// Get the page view handler for an entity
        /// </summary>
        public static UniquePageviewHandler GetPageviewHandler(string uniqueIndentifier)
        {
            var cache = Common.GetCache();
            var cacheIndex = uniqueIndentifier;
            if (cache[cacheIndex] == null)
            {
                PageViewHandlerLocks.TryAdd(cacheIndex, new object());
                lock (PageViewHandlerLocks[cacheIndex])
                {
                    if (cache[cacheIndex] == null)
                    {
                        var tracker = new UniquePageviewHandler(Settings.Entities.UniquePageViewTrackingMaxSize);
                        cache.Add(cacheIndex,
                            tracker,
                            null,
                            Cache.NoAbsoluteExpiration,
                            new TimeSpan(Settings.Entities.UniquePageViewTrackingSlidingExpiryHours, 0, 0),
                            CacheItemPriority.Normal,
                            null);
                    }
                    object tR;
                    PageViewHandlerLocks.TryRemove(cacheIndex, out tR);
                }
            }
            return (UniquePageviewHandler)cache[cacheIndex];
        }
    }
