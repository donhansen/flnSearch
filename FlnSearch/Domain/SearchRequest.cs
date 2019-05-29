using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace FlnSearch.Domain
{
    public class SearchRequest
    {
        public int Size { get; set; }
        public int OrderNumber { get; set; }
        public DateTime orderDate { get; set; }
        public int ServiceType { get; set; }
        public string OrderStatus { get; set; }
        public int CustomerNumber { get; set; }
        public string RecipientCompany { get; set; }
        public string RecipientName { get; set; }
        public string Clientmatter { get; set; }

        //make attribute to be searched instead of this?
        private static readonly List<string> _searchableFields = new List<string> { "OrderNumber", "orderDate", "ServiceType", "OrderStatus", "CustomerNumber", "RecipientCompany", "Clientmatter" };

        private List<SortItem> _sort;
        public List<SortItem> Sort { get; private set; }

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

        public string GenerateJsonString()
        {
            StringBuilder builder = new StringBuilder();
            StringBuilder matchBuilder = new StringBuilder();

            var qryProprties = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanRead && p.CanWrite && _searchableFields.Contains(p.Name)).ToArray();

            
            for (var i = 0; i < qryProprties.Count(); i++)
            {
                var property = qryProprties[i];
                var value = property.GetValue(this);
                if (property.PropertyType == typeof(string))
                {
                    if (value == null)
                        continue;

                    matchBuilder.AppendFormat("{{\"match\":{{\"{0}\":\"{1}\" }}}},", property.Name, value);
                }
                else if (property.PropertyType == typeof(DateTime))
                {
                    if ((DateTime)value == DateTime.MinValue)
                        continue;

                    matchBuilder.AppendFormat("{{\"match\":{{\"{0}\":\"{1}\" }}}},", property.Name, value);
                }
                else if (value is int && (int)value > 0)
                {
                    matchBuilder.AppendFormat("{{\"match\":{{\"{0}\":{1} }}}},", property.Name, value);
                }
            }


            builder.Append("{");
            if (Size > 0)
                builder.AppendFormat("\"size\":{0},", Size);

            builder.Append("\"query\":{\"bool\":{\"must\":[");

            builder.Append(matchBuilder.ToString().TrimEnd(','));

           // builder.AppendFormat("{{\"match\":{{\"CustomerNumber\":{0} }}}},", 31195);
           // builder.AppendFormat("{{\"match\":{{\"OrderStatus\":\"{0}\" }}}}", "Entered");
            builder.Append("]");
            builder.Append("}");
            builder.Append("}");
            builder.Append("}");

            return builder.ToString();
        }
    }
}
