using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FlnSearch.Domain
{
    [Serializable]
    public class SearchResultItem
    {
        [JsonProperty(PropertyName = "_index")]
        public string Index { get; set; }
        [JsonProperty(PropertyName = "_type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "_id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "_score")]
        public decimal Score { get; set; }

        private List<SearchItem> _source;
        public List<SearchItem> Source
        {
            get
            {
                if (_source == null)
                    _source = new List<SearchItem>();

                return _source;

            }
        }

        public void AddSource(SearchItem sourceItem)
        {
            if (_source == null)
                _source = new List<SearchItem>();

            var item = _source.FirstOrDefault(s => s.Name.Equals(sourceItem.Name));

            if (item != null)
                _source.Remove(item);

            _source.Add(sourceItem);
        }
    }
}
