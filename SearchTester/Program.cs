using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchTester
{
    class Program
    {
        static void Main(string[] args)
        {
            string readLineText = string.Empty;

             while(readLineText.ToLower() != "x")
            {
                Console.WriteLine("11411657");
                Console.WriteLine("Enter search text or 'x' to close");
                readLineText = Console.ReadLine();

                if (readLineText.ToLower() != "x")
                {
                    var result = Search(readLineText);

                    Console.WriteLine("__________________________________________________");

                //    var task = RunSearch(readLineText).Result;
                //    Console.WriteLine(task);

                //    Console.WriteLine("__________________________________________________");

                //    var taskPost = PostSearch(readLineText).Result;
                //    Console.WriteLine(taskPost);
                }
            }
        }

        public static object Search(string searchText)
        {
            var search = new FlnSearch.AwsSearch();
            object results = search.DoSearch(searchText);

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
