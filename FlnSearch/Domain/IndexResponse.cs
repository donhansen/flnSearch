using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FlnSearch.Domain
{
    public class IndexResponse
    {
        [JsonProperty(PropertyName = "index")]
        public string Index { get; set; }
        [JsonProperty(PropertyName = "acknowledged")]
        public bool Acknowledged { get; set; }
        [JsonProperty(PropertyName = "shards_acknowledged")]
        public bool ShardsAcknowledged { get; set; }

        public bool IsError { get { return !Acknowledged; } }
        public Exception Exception { get; set; }
    }
}
