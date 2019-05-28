using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlnSearch.Domain
{
    public class SearchRequest
    {
        public int Size { get; set; }

        private List<SortItem> _sort;
        public List<SortItem> Sort { get; private set; }

        private Dictionary<string, object> _queryFields;
        public Dictionary<string, object> QueryFields { get { return _queryFields; } }

        public void AddSearchField(string fieldName, object value)
        {
            if (_queryFields == null)
                _queryFields = new Dictionary<string, object>();

            if (_queryFields.ContainsKey(fieldName))
                _queryFields[fieldName] = value;
            else
                _queryFields.Add(fieldName, value);
        }
        public void AddSortField(string fieldName, int sortOrder, bool isDesc)
        {
            if (_sort == null)
                _sort = new List<SortItem>();

            var item = _sort.FirstOrDefault(s => s.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            if (item == null)
                _sort.Add(new SortItem() { FieldName = fieldName, SortOrder = sortOrder, IsDesc = isDesc });
            else
            {
                item.SortOrder = sortOrder;
                item.IsDesc = isDesc;
            }
        }
    }
}
