/* 
    Demonstrates basic usage of the Merchant Data Export API
    More information and additional examples can be found at https://github.com/clover/export-api-examples
    
    Script Usage
    ------------
    Update these configuration variables defined below:
    - HOST: The target API host
    - MERCHANT_ID: The Clover ID of the merchant whose data you want to export
    - ACCESS_TOKEN: A valid API token with access to the target merchant
    - EXPORT_TYPE: The type of data to export('ORDERS' or 'PAYMNENTS')
    - START_TIME: The start(lower-bound) of the export time window
    - END_TIME: The end(upper-bound) of the export time window
*/



using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;

namespace WebAPIClient
{
    class Program
    {

        // Begin Script Configuration
        public static String host = "https://apisandbox.dev.clover.com"; 
        public static String merchant_id = "";
        public static String api_token = "";
        public static String export_type = "ORDERS"; // ORDERS or PAYMENTS
        public static long startTimeUTC = 1513065600000; //Dec 12, 2017 12:00 AM UTC
        public static long endTimeUTC = 1515657540000; // Jan 10, 2018 11:59 PM UTC
        // End Script Configuration


        public static String export_endpoint = $"{host}/v3/merchants/{merchant_id}/exports/";
        public static HttpClient client = new HttpClient();

        private static async Task BeginExport()
        {
            string json_request = $@"{{""type"": ""{export_type}"", ""startTime"": {startTimeUTC}, ""endTime"": {endTimeUTC}}}";
            Console.WriteLine($"Export Endpoint: {export_endpoint}");
            Console.WriteLine("POSTing payload: ");
            Console.WriteLine(JObject.Parse(json_request));

            var response = await client.PostAsync(export_endpoint, new StringContent(json_request));
            var msg = await response.Content.ReadAsStringAsync();

            var current_status = JObject.Parse(msg)["status"];
            // The response will include the current status of the export. Our export job is in the queue and waiting to be processed.
            if (current_status != null && current_status.ToString() == "PENDING")
            {
                var export_id = JObject.Parse(msg)["id"].ToString();
                Console.WriteLine($"Export ID: {export_id}");
                Console.WriteLine("Request sent. Export pending.");
                // And now we...
                await WaitForLink(export_id);
            }
            else{
                Console.Write(msg);
            };
        }

        private static async Task WaitForLink(String export_id)
        {
            // Now we'll keep an eye on /v3/merchants/{merchant_id}/exports/{export_id} so we know when our export is ready
            var response = await client.GetAsync(export_endpoint + export_id);;
            var msg = await response.Content.ReadAsStringAsync();
            var current_status = JObject.Parse(msg)["status"].ToString();

            // PENDING: Process hasn't been started yet
            // IN_PROGRESS: Exporting is in progress
            while (current_status == "PENDING" || current_status == "IN_PROGRESS")
            {
                Console.WriteLine($"Status: {current_status}");
                // Wait for about 5 seconds before checking the endpoint again
                Thread.Sleep(5000);

                response = await client.GetAsync(export_endpoint + export_id);;
                msg = await response.Content.ReadAsStringAsync();
                current_status = JObject.Parse(msg)["status"].ToString();
            }

            // And we're done! A list of URLs to obtain your exported information.
            Console.WriteLine("=========Export URLs=========");
            Console.WriteLine(JObject.Parse(msg)["exportUrls"]["elements"].ToString());
        }

        static void Main(string[] args)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api_token);
            BeginExport().Wait();
        }
    }
}
