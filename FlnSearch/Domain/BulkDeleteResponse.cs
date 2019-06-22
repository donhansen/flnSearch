using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FlnSearch.Domain
{
    [Serializable]
    public class BulkDeleteResponse
    {
        

        public string RequestIdentifier { get; set; }

        [JsonProperty(PropertyName = "took")]
        public int Took { get; set; }
        [JsonProperty(PropertyName = "timed_out")]
        public bool TimedOut { get; set; }
        [JsonProperty(PropertyName = "total")]
        public int Total { get; set; }
        [JsonProperty(PropertyName = "deleted")]
        public int Deleted { get; set; }
        [JsonProperty(PropertyName = "batches")]
        public int Batches { get; set; }
        [JsonProperty(PropertyName = "version_conflicts")]
        public int VersionConflicts { get; set; }
        [JsonProperty(PropertyName = "noops")]
        public int Noops { get; set; }
        [JsonProperty(PropertyName = "throttled_millis")]
        public int ThrottledMilliseconds { get; set; }
        [JsonProperty(PropertyName = "requests_per_second")]
        public double RequestsPerSecond { get; set; }
        [JsonProperty(PropertyName = "throttled_until_millis")]
        public int throttledUntilMilliseconds { get; set; }
    }
}

/*
{  "took":8,
 * "timed_out":false,
 * "total":0,
 * "deleted":0,
 * "batches":0,
 * "version_conflicts":0,
 * "noops":0,"retries":{"bulk":0,"search":0},
 * "throttled_millis":0,
 * "requests_per_second":-1.0,
 * "throttled_until_millis":0,
 * "failures":[]}
*/