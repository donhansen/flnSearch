﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace FlnSearch.Domain
{
    public class SearchRequest : QueryRequest
    {
        public SearchRequest()
            : base()
        {

        }

        public override string GenerateQuery()
        {
            var qryProprties = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanRead && p.CanWrite && QueryFields.Contains(p.Name)).ToArray();
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
                var value = property.GetValue(this, null);
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
                else if (StartDate.HasValue)
                    qry.AppendFormat("\"gte\":{0}", StartDate.Value.Ticks);
                else
                    qry.AppendFormat("\"lte\":{0}", EndDate.Value.Ticks);

                qry.Append("}");//orderticks
                qry.Append("}");//range
                qry.Append("}");//filter
            }
            qry.Append("}");//bool
            qry.Append("}");//query

            //apply sorting
            if (Sort != null && Sort.Count > 0)
            {
                Sort.Sort();
                qry.Append(",\"sort\":[");
                for (var i = 0; i < Sort.Count; i++)
                {
                    if (i > 0)
                        qry.Append(",");

                    qry.AppendFormat("{{\"{0}\":{{\"order\":\"{1}\"}}}}", Sort[i].FieldName, Sort[i].IsDesc ? "desc" : "asc");
                    qry.Append(",\"_score\"]}");
                }
            }
            qry.Append("}");


            return qry.ToString();
        }

        private List<SortItem> _sort;
        public List<SortItem> Sort
        {
            get
            {
                if (_sort == null)
                    _sort = new List<SortItem>();

                return _sort;
            }
            //private set { }
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
