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

        private static readonly Dictionary<string, string> Mappings
            = new Dictionary<string, string> { 
            { "OrderNumber", "long" },
            { "OrderDate", "date" },
            { "OrderDateTicks", "long" },
            { "ServiceType", "integer" },
            { "OrderStatus", "text" },
            { "CustomerNumber", "text" },
            { "BolNumber", "text" },
            { "RecipientCompany", "text" },
            { "RecipientName", "text" },
            { "ClientMatter", "text" },  
            };

        private static readonly List<string> FieldNames = new List<string> { "CustomerNumber", "OrderNumber", "OrderDate" };

        private string _baseUrl;
        private string _index;

        private static readonly HttpClient _httpClient;

        static AwsSearch()
        {
            _httpClient = new HttpClient();
        }

        public AwsSearch()
        {
            _baseUrl = ConfigurationManager.AppSettings["searchUrl"];
            _index = ConfigurationManager.AppSettings["index"];
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
        }
        private async Task<string> RemoveIndex(string indexName)
        {
            var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, string.Format("{0}/{1}", _baseUrl, indexName)));
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        private async Task<string> PutAsync(string url, string jsonText)
        {
            var content = new StringContent(jsonText, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        private async Task<string> PostAsync(string url, string jsonText)
        {
            var content = new StringContent(jsonText, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        //private async Task<string> RunSearchPost(string searchText)
        //{
        //    var url = string.Format("{0}/orders/_search", _baseUrl);

        //    var jsonText = string.Format("{{\"size\":20, \"query\":{{\"query_string\":{{\"query\":\"{0}\"}}}}}}", searchText);
        //    var content = new StringContent(jsonText, Encoding.UTF8, "application/json");
        //    var response = await _httpClient.PostAsync(url, content);
        //    response.EnsureSuccessStatusCode();

        //    var responseBody = await response.Content.ReadAsStringAsync();
        //    return responseBody;
        //}

        public void GenerateIndex(string indexName)
        {
            var builder = new StringBuilder();
            builder.Append("{");

            //build the mappings 
            builder.Append("\"mappings\":");
            builder.Append("{");//mappings
            builder.Append("\"order\":");
            builder.Append("{");//order
            builder.Append("\"properties\":");
            builder.Append("{");//properties

            for (var i = 0; i < Mappings.Count; i++)
            {
                var item = Mappings.ElementAt(i);
                builder.AppendFormat(" \"{0}\": {{ \"type\":\"{1}\" }}", item.Key, item.Value);
                if (i < Mappings.Count - 1)
                    builder.Append(",");
            }
            builder.Append("}");//close on properties
            builder.Append("}");//close on order
            builder.Append("}");//close on mapping
            builder.Append("}");

            var url = string.Format("{0}/{1}", _baseUrl, indexName);
            var content = builder.ToString();

            var result = PutAsync(url, content.ToString()).Result;

        }

        public void DeleteIndex(string indexName)
        {
            var result = RemoveIndex(indexName).Result;
        }

        public void BulkLoad(List<OrderRecord> records)
        {
            var builder = new StringBuilder();

            foreach (var record in records)
            {
                builder.AppendFormat("{{\"index\":{{\"_index\":\"{0}\",\"_type\":\"order\",\"_id\":\"{1}\"}}}}", _index, record.OrderNumber);
                builder.Append(Environment.NewLine);
                builder.Append("{");
                builder.AppendFormat("\"OrderNumber\":{0}", record.OrderNumber);
                 builder.AppendFormat(",\"OrderDate\":\"{0}\"", record.OrderDate.Ticks);
                builder.AppendFormat(",\"OrderDateTicks\":{0}", record.OrderDate.Ticks);
                builder.AppendFormat(",\"ServiceType\":{0}", record.ServiceType);
                if (!string.IsNullOrEmpty(record.OrderStatusCode))
                {
                    builder.AppendFormat(",\"OrderStatusCode\":\"{0}\"", record.OrderStatusCode.Trim());
                    builder.AppendFormat(",\"OrderStatus\":\"{0}\"", record.OrderStatus.Trim());
                }
                builder.AppendFormat(",\"CustomerNumber\":{0}", record.CustomerNumber);
                if (!string.IsNullOrEmpty(record.BolNumber))
                    builder.AppendFormat(",\"BolNumber\":\"{0}\"", record.BolNumber.Trim());
                if (!string.IsNullOrEmpty(record.RecipientCompany))
                    builder.AppendFormat(",\"RecipientCompany\":\"{0}\"", record.RecipientCompany.Trim());
                if (!string.IsNullOrEmpty(record.RecipientName))
                    builder.AppendFormat(",\"RecipientName\":\"{0}\"", record.RecipientName.Trim());
                if (!string.IsNullOrEmpty(record.ClientMatter))
                    builder.AppendFormat(",\"ClientMatter\":\"{0}\"", record.ClientMatter.Trim());

                builder.Append("}");
                builder.Append(Environment.NewLine);
                //builder.AppendFormat("{{\"LastUpdateDate\": \"{0}\" }},",record.LastUpdateDate);
            }

            var content = builder.ToString();

            var result = PostAsync(string.Format("{0}/_bulk", _baseUrl), content).Result;
        }

        public SearchResult DoSearch(SearchRequest request)
        {
            var searchresult = new SearchResult();

            var url = string.Format("{0}/{1}/_search", _baseUrl, _index);
            var requestText = request.GenerateJsonString();
            var responseJson = PostAsync(url, requestText).Result;

            JObject awsSearch = JObject.Parse(responseJson);
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
                    object value = null;
                    if (FieldNames.Contains(name))
                    {
                         value = GetValue(((JProperty)s).Value);
                        resultItem.SetPropertyValue(name, value);
                    }
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
