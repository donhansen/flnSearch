using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net.Http;

namespace FlnSearch
{
    public class AwsSearch
    {
        private string _baseUrl;
        private static readonly HttpClient _httpClient;

        static AwsSearch()
        {
            _httpClient = new HttpClient();
        }

        public AwsSearch()
        {
            _baseUrl = ConfigurationManager.AppSettings["searchUrl"];


        }

        

        public async Task<string> RunSearchGet(string index, string searchText, int size)
        {
            string query = size > 0 
                ? string.Format("{0}&size={1}", searchText, size)
                : searchText;

            var url = string.Format("{0}/{1}/_search?q={2}", _baseUrl, index, query);

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException ex)
            {

                Console.WriteLine("\nException Caught");
                Console.WriteLine("Message :{0}", ex.Message);
                return ex.Message;
            }


            //    var result = await _httpClient.GetStringAsync(url);
            //    return result; 
        }

        private async Task<string> RunSearchPost(Domain.OrderSearch searchCriteria)
        {
            var url = string.Format("{0}/orders/_search", _baseUrl);
            var jsonText = "{\"size\":20, \"query\":{\"query_string\":{\"default_field\":\"OrderStatus\",\"query\":\"Entered\"}}}";

            var content = new StringContent(jsonText, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        public async Task<string> RunSearchPost(string searchText)
        {
            var url = string.Format("{0}/orders/_search", _baseUrl);
            var jsonText = string.Format("{{\"size\":20, \"query\":{{\"query_string\":{{\"default_field\":\"OrderNumber\",\"query\":\"{0}\"}}}}}}", searchText);

            

            var content = new StringContent(jsonText, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        //public string Search(string index, string queryText)
        //{
        //    var url = string.Format("{0}/{1}/_search/q={2}", _baseUrl, index, queryText);
        //    var result =_httpClient.ge
        //    return result;
        //}
    }
}
