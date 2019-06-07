using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlnSearch;
using FlnSearch.Domain;
using System.IO;

namespace SearchTester
{
    class Program
    {
        static void Main(string[] args)
        {
            //LoadIndex();

            string readLineText = string.Empty;
            Console.WriteLine("11411657");
            while (readLineText.ToLower() != "x")
            {
                Console.WriteLine("DELETE Index Name:");
                var indexName = Console.ReadLine();
                if (!string.IsNullOrEmpty(indexName))
                    DeleteIndex(indexName);

                Console.WriteLine("CREATE Index Name:");
                indexName = Console.ReadLine();
                if (!string.IsNullOrEmpty(indexName))
                    CreateIndex(indexName);

                Console.WriteLine("Enter Max count");
                var count = Console.ReadLine();


                var request = new SearchRequest() { Size = Convert.ToInt32(count) };
                //request.CustomerNumber = 31195;
                //request.OrderStatus = "Entered";
                request.ServiceType = 88;
                request.StartDate = new DateTime(2019, 5, 9);
                request.EndDate = new DateTime(2019, 6, 12);
                request.SortByField ="OrderDate";
                request.IsSortDESC = false;
                var result = Search(request);

                Console.WriteLine("__________________________________________________");

                Console.WriteLine("Enter 'x' to quit or enter to start a new search");
                readLineText = Console.ReadLine();


            }
        }

        public static void DeleteIndex(string name)
        {
            var search = new FlnSearch.AwsSearch();
            search.DeleteIndex(name);
        }

        public static void CreateIndex(string name)
        {
            var search = new FlnSearch.AwsSearch();
            search.GenerateIndex(name);
        }

        public static object Search(SearchRequest request)
        {
            var search = new FlnSearch.AwsSearch();
            object results = search.DoSearch(request);

            return results;
        }

        private static List<OrderRecord> GetAllOrders()
        {
            var allorders = new List<OrderRecord>();

            using (var reader = new StreamReader(@"C:\Users\Niels Hansen\Documents\Visual Studio 2012\Projects\FlnSearch\SearchTester\last30.rpt"))
            {
                bool firstLine = true;
                int lineNumber = 0;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split('\t');
                    lineNumber++;

                    //first row is the field names
                    if (firstLine)
                    {
                        firstLine = !firstLine;
                        continue;
                    }
                    var order = new OrderRecord();
                    order.RowNum = Convert.ToInt32(values[0]);
                    order.OrderNumber = Convert.ToInt32(values[1]);
                    order.OrderDate = Convert.ToDateTime(values[2]);
                    order.ServiceType = Convert.ToInt32(values[3]);
                    order.OrderStatusCode = values[4].Equals("NULL", StringComparison.OrdinalIgnoreCase) ? null : values[4];
                    order.OrderStatus = values[5].Equals("NULL", StringComparison.OrdinalIgnoreCase) ? null : values[5]; ;
                    order.CustomerNumber = Convert.ToInt32(values[6]);
                    order.BolNumber = values[7].Equals("NULL", StringComparison.OrdinalIgnoreCase) ? null : values[7].Replace("\"", "\\\"");
                    order.RecipientCompany = values[8].Equals("NULL", StringComparison.OrdinalIgnoreCase) ? null : values[8].Replace("\"", "\\\"");
                    order.RecipientName = values[9].Equals("NULL", StringComparison.OrdinalIgnoreCase) ? null : values[9].Replace("\"", "\\\"");
                    order.ClientMatter = values[10].Equals("NULL", StringComparison.OrdinalIgnoreCase) ? null : values[10].Replace("\"", "\\\"");
                    order.LastUpdateDate = Convert.ToDateTime(values[11]);

                    allorders.Add(order);
                }
            }

            return allorders;
        }

        private static void LoadIndex()
        {
            var orders = GetAllOrders();
            var search = new FlnSearch.AwsSearch();

            var remainingCount = orders.Count();
            var startIndex = 0;


            while (remainingCount > 0)
            {
                var count = remainingCount > 5000 ? 5000 : remainingCount;
                var batch = orders.GetRange(startIndex, count);

                search.BulkLoad(batch);

                remainingCount -= count;
                startIndex += count;
            }



            //var batch = orders.Where(o => o.RowNum >= 0 && o.RowNum < 5000).ToList();
            //search.BulkLoad(batch);
        }
    }
}
