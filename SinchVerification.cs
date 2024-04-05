using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FunctionSinchapi
{

    public static class SinchVerification
    {
        static string GenerateVerificationCode()
        {
            Random random = new Random();
            return random.Next(1000, 9999).ToString();
        }

        private static readonly HttpClient httpClient = new HttpClient();

        [FunctionName("SinchVerification")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("SinchVerification HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JObject.Parse(requestBody);

            string phoneNumber = data.phoneNumber;

            string SinchAppKey = Environment.GetEnvironmentVariable("SinchAppKey");
            string SinchAppSecret = Environment.GetEnvironmentVariable("SinchAppSecret");
            string base64Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($@"{SinchAppKey}:{SinchAppSecret}"));
            string authCode = GenerateVerificationCode();

            var requestJson = JObject.Parse($@"
                                {{
                                    ""identity"": {{
                                        ""type"": ""number"",
                                        ""endpoint"": ""{phoneNumber}""
                                    }},
                                    ""method"": ""sms"",
                                    ""smsOptions"": {{
                                        ""expiry"": ""00:01:00"",
                                        ""codeType"": ""Numeric"",
                                        ""code"":""{authCode}"",
                                        ""template"": ""this is abhishek.please find the {{{{CODE}}}}""
                                 }}
                                }}");

            var postData = new StringContent(requestJson.ToString(), Encoding.UTF8, "application/json");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64Auth);

            var response = await httpClient.PostAsync("https://verificationapi-v1.sinch.com/verification/v1/verifications", postData);

            if (!response.IsSuccessStatusCode)
            {
                log.LogError($"SinchVerification API request failed with status code {response.StatusCode}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            dynamic responseData = JObject.Parse(responseContent);
            log.LogError($"Response Content:{responseContent} ");

            var responseBody = new JObject
            {
                { "phoneNumber", phoneNumber },
                { "pinCode", authCode }
            };

            return new OkObjectResult(responseBody);
        }
    }
}
