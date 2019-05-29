using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlnSearch;
using FlnSearch.Domain;

namespace SearchTester
{
    class Program
    {
        static void Main(string[] args)
        {
            string readLineText = string.Empty;
            Console.WriteLine("11411657");
            while (readLineText.ToLower() != "x")
            {
                Console.WriteLine("Enter Max count");
                var count = Console.ReadLine();


                var request = new SearchRequest() { Size = Convert.ToInt32(count)};
                request.CustomerNumber = 31195;
                request.OrderStatus = "Entered";
                var result = Search(request);

                Console.WriteLine("__________________________________________________");

                Console.WriteLine("Enter 'x' to quit or enter to start a new search");
                readLineText = Console.ReadLine();


            }
        }

        public static object Search(SearchRequest request)
        {
            var search = new FlnSearch.AwsSearch();
            object results = search.DoSearch(request);

            return results;
        }

        public static async Task<string> RunSearch(string text)
        {
            var search = new FlnSearch.AwsSearch();
            string result = await search.RunSearchGet("orders", text, 10);
            return result;
        }
        public static async Task<string> PostSearch(string text)
        {
            var search = new FlnSearch.AwsSearch();
            string result = await search.RunSearchPost(text);//"orders", text, 10);
            return result;
        }
    }
}
