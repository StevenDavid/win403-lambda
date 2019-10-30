using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

// PAckages I had to add to the function
using System.Diagnostics;
using System.Data.SqlClient;
using win403.Models;
using Newtonsoft.Json;
using System.Net.Http;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace win403lambda
{
    public class Functions
    {
        static int calls = 0; 
        static string qsk = Environment.GetEnvironmentVariable("qsk");
        static string encodedQuery = Environment.GetEnvironmentVariable("encodedQuery");
            
        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public Functions()
        {
        }


        /// <summary>
        /// A Lambda function to respond to HTTP Get methods from API Gateway
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The list of blogs</returns>
        public APIGatewayProxyResponse Get(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Console.WriteLine("Get Request");
            Console.WriteLine("Request=" + JsonConvert.SerializeObject(request));
            string qsp = "";
            string bodyResponse = "";
            DataConnectionViewModel TestOutput = new DataConnectionViewModel();
            Stopwatch LambdaStopwatch = new Stopwatch();
            
            if(request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey(qsk)) {
                qsp = request.QueryStringParameters[qsk];
            }
            
            Console.WriteLine("qsp=" +qsp);
            
            if(qsp == "webapi") {
                TestOutput = PoolManagerMSSQLQuery().Result;
                bodyResponse = JsonConvert.SerializeObject(TestOutput);
            } else {
                LambdaStopwatch.Start();
                TestOutput = DirectMSSQLQuery();
                LambdaStopwatch.Stop();
                
                TestOutput.LamElapsedTime = LambdaStopwatch.Elapsed.ToString();
                TestOutput.LamElapsedTimeMilli = (Convert.ToDecimal(LambdaStopwatch.Elapsed.Ticks)/10000).ToString();
                bodyResponse = JsonConvert.SerializeObject(TestOutput);
            }
            
            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = bodyResponse,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };

            return response;
        }
        
        
        public DataConnectionViewModel DirectMSSQLQuery()
        {
            byte[] decodedBytes = Convert.FromBase64String(encodedQuery);
            string query = System.Text.Encoding.UTF8.GetString(decodedBytes);
            DataConnectionViewModel ConResult = new DataConnectionViewModel(); 
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            string ConnStr = Helpers.GetMSSQLConnectionString();

            Console.WriteLine("ConnectionString=" + ConnStr);

            using (SqlConnection con = new SqlConnection(ConnStr)) {
                con.Open();

                Console.WriteLine("connection open");

                try {
                    var cmd = new SqlCommand(query, con);
                    var result = cmd.ExecuteScalar();
                    ConResult.IPAddress = result.ToString(); 
                    stopwatch.Stop();
                    ConResult.ElapsedTime = stopwatch.Elapsed.ToString();
                    ConResult.ElapsedTimeMilli = stopwatch.Elapsed.Milliseconds.ToString();

                    Console.WriteLine("Connection Succeeded! Time=" + ConResult.ElapsedTime);
                    
                    string output = JsonConvert.SerializeObject(ConResult);
                    Console.WriteLine("ConResult=" + output);
                    calls++; 
                    ConResult.Calls = calls;
                    ConResult.CallType = "Direct";
                    return ConResult;
                    
                }
                catch(System.Data.Common.DbException ex) {
                    Console.WriteLine("Connection failed, message=" + ex.Message);
                    return ConResult;
                }
            }

        }
        
        
        static async Task<DataConnectionViewModel> PoolManagerMSSQLQuery() {

            Console.WriteLine("Load Environment Variables");
            string baseUrl = Environment.GetEnvironmentVariable("baseUrl");
            
            DataConnectionViewModel ConResult = new DataConnectionViewModel();

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            using (HttpClient client = new HttpClient()) {       
                
                Console.WriteLine("calling url: " + baseUrl + encodedQuery);
                
                using (HttpResponseMessage res = await client.GetAsync(baseUrl + encodedQuery)) {
                    using (HttpContent content = res.Content) {
                        string data = await content.ReadAsStringAsync();
                        stopwatch.Stop();

                        if (data != null) {
                            Console.WriteLine("Call Suceeded:"+ data);
                            ConResult = JsonConvert.DeserializeObject<DataConnectionViewModel>(data);
                            ConResult.LamElapsedTime = stopwatch.Elapsed.ToString();
                            ConResult.LamElapsedTimeMilli = (Convert.ToDecimal(stopwatch.Elapsed.Ticks)/10000).ToString();
                            calls++; 
                            ConResult.Calls = calls;
                            ConResult.CallType = "Pool";
                        }
                        else {
                            Console.WriteLine("No data returned");
                        }
                    }
                }
            }
            Console.WriteLine("Return Results");
            return ConResult;
        }

        
    }
}
