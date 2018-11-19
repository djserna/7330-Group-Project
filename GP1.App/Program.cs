using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using MySql.Data.MySqlClient;
using System.Configuration;

namespace GP1.App
{
    class Program
    {
        static string IEXTrading_API_PATH = "https://api.iextrading.com/1.0/stock/{symbol}/chart/{years}y";
        static string[] symbols = { "msft" };
        static void Main(string[] args)
        {
            DeleteExistingStockData();
            foreach (string symbol in symbols)
            {
                var historicalDataList = GetChart(symbol, 5);
                WriteStockDataToDatabase(historicalDataList, symbol);
            }
        }

        public static List<HistoricalDataResponse> GetChart(string symbol, int years)
        {
            RestClient client = new RestClient(IEXTrading_API_PATH);
            RestRequest request = new RestRequest(Method.GET);
            request.AddHeader("Accept", "application/json");
            request.AddUrlSegment("symbol", symbol);
            request.AddUrlSegment("years", years);

            var historicalDataList = new List<HistoricalDataResponse>();

            try
            {
                var response = client.Execute(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    historicalDataList = JsonConvert.DeserializeObject<List<HistoricalDataResponse>>(response.Content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling api. Message:{ex.Message}");
            }

            return historicalDataList;
        }

        private static void DeleteExistingStockData()
        {
            using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["StockData"].ToString()))
            {
                var command = new MySqlCommand("usp_DeleteStockData", connection);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting stock data from database. Message:{ex.Message}");
                }
            }
        }
        private static void WriteStockDataToDatabase(List<HistoricalDataResponse> stockDataList, string symbol)
        {
            Console.WriteLine($"Writing {stockDataList.Count} records to the database");
            int counter = stockDataList.Count;
            using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["StockData"].ToString()))
            {
                try
                {
                    connection.Open();
                    foreach (var stockData in stockDataList)
                    {
                        var command = new MySqlCommand("usp_InsertStockData", connection);
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Symbol", symbol);
                        command.Parameters.AddWithValue("@High", stockData.high);
                        command.Parameters.AddWithValue("@Low", stockData.low);
                        command.Parameters.AddWithValue("@QuoteDate", stockData.date);
                        command.ExecuteNonQuery();
                        counter--;
                        if (counter % 100 == 0)
                        {
                            Console.WriteLine($"{counter} records remaining.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting stock data from database. Message:{ex.Message}");
                }
            }
        }
    }
}
