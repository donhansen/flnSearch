using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FlnSearch.Domain
{
    public class LoadError
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "reason")]
        public string Reason { get; set; }
        public string SubType { get; set; }
        public string Message { get; set; }
    }


    public class LoadItemResult
    {
        [JsonProperty(PropertyName = "_index")]
        public string Index { get; set; }
        [JsonProperty(PropertyName = "_type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "_id")]
        public int Id { get; set; }
        [JsonProperty(PropertyName = "_version")]
        public int Version { get; set; }
        [JsonProperty(PropertyName = "result")]
        public string Result { get; set; }
        [JsonProperty(PropertyName = "status")]
        public int Status { get; set; }
        public LoadError Error { get; set; }
    }

    [Serializable]
    public class BulkLoadResponse
    {
        [JsonProperty(PropertyName = "took")]
        public int Took { get; set; }
        [JsonProperty(PropertyName = "errors")]
        public bool Errors { get; set; }

        public int RecordsInBatch { get;set;}

        private List<LoadItemResult> _failedItems;
        public List<LoadItemResult> FailedItems
        {
            get
            {
                if (_failedItems == null)
                    _failedItems = new List<LoadItemResult>();

                return _failedItems;

            }
            set { _failedItems = value; }
        }

        //public List<LoadItemResult> ErrorItems
        //{
        //    get
        //    {
        //        return FailedItems.Where(r => r.Error != null).ToList();
        //    }
        //}
    }
}



