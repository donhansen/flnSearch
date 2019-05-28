using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlnSearch.Domain
{
    [Serializable]
    public class SortItem
    {
        /// <summary>
        /// Name of field to sort by
        /// </summary>
        public string FieldName { get; set; }
        /// <summary>
        /// Order the field belongs to for the sort
        /// </summary>
        public int SortOrder { get; set; }
        /// <summary>
        /// When true, the sort will be in desending order
        /// </summary>
        public bool IsDesc { get; set; }
    }
}
