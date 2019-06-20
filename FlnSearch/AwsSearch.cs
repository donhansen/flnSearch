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
using System.Reflection;
using System.Net.Http;
using NLog;
namespace FlnSearch
{
    public class AwsSearch
    {
        private static Logger logger = LogManager.GetLogger("AwsSearch");

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
            { "LastUpdateDate", "date"},
            };

        // private static readonly List<string> FieldNames = new List<string> { "CustomerNumber", "OrderNumber", "OrderDate" };

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

        
        private  Task<string> RemoveIndex(string indexName)
        {
            var response =  _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, string.Format("{0}/{1}", _baseUrl, indexName)));
            return response.Result.Content.ReadAsStringAsync();

        }

        private  Task<string> DeleteAsync(string url, string jsonText)
        {
            var content = new StringContent(jsonText, Encoding.UTF8, "application/json");
            var response =  _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, url) { Content = content });
            return response.Result.Content.ReadAsStringAsync();

        }

        private  Task<string> PutAsync(string url, string jsonText)
        {
            var content = new StringContent(jsonText, Encoding.UTF8, "application/json");
            var response =  _httpClient.PutAsync(url, content);
            //response.EnsureSuccessStatusCode();

            return response.Result.Content.ReadAsStringAsync();
        }

        //private async Task<string> PostAsync(string url, string jsonText)
        //{
        //    var content = new StringContent(jsonText, Encoding.UTF8, "application/json");
        //    var response = await _httpClient.PostAsync(url, content);
        //    response.EnsureSuccessStatusCode();

        //    var responseBody = await response.Content.ReadAsStringAsync();
        //    return responseBody;
        //}

        private Task< string> PostAsync(string url, string jsonText)
        {
            
            var content = new StringContent(jsonText, Encoding.UTF8, "application/json");
            var response =  _httpClient.PostAsync(url, content);

            return response.Result.Content.ReadAsStringAsync();


            //var responseBody =  response. Content.ReadAsStringAsync();
            //return responseBody;
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

        public IndexResponse GenerateIndex(string indexName)
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

            try
            {
                var result = PutAsync(url, content.ToString()).Result;
                var response = JsonConvert.DeserializeObject<IndexResponse>(result);
                return response;
            }
            catch (Exception ex)
            {
                return new IndexResponse() { Acknowledged = false, Index = indexName, Exception = ex };
            };


        }

        public IndexResponse DeleteIndex(string indexName)
        {
            try
            {
                var result = RemoveIndex(indexName).Result;
                var response = JsonConvert.DeserializeObject<IndexResponse>(result);
                return response;
            }
            catch (Exception ex)
            {
                return new IndexResponse() { Acknowledged = false, Index = indexName, Exception = ex };
            }

        }

        public BulkLoadResponse BulkLoad(List<OrderRecord> records)
        {
            var response = new BulkLoadResponse();
            var builder = new StringBuilder();

            foreach (var record in records)
            {
                builder.AppendFormat("{{\"index\":{{\"_index\":\"{0}\",\"_type\":\"order\",\"_id\":\"{1}\"}}}}", _index, record.OrderNumber);
                builder.Append(Environment.NewLine);
                builder.Append("{");
                builder.AppendFormat("\"OrderNumber\":{0}", record.OrderNumber);
                builder.AppendFormat(",\"OrderDate\":\"{0}\"", record.OrderDate.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                builder.AppendFormat(",\"OrderDateTicks\":{0}", record.OrderDate.Ticks);
                builder.AppendFormat(",\"ServiceType\":{0}", record.ServiceType);
                if (!string.IsNullOrEmpty(record.OrderStatusCode))
                {
                    builder.AppendFormat(",\"OrderStatusCode\":\"{0}\"", record.OrderStatusCode.Trim().Replace(@"\", @"\\").Replace("\"", "\\\""));
                    builder.AppendFormat(",\"OrderStatus\":\"{0}\"", record.OrderStatus.Trim().Replace(@"\", @"\\").Replace("\"", "\\\""));
                }
                builder.AppendFormat(",\"CustomerNumber\":{0}", record.CustomerNumber);
                if (!string.IsNullOrEmpty(record.BolNumber))
                    builder.AppendFormat(",\"BolNumber\":\"{0}\"", record.BolNumber.Trim().Replace(@"\", @"\\").Replace("\"", "\\\""));
                if (!string.IsNullOrEmpty(record.RecipientCompany))
                    builder.AppendFormat(",\"RecipientCompany\":\"{0}\"", record.RecipientCompany.Trim().Replace(@"\", @"\\").Replace("\"", "\\\""));
                if (!string.IsNullOrEmpty(record.RecipientName))
                    builder.AppendFormat(",\"RecipientName\":\"{0}\"", record.RecipientName.Trim().Replace(@"\", @"\\").Replace("\"", "\\\""));
                if (!string.IsNullOrEmpty(record.ClientMatter))
                    builder.AppendFormat(",\"ClientMatter\":\"{0}\"", record.ClientMatter.Trim().Replace(@"\", @"\\").Replace("\"", "\\\""));

                builder.AppendFormat(",\"LastUpdateDate\":\"{0}\"", record.LastUpdateDate.ToString("yyyy-MM-ddTHH:mm:ssZ"));

                builder.Append("}");
                builder.Append(Environment.NewLine);
            }

            var content = builder.ToString();

            var result = PostAsync(string.Format("{0}/_bulk", _baseUrl), content).Result;

            var root = JToken.Parse(result);
            response.Took = (int)root["took"];
            response.Errors = (bool)root["errors"];

            List<JToken> itemResults = root["items"].Children().ToList();

            response.RecordsInBatch = itemResults.Count;

            //only get the error items.  Getting all items has a big impact on performance
            if (response.Errors)
            {
                foreach (var token in itemResults)
                {
                    var tokenError = token["index"]["error"];
                    if (tokenError != null)
                    {
                        LoadItemResult item = token["index"].ToObject<LoadItemResult>();
                        var error = tokenError.ToObject<LoadError>();
                        error.SubType = tokenError["caused_by"]["type"].ToString();
                        error.Message = tokenError["caused_by"]["reason"].ToString();
                        item.Error = error;

                        response.FailedItems.Add(item);
                        logger.Debug(string.Format("Error:{0}::id:{1}\r\n\t{2}\r\n\t{3}\r\n------------------------------------",item.Error,item.Id, item.Error.Reason, item.Error.Message  ));
                    }
                }
            }
            return response;
        }

        public SearchResult DoSearch(QueryRequest request)
        {
            var searchresult = new SearchResult();
            var requestText = request.GenerateQuery();
            var url = string.Format("{0}/{1}/_search", _baseUrl, _index);
            var responseJson = PostAsync(url, requestText).Result;

            JObject awsSearch = JObject.Parse(responseJson);
            searchresult.Took = (int)awsSearch["took"];
            searchresult.TimedOut = (bool)awsSearch["timed_out"];
            searchresult.MatchCount = (int)awsSearch["hits"]["total"];

            List<JToken> hits = awsSearch["hits"]["hits"].Children().ToList();
            var properties = typeof(SearchResultItem).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite);

            foreach (var token in hits)
            {
                var resultItem = token.ToObject<SearchResultItem>();
                List<JToken> fields = token["_source"].Children().ToList();
                foreach (JToken s in fields)
                {
                    JValue jValue = ((JProperty)s).Value as JValue;
                    var name = ((JProperty)s).Name;
                    object value = jValue != null ? jValue.Value : null;
                    resultItem.AddSource(new SearchItem() { Name = name, Value = value });

                    //if has a value then set the property (if present)
                    if (value != null)
                    {
                        //set the result items property
                        var prop = properties.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                        if (prop != null)
                        {
                            Type propType = prop.PropertyType;
                            if (propType.IsGenericType && propType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                            {  //get underyling type
                                propType = Nullable.GetUnderlyingType(propType);
                            }
                            prop.SetValue(resultItem, Convert.ChangeType(value, propType), null);
                        }
                    }
                }
                searchresult.AddSearchResultItem(resultItem);
            }
            return searchresult;
        }

        public BulkDeleteResponse BulkDelete(QueryRequest request)
        {
            var url = string.Format("{0}/{1}/_delete_by_query", _baseUrl, _index);
            var requestText = request.GenerateQuery();
            var responseJson = PostAsync(url, requestText).Result;

            BulkDeleteResponse response = JsonConvert.DeserializeObject<BulkDeleteResponse>(responseJson);
            return response;
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
