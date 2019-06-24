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
using System.Data;
using System.Data.SqlClient;

namespace FlnSearch
{
    public class AwsSearch
    {
        private static string connectionString = ConfigurationManager.ConnectionStrings["FLN"].ToString();
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
            { "LastUpdateDateTicks", "long"},
            };

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

            logger.Debug(string.Format("AwsSearch: BaseUrl:'{0}'; Index:'{1}'", _baseUrl, _index));
        }

        private Task<string> RemoveIndex(string indexName)
        {
            try
            {
                var response = _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, string.Format("{0}/{1}", _baseUrl, indexName)));
                return response.Result.Content.ReadAsStringAsync();

            }
            catch (Exception ex)
            {
                logger.Error(string.Format("{0} Failed.\r\n:{1}", "RemoveIndex", ex.Message));
                throw;
            }
        }

        private Task<string> DeleteAsync(string url, string jsonText)
        {
            try
            {
                var content = new StringContent(jsonText, Encoding.UTF8, "application/json");
                var response = _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, url) { Content = content });
                return response.Result.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("{0} Failed.\r\n:{1}", "DeleteAsync", ex.Message));
                throw;
            }
        }

        private Task<string> PutAsync(string url, string jsonText)
        {
            try
            {
                var content = new StringContent(jsonText, Encoding.UTF8, "application/json");
                var response = _httpClient.PutAsync(url, content);
                return response.Result.Content.ReadAsStringAsync();

            }
            catch (Exception ex)
            {
                logger.Error(string.Format("{0} Failed.\r\n:{1}", "PutAsync", ex.Message));
                throw;
            }
        }
        //private async Task<string> PostAsync(string url, string jsonText)
        //{
        //    var content = new StringContent(jsonText, Encoding.UTF8, "application/json");
        //    var response = await _httpClient.PostAsync(url, content);
        //    response.EnsureSuccessStatusCode();

        //    var responseBody = await response.Content.ReadAsStringAsync();
        //    return responseBody;
        //}

        private Task<string> PostAsync(string url, string jsonText)
        {

            try
            {
                var content = new StringContent(jsonText, Encoding.UTF8, "application/json");
                var response = _httpClient.PostAsync(url, content);
                return response.Result.Content.ReadAsStringAsync();

            }
            catch (Exception ex)
            {
                logger.Error(string.Format("{0} Failed.\r\n:{1}", "PostSync", ex.Message));
                throw;
            }
        }

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
                logger.Error(string.Format("{0} Failed.\r\n:{1}", "GenerateIndex", ex.Message)); ;
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
                logger.Error(string.Format("{0} Failed.\r\n:{1}", "DeleteIndex", ex.Message));
                return new IndexResponse() { Acknowledged = false, Index = indexName, Exception = ex };
            }

        }

        public BulkLoadResponse LoadDataFromSource(DateTime startDate, DateTime enddate)
        {

            logger.Debug("+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+");
            logger.Debug(string.Format("LoadDataFromSource:: StartDate:{0} EndDate:{1}", startDate, enddate));

            var orders = GetDataFromSql(startDate, enddate);
            var bulkResponse = new BulkLoadResponse();
            int recordsPerLoad = 2500;

            var remainingCount = orders.Count();
            var startIndex = 0;

            while (remainingCount > 0)
            {
                var count = remainingCount > recordsPerLoad ? recordsPerLoad : remainingCount;
                var batch = orders.GetRange(startIndex, count);

                logger.Debug("+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+");
                logger.Debug(string.Format("processing records<start> :{0} thru {1}...", startIndex, startIndex + count));
                var response = BulkLoad(batch);
                bulkResponse.RecordsInBatch += response.RecordsInBatch;
                logger.Debug(string.Format("Process took: {0}", response.Took));

                if (response.Errors)
                {
                    bulkResponse.Errors = true;
                    bulkResponse.FailedItems.AddRange(response.FailedItems);
                    logger.Debug(string.Format("{0} errors found", response.FailedItems.Count));
                }
                logger.Debug(string.Format("processing records<end> :{0} thru {1}", startIndex, startIndex + count));


                remainingCount -= count;
                startIndex += count;
            }
            return bulkResponse;
        }

        private List<OrderRecord> GetDataFromSql(DateTime startDate, DateTime endDate)
        {
            var results = new List<OrderRecord>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("FLN.GetSearchRecords", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate;
                    cmd.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDate;

                    connection.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var record = new OrderRecord();
                            record.BolNumber = reader["BolNumber"] == DBNull.Value ? null : reader["BolNumber"].ToString();
                            record.ClientMatter = reader["ClientMatter"] == DBNull.Value ? null : reader["ClientMatter"].ToString();
                            record.CustomerNumber = reader["CustomerNumber"] == DBNull.Value ? 0 : Convert.ToInt32(reader["CustomerNumber"].ToString());
                            record.LastUpdateDate = reader["LastUpdateDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["LastUpdateDate"].ToString());
                            record.OrderDate = reader["OrderDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["OrderDate"].ToString());
                            record.OrderNumber = reader["OrderNumber"] == DBNull.Value ? 0 : Convert.ToInt64(reader["OrderNumber"].ToString());
                            record.OrderStatus = reader["OrderStatus"] == DBNull.Value ? null : reader["OrderStatus"].ToString();
                            record.OrderStatusCode = reader["OrderStatusCode"] == DBNull.Value ? null : reader["OrderStatusCode"].ToString();
                            record.RecipientCompany = reader["RecipientCompany"] == DBNull.Value ? null : reader["RecipientCompany"].ToString();
                            record.RecipientName = reader["RecipientName"] == DBNull.Value ? null : reader["RecipientName"].ToString();
                            record.RowNum = reader["RowNum"] == DBNull.Value ? 0 : Convert.ToInt32(reader["RowNum"].ToString());
                            record.ServiceType = reader["ServiceType"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ServiceType"].ToString());
                            results.Add(record);
                        }
                    }
                };
            }
            return results;
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
                builder.AppendFormat(",\"LastUpdateDateTicks\":{0}", record.LastUpdateDate.Ticks);


                builder.Append("}");
                builder.Append(Environment.NewLine);
            }

            JToken root = null;
            var content = builder.ToString();

            try
            {
                var result = PostAsync(string.Format("{0}/_bulk", _baseUrl), content).Result;
                root = JToken.Parse(result);
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("{0} Failed.\r\n:{1}", "BulkLoad", ex.Message));
                response.Errors = true;
                response.ResponseMessage = string.Format("{0} failed.\r\n{1}", "BulkLoad", ex.Message);
                return response;

            }
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
                        logger.Debug(string.Format("Error:{0}::id:{1}\r\n\t{2}\r\n\t{3}\r\n------------------------------------", item.Error, item.Id, item.Error.Reason, item.Error.Message));
                    }
                }
            }
            response.ResponseMessage = string.Format("BulkLoad::{0} Total items; {1} items failed", response.RecordsInBatch, response.FailedItems.Count);
            return response;
        }

        public SearchResult DoSearch(QueryRequest request)
        {
            var searchresult = new SearchResult(request.UniqueIdentifer);
            var requestText = request.GenerateQuery();
            var url = string.Format("{0}/{1}/_search", _baseUrl, _index);

            if (logger.IsDebugEnabled)
                logger.Debug(string.Format("Search Request Started - {0} -Time:{1} {2}", request.UniqueIdentifer, DateTime.Now.ToShortTimeString(), request.ToString()));

            string responseJson;
            try
            {
                responseJson = PostAsync(url, requestText).Result;
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("{0} -{1}- Failed. {2}", "DoSearch", request.UniqueIdentifer, ex.Message));
                throw;
            }
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
            if (logger.IsDebugEnabled)
                logger.Debug(string.Format("Search {0} Complete: Time: {1})", searchresult.UniqueIdentifier, DateTime.Now.ToShortTimeString()));

            
            
            return searchresult;
        }

        public BulkDeleteResponse BulkDelete(QueryRequest request)
        {
            var url = string.Format("{0}/{1}/_delete_by_query", _baseUrl, _index);
            var requestText = request.GenerateQuery();

            if (logger.IsDebugEnabled)
            {
                logger.Debug("+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+");
                logger.Debug(string.Format("Delete Request {0} Started:{1}Query:{2}", request.UniqueIdentifer, DateTime.Now.ToShortTimeString(), request.ToString()));
            }
            try
            {
                var responseJson = PostAsync(url, requestText).Result;
                BulkDeleteResponse response = JsonConvert.DeserializeObject<BulkDeleteResponse>(responseJson);
                response.RequestIdentifier = request.UniqueIdentifer;

                if (logger.IsDebugEnabled)
                {
                    logger.Debug(string.Format("Delete Request {0} completed: {1}; {2} Records deleted", request.UniqueIdentifer, DateTime.Now.ToShortTimeString(), response.Deleted));
                    logger.Debug("+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+");
                }
                return response;
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("{0} -{1}- Failed. {2}", "DoSearch", request.UniqueIdentifer, ex.Message));
                throw;
            }


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
