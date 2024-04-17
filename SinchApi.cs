using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FunctionSinchapi
{
    public class SinchApi : EnvironmentConfiguration
    {
        #region Variables
        private CloudStorage cloudStorage = null;

        private readonly string logFileName = string.Empty;

        private string sinchServicePlanID;

        private string sinchAPIKey;

        private string sinchPhoneNumber;
        #endregion

        #region Constructor
        public SinchApi()
        {
            this.GetEnvironmentVariables();
            cloudStorage = new CloudStorage();
            this.logFileName = this.salesLogsFolder + this.sinchLogsFolder + "SinchLog_" + DateTime.UtcNow.ToString("hh:mm:ss.fff tt") + ".txt";
        }
        #endregion

        private static string GenerateVerificationCode()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        public async Task<JObject> Send(string phoneNumber)
        {
            var responseBody = new JObject();

            try
            {
                string salesEnv = Environment.GetEnvironmentVariable("SalesEnv");
                if (!string.IsNullOrEmpty(salesEnv) && salesEnv.ToLower() == "local")
                {
                    sinchPhoneNumber = Environment.GetEnvironmentVariable("SinchPhoneNumber");
                    sinchAPIKey = Environment.GetEnvironmentVariable("SinchAPIKey");
                    sinchServicePlanID = Environment.GetEnvironmentVariable("SinchServicePlanID");
                }
                else
                {
                    sinchPhoneNumber = await KeyVault.GetKeyVaultSecret("SinchPhoneNumber");
                    sinchAPIKey = await KeyVault.GetKeyVaultSecret("SinchAPIKey");
                    sinchServicePlanID = await KeyVault.GetKeyVaultSecret("SinchServicePlanID");
                }

                Console.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + ": Execution started");

                string authCode = GenerateVerificationCode();

                this.logTracker.AppendLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + ": Verification code generated");

                var requestJson = JObject.Parse($@"{{
                            ""from"": ""{sinchPhoneNumber}"",
                            ""to"": [
                                    ""{phoneNumber}""
                                    ],
                            ""body"": ""Your AGDCNow verification code is {authCode}"",
                            ""delivery_report"": ""none"",
                            ""type"": ""mt_text""
                        }}");

                var sinchConversationAppURL = String.Format(this.sinchAppURL, sinchServicePlanID);
                var postData = new StringContent(requestJson.ToString(), Encoding.UTF8, "application/json");

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", sinchAPIKey);

                    logTracker.AppendLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + ": Sinch Api call started to send code");
                    var response = await httpClient.PostAsync(sinchConversationAppURL, postData);

                    if (!response.IsSuccessStatusCode)
                    {
                        string detail = await response.Content.ReadAsStringAsync();
                        logTracker.AppendLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + $": SinchConversation API request failed with status code: {response.StatusCode} and status message:{response.RequestMessage}");
                        throw new Exception(detail);
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
            }
            catch (Exception ex)
            {
                logTracker.AppendLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + $":Error occured during the process of Verification");
                logTracker.AppendLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + $":Error Message {ex.Message}");
                logTracker.AppendLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + $":Stack Trace: {ex.StackTrace}");
                logTracker.AppendLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + $":Inner Exception Stack Trace: {ex.InnerException}");
                Console.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + " " + ex);
                throw;
            }
            finally
            {
                await this.cloudStorage.UploadFileToAzureBlob(this.logFileName, this.logTracker.ToString(), "text/plain");
            }

            return responseBody;
        }
    }
}
