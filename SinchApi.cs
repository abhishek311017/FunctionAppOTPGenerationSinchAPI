using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FunctionSinchapi
{
    public class SinchApi : EnvironmentConfiguration
    {
        private HttpClient httpClient;

        private CloudStorage cloudStorage = null;

        private readonly string logFileName = string.Empty;

        public SinchApi()
        {
            httpClient = new HttpClient();
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

                ////Need to take below credentials from keyvault////
                string sinchAccessKey = Environment.GetEnvironmentVariable("SinchAccessKey");//await KeyVault.GetKeyVaultSecret("sinchAccessKey"); // 
                string sinchAccessSecret = Environment.GetEnvironmentVariable("SinchAccessSecret");//await KeyVault.GetKeyVaultSecret("sinchAccessSecret"); // // //
                string sinchProjectID = Environment.GetEnvironmentVariable("SinchProjectID");//await KeyVault.GetKeyVaultSecret("sinchProjectID"); // 
                string sinchAppID = Environment.GetEnvironmentVariable("SinchAppID"); //await KeyVault.GetKeyVaultSecret("sinchAppID");

                string base64Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($@"{sinchAccessKey}:{sinchAccessSecret}"));
                string authCode = GenerateVerificationCode();

                this.logTracker.AppendLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + ": Verification code generated");

                //var requestJson = JObject.Parse($@"
                //                {{
                //                    ""identity"": {{
                //                        ""type"": ""number"",
                //                        ""endpoint"": ""{phoneNumber}""
                //                    }},
                //                    ""method"": ""sms"",
                //                    ""smsOptions"": {{
                //                        ""expiry"": ""00:10:00"",
                //                        ""codeType"": ""Numeric"",
                //                        ""code"":""{authCode}"",
                //                        ""template"": ""Your verification code is {{{{CODE}}}}""
                //                 }}
                //                }}
                //");

                var requestJson = JObject.Parse($@"{{
                            ""app_id"": ""{sinchAppID}"",
                            ""recipient"": {{
                                ""identified_by"": {{
                                    ""channel_identities"": [
                                        {{
                                            ""channel"": ""SMS"",
                                            ""identity"": ""{phoneNumber}""
                                        }}
                                    ]
                                }}
                            }},
                            ""message"": {{
                                ""text_message"": {{
                                    ""text"": ""Your AGDCNow verification code is {authCode}""
                                }}
                            }},
                        }}");

                var sinchConversationAppURL = String.Format(this.sinchAppURL, sinchProjectID);
                var postData = new StringContent(requestJson.ToString(), Encoding.UTF8, "application/json");
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64Auth);

                logTracker.AppendLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + ": Sinch Api call started to send code");
                var response = await httpClient.PostAsync(sinchConversationAppURL, postData);

                if (!response.IsSuccessStatusCode)
                {
                    logTracker.AppendLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + $": SinchConversation API request failed with status code: {response.StatusCode} and status message:{response.RequestMessage}");
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
