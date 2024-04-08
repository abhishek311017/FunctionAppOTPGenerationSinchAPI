using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FunctionSinchapi
{
    public class SinchApi : EnvironmentConfiguration
    {
        private static readonly HttpClient httpClient = new HttpClient();

        private CloudStorage cloudStorage = null;

        private readonly string logFileName = string.Empty;

        public SinchApi()
        {
            this.GetEnvironmentVariables();
            cloudStorage = new CloudStorage();
            this.logFileName = this.salesLogsFolder + this.sinchLogsFolder + DateTime.UtcNow.ToString("hh:mm:ss.fff tt") + ".txt";
        }

        private static string GenerateVerificationCode()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        public async Task<JObject> Send(string phoneNumber)
        {
            var responseContent = string.Empty;
            var responseBody = new JObject();

            try
            {
                Console.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + ": Execution started");
                //need to take from keyvault
                string sinchAppKey = Environment.GetEnvironmentVariable("SinchAppKey"); //await KeyVault.GetKeyVaultSecret("SinchAppKey"); //
                string sinchAppSecret = await KeyVault.GetKeyVaultSecret("SinchAppSecret"); //Environment.GetEnvironmentVariable("SinchAppSecret"); // //

                //string sinchAppURL = Environment.GetEnvironmentVariable("SinchAppURL");

                string base64Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($@"{sinchAppKey}:{sinchAppSecret}"));

                string authCode = GenerateVerificationCode();

                this.logTracker.AppendLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + ": Verification code generated");

                var requestJson = JObject.Parse($@"
                                {{
                                    ""identity"": {{
                                        ""type"": ""number"",
                                        ""endpoint"": ""{phoneNumber}""
                                    }},
                                    ""method"": ""sms"",
                                    ""smsOptions"": {{
                                        ""expiry"": ""00:10:00"",
                                        ""codeType"": ""Numeric"",
                                        ""code"":""{authCode}"",
                                        ""template"": ""Your verification code is {{{{CODE}}}}""
                                 }}
                                }}
                ");

                var postData = new StringContent(requestJson.ToString(), Encoding.UTF8, "application/json");
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64Auth);

                logTracker.AppendLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + ": Sinch Api call started to send code");
                var response = await httpClient.PostAsync(this.sinchAppURL, postData);
                if (!response.IsSuccessStatusCode)
                {
                    logTracker.AppendLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + $": SinchVerification API request failed with status code: {response.StatusCode} and status message:{response.RequestMessage}");
                    throw new Exception();
                }
                else
                {
                    logTracker.AppendLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + $": Verification Code successfully sent to {phoneNumber}");
                    responseBody = new JObject
                                    {
                                        { "phoneNumber", phoneNumber },
                                        { "authCode", authCode }
                                    };
                }
            }
            catch (Exception ex)
            {
                logTracker.AppendLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + $":Error occured during the process of Verification" + ex);
                logTracker.AppendLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + $":Error Message {ex.Message}");
                logTracker.AppendLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + $":Stack Trace: {ex.StackTrace}");
                Console.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + " " + ex);
                throw;
            }
            finally
            {
                await this.cloudStorage.UploadFileToAzureBlob(this.logFileName, this.logTracker.ToString(), "application/json");
            }

            return responseBody;
        }
    }
}
