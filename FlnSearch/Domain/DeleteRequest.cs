using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace FlnSearch.Domain
{
    public class DeleteRequest : QueryRequest
    {
        public DeleteRequest()
            : base()
        { }

        public override string GenerateQuery()
        {
            var qryProprties = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanRead && p.CanWrite && QueryFields.Contains(p.Name)).ToArray();
            var isFirstMatch = true;
            StringBuilder qry = new StringBuilder();
            qry.Append("{");
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
                qry.AppendFormat("\"{0}\":", "LastUpdateDateTicks");
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
            qry.Append("}");


            return qry.ToString();
        }

    }
}
