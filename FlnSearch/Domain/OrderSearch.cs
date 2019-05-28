using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlnSearch.Domain
{
    public class OrderSearch
    {
        public int userId { get; set; }
        public string CustomerNo { get; set; }
        public string ClientMatter { get; set; }
        public string RecipientName { get; set; }
        public string RecipientCompany { get; set; }
        public string OrderNumber { get; set; }
        public string BOLNumber { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string ServiceType { get; set; }
        public string Status { get; set; }


    }
}
