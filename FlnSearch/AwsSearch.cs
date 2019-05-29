using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FlnSearch.Domain;

namespace FlnSearch
{
    public class AwsSearch
    {
        private static readonly List<string> FieldNames = new List<string> { "CustomerNumber", "OrderNumber", "OrderDate" };

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

        private async Task<string> RunSearchPost(SearchRequest request)
        {
            var url = string.Format("{0}/orders/_search", _baseUrl);
            var jsonText = "{\"size\":20, \"query\":{\"query_string\":{\"default_field\":\"OrderStatus\",\"query\":\"Entered\"}}}";

            jsonText = request.GenerateJsonString();

            var content = new StringContent(jsonText, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        public async Task<string> RunSearchPost(string searchText)
        {
            var url = string.Format("{0}/orders/_search", _baseUrl);
            //            var jsonText = string.Format("{{\"size\":20, \"query\":{{\"query_string\":{{\"default_field\":\"OrderNumber\",\"query\":\"{0}\"}}}}}}", searchText);

            var jsonText = string.Format("{{\"size\":20, \"query\":{{\"query_string\":{{\"query\":\"{0}\"}}}}}}", searchText);


            var content = new StringContent(jsonText, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }


        public SearchResult DoSearch(SearchRequest request)
        {
            var searchresult = new SearchResult();
            var jsonText = RunSearchPost(request).Result;

            JObject awsSearch = JObject.Parse(jsonText);
            searchresult.Took = (int)awsSearch["took"];
            searchresult.TimedOut = (bool)awsSearch["timed_out"];
            searchresult.MatchCount = (int)awsSearch["hits"]["total"];

            List<JToken> hits = awsSearch["hits"]["hits"].Children().ToList();

            foreach (var token in hits)
            {
                var resultItem = token.ToObject<SearchResultItem>();
                List<JToken> fields = token["_source"].Children().ToList();
                foreach (JToken s in fields)
                {
                    var name = ((JProperty)s).Name;
                    var value = GetValue(((JProperty)s).Value);

                    if (FieldNames.Contains(name))
                        resultItem.SetPropertyValue(name, value);

                    resultItem.AddSource(new SearchItem() { Name = name, Value = value });
                }
                searchresult.AddSearchResultItem(resultItem);
            }
            return searchresult;
        }
        private static object GetValue(JToken value)
        {
            var type = value.Type;
            object returnValue = null;

            switch (type)
            {
                case JTokenType.Array:
                    break;
                case JTokenType.Boolean:
                    returnValue = (bool)value;
                    break;
                case JTokenType.Bytes:
                    break;
                case JTokenType.Comment:
                    break;
                case JTokenType.Constructor:
                    break;
                case JTokenType.Date:
                    returnValue = (DateTime)value;
                    break;
                case JTokenType.Float:
                    break;
                case JTokenType.Guid:
                    break;
                case JTokenType.Integer:
                    returnValue = (int)value;
                    break;
                case JTokenType.None:
                    break;
                case JTokenType.Null:
                    break;
                case JTokenType.Object:
                    break;
                case JTokenType.Property:
                    break;
                case JTokenType.Raw:
                    break;
                case JTokenType.String:
                    returnValue = (string)value;
                    break;
                case JTokenType.TimeSpan:
                    break;
                case JTokenType.Undefined:
                    break;
                case JTokenType.Uri:
                    break;
                default:
                    break;
            }
            return returnValue;
        }
    }
}
