using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FlnSearch.Domain
{
    public class SearchResult
    {
        [JsonProperty(PropertyName = "took")]
        public int Took { get; set; }
        [JsonProperty(PropertyName = "timed_out")]
        public bool TimedOut { get; set; }

        public int MatchCount { get; set; }

        private List<SearchResultItem> _resultItems;
        public List<SearchResultItem> ResultItems
        {
            get
            {
                if (_resultItems == null)
                    _resultItems = new List<SearchResultItem>();
                
                return _resultItems;
            }
           // private set;
        }

        public void AddSearchResultItem(SearchResultItem resultItem)
        {
            if (_resultItems == null)
                _resultItems = new List<SearchResultItem>();

            var item = _resultItems.FirstOrDefault(s => s.Id.Equals(resultItem.Id));

            if (item != null)
                _resultItems.Remove(item);

            _resultItems.Add(resultItem);
        }
    }
}
