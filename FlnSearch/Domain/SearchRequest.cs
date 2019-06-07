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
        public int? Size { get; set; }
        public int? From { get; set; }
        public int? OrderNumber { get; set; }
        public DateTime? orderDate { get; set; }
        public int? ServiceType { get; set; }
        public string OrderStatus { get; set; }
        public int? CustomerNumber { get; set; }
        public string RecipientCompany { get; set; }
        public string RecipientName { get; set; }
        public string Clientmatter { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        //make attribute to be searched instead of this?
        private static readonly List<string> _searchableFields = new List<string> { 
            "OrderNumber",
            "orderDate",
            "ServiceType",
            "OrderStatus",
            "CustomerNumber",
            "RecipientCompany",
            "Clientmatter"
        };

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

        public string GenerateSearchQuery()
        {
            var qryProprties = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanRead && p.CanWrite && _searchableFields.Contains(p.Name)).ToArray();
            var isFirstMatch = true;
            StringBuilder qry = new StringBuilder();
            qry.Append("{");
            qry.AppendFormat("\"from\":{0},\"size\":{1},", From.GetValueOrDefault(0), Size.GetValueOrDefault(20));
            qry.Append("\"query\":");
            qry.Append("{");//query
            qry.Append("\"bool\":");
            qry.Append("{");//bool
            qry.Append("\"must\":");
            qry.Append("[");//must

            for (var i = 0; i < qryProprties.Count(); i++)
            {
                var property = qryProprties[i];
                var value = property.GetValue(this);
                var type = property.GetType();

                //if value not set then continue
                if (value == null)
                    continue;

                if (!isFirstMatch)
                    qry.Append(",");
                else
                    isFirstMatch = false;

                var matchString = type == typeof(int?)
                    ? "{{\"match\":{{\"{0}\":{1}}}}}"
                    : "{{\"match\":{{\"{0}\":\"{1}\"}}}}";

                qry.AppendFormat(matchString, property.Name, value);
            }
            qry.Append("]");//must

            //set filter on start/end date
            if (StartDate.HasValue || EndDate.HasValue)
            {
                qry.Append(",\"filter\":");
                qry.Append("{");//filter
                qry.Append("\"range\":");
                qry.Append("{");//range
                qry.AppendFormat("\"{0}\":", "OrderDateTicks");
                qry.Append("{");//orderticks

                if (StartDate.HasValue && EndDate.HasValue)
                    qry.AppendFormat("\"gte\":{0},\"lte\":{1}", StartDate.Value.Ticks, EndDate.Value.Ticks);
                else if(StartDate.HasValue)
                    qry.AppendFormat("\"gte\":{0}", StartDate.Value.Ticks);
                else
                    qry.AppendFormat("\"lte\":{0}",  EndDate.Value.Ticks);

                qry.Append("}");//orderticks
                qry.Append("}");//range
                qry.Append("}");//filter
            }
            qry.Append("}");//bool
            qry.Append("}");//query
            qry.Append("}");


            return qry.ToString();
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

            builder.Append("]");
            builder.Append("}");
            builder.Append("}");
            builder.Append("}");

            return builder.ToString();
        }
    }
}
