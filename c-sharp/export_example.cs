using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace WebAPIClient
{
    class Program
    {
        public static HttpClient client = new HttpClient();
        public static String api_token = "";
        public static String merchant_id = "";
        public static String export_type = "ORDERS"; // ORDERS or PAYMENTS
        public static long startTimeUTC = 1513065600000; //Dec 12, 2017 12:00 am
        public static long endTimeUTC = 1515657540000; // Jan 10, 2018 11:59 pm
        public static String export_endpoint = "https://apisandbox.dev.clover.com/v3/merchants/" + merchant_id + "/exports/";
        public static char[] separators = { ',', '{', '}', ':' };

        private static async Task BeginExport()
        {

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api_token);

            var json_request = new StringContent("{\"type\": \"" + export_type + "\", \"startTime\": " + startTimeUTC + ", \"endTime\": " + endTimeUTC +"}", Encoding.UTF8, "application/json");

            var response = await client.PostAsync(export_endpoint, json_request);
            var msg = await response.Content.ReadAsStringAsync();

            string[] elements = msg.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            if (msg.Contains("PENDING"))
            {
                Console.WriteLine("Request sent...");
                await WaitForLink(elements[1].Trim().Trim('"'));
            }
            else{
                Console.Write(msg);
            };

        }

        private static async Task WaitForLink(String export_id){

            var status_check = client.GetAsync(export_endpoint + export_id);
            var response = await status_check;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api_token);
            var msg = await response.Content.ReadAsStringAsync();

            string[] elements = msg.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            while (msg.Contains("PENDING") || msg.Contains("IN_PROGRESS")){
                elements = msg.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine("Status " + elements[5].Trim().Trim('"'));
                Thread.Sleep(5000);

                status_check = client.GetAsync(export_endpoint + export_id);
                response = await status_check;
                msg = await response.Content.ReadAsStringAsync();
            }

            char[] url_separator = {'[', ']'};
            elements = msg.Split(url_separator, StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine("Export URLS: "+ elements[1].Trim().Trim('"'));

        }

        static void Main(string[] args)
        {
            BeginExport().Wait();
        }
    }
}
