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
            //LoadData();
            //LoadRange(@"C:\Users\Niels Hansen\Documents\Visual Studio 2012\Projects\FlnSearch\SearchTester\file3.rpt", 76084,1);

            string readLineText = string.Empty;
           
            while (readLineText.ToLower() != "x")
            {
                //Console.WriteLine("DELETE Index Name:");
                //var indexName = Console.ReadLine();
                //if (!string.IsNullOrEmpty(indexName))
                //    DeleteIndex(indexName);

                //Console.WriteLine("CREATE Index Name:");
                //indexName = Console.ReadLine();
                //if (!string.IsNullOrEmpty(indexName))
                //    CreateIndex(indexName);

                Console.WriteLine("Enter Max count");
                var requestCount = Console.ReadLine();
                if (string.IsNullOrEmpty(requestCount))
                    requestCount = "20";

                int count = Convert.ToInt32(requestCount);

                var request = new SearchRequest() { Size = count};
                request.CustomerNumber = 5914;
                //request.OrderNumber = 4012028;
                //request.OrderStatus = "Entered";
                //request.ServiceType = 88;
                request.StartDate = new DateTime(2019, 6, 1);
                request.EndDate = new DateTime(2019, 6, 12);
                request.AddSortField("OrderDate", 0, true);// SortByField = "OrderDate";
                request.AddSortField("OrderStatus", 1, false);
                var result = Search(request);

                Console.WriteLine(string.Format("{0} records found", result.MatchCount));
                if (result.MatchCount > 0)
                {
                    int total = count;
                    if(result.MatchCount < count)
                        total = result.MatchCount;

                    for (var i = 0; i < total; i++)
                    {
                        var item = result.ResultItems[i];
                        Console.WriteLine(string.Format("Customer Number:{0}; OrderDate:{1};Status:{2} LastUpdateDate:{3}; Score:{4}",item.CustomerNumber, item.OrderDate, item.OrderStatus, item.LastUpdateDate, item.Score  ));
                    }
                }

                Console.WriteLine("__________________________________________________");

                Console.WriteLine("Enter 'x' to quit or enter to start a new search");
                readLineText = Console.ReadLine();


            }
        }

        private static void LoadData()
        {
            Console.WriteLine("loading file...");
            var bulkResponse = LoadIndex(@"C:\Users\Niels Hansen\Documents\Visual Studio 2012\Projects\FlnSearch\SearchTester\file1.rpt");
            Console.WriteLine(string.Format("Processes file:{0} records processed. Total errors: {1}", bulkResponse.RecordsInBatch, bulkResponse.FailedItems.Count));
            Console.WriteLine("loading file...");
            bulkResponse = LoadIndex(@"C:\Users\Niels Hansen\Documents\Visual Studio 2012\Projects\FlnSearch\SearchTester\file2.rpt");
            Console.WriteLine(string.Format("Processes file:{0} records processed. Total errors: {1}", bulkResponse.RecordsInBatch, bulkResponse.FailedItems.Count));
            Console.WriteLine("loading file...");
            bulkResponse = LoadIndex(@"C:\Users\Niels Hansen\Documents\Visual Studio 2012\Projects\FlnSearch\SearchTester\file3.rpt");
            Console.WriteLine(string.Format("Processes file:{0} records processed. Total errors: {1}", bulkResponse.RecordsInBatch, bulkResponse.FailedItems.Count));
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

        public static SearchResult Search(SearchRequest request)
        {
            var search = new FlnSearch.AwsSearch();
            var results = search.DoSearch(request);

            return results;
        }

        private static List<OrderRecord> GetAllOrders(string fileName)
        {
            var allorders = new List<OrderRecord>();

            using (var reader = new StreamReader(fileName))
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

        private static void LoadRange(string fileName, int startIndex, int count)
        {
            var orders = GetAllOrders(fileName);
            var seaexh = new FlnSearch.AwsSearch();

            var batch = orders.GetRange(startIndex, count);
            seaexh.BulkLoad(batch);
        }

        private static BulkLoadResponse LoadIndex(string fileName)
        {
            var bulkResponse = new BulkLoadResponse();
            int recordsPerLoad = 5000;

            var orders = GetAllOrders(fileName);
            var search = new FlnSearch.AwsSearch();

            var remainingCount = orders.Count();
            var startIndex = 0;

            while (remainingCount > 0)
            {
                var count = remainingCount > recordsPerLoad ? recordsPerLoad : remainingCount;
                var batch = orders.GetRange(startIndex, count);

                Console.WriteLine(string.Format("\tprocessing records :{0} thru {1}...", startIndex, startIndex + count));
                var response = search.BulkLoad(batch);
                Console.WriteLine(string.Format("\t\tProcess took: {0}", response.Took));

                if (response.Errors)
                {
                    bulkResponse.Errors = true;
                    bulkResponse.FailedItems.AddRange(response.FailedItems);
                    bulkResponse.RecordsInBatch += response.RecordsInBatch;
                    Console.WriteLine(string.Format("\t{0} errors found", response.FailedItems.Count));
                }

                remainingCount -= count;
                startIndex += count;
            }
            return bulkResponse;
        }
    }
}
