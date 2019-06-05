using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlnSearch.Domain
{
    /// <summary>
    /// Record to be loaded to search
    /// </summary>
    public class OrderRecord
    {
        public int RowNum { get; set; }
        public long OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public int ServiceType { get; set; }
        public string OrderStatusCode { get; set; }
        public string OrderStatus { get; set; }
        public int CustomerNumber { get; set; }
        public string BolNumber { get; set; }
        public string RecipientCompany { get; set; }
        public string RecipientName { get; set; }
        public string ClientMatter { get; set; }
        public DateTime LastUpdateDate { get; set; }

        public int _id { get { return CustomerNumber; } }

    }
}
