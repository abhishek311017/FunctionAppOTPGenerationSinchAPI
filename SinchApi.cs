using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FunctionSinchapi
{
    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }
    }

    public class SinchApi : EnvironmentConfiguration
    {
        // private HttpClient httpClient;

        private CloudStorage cloudStorage = null;

        private readonly string logFileName = string.Empty;

        private readonly string sinchAccessKey;
        private readonly string sinchAccessSecret;
        private readonly string sinchAuthURL;
        private readonly string sinchProjectID;
        private readonly string sinchAppID;
        private readonly string base64Auth;

        public SinchApi()
        {
            sinchAccessKey = Environment.GetEnvironmentVariable("SinchAccessKey");//await KeyVault.GetKeyVaultSecret("sinchAccessKey"); // 
            sinchAccessSecret = Environment.GetEnvironmentVariable("SinchAccessSecret");//await KeyVault.GetKeyVaultSecret("sinchAccessSecret"); //
            sinchAuthURL = Environment.GetEnvironmentVariable("SinchAuthURL");//await KeyVault.GetKeyVaultSecret("sinchProjectID"); // 
            sinchProjectID = Environment.GetEnvironmentVariable("SinchProjectID");
            sinchAppID = Environment.GetEnvironmentVariable("SinchAppID"); //await KeyVault.GetKeyVaultSecret("sinchAppID");
            base64Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{sinchAccessKey}:{sinchAccessSecret}"));
            this.GetEnvironmentVariables();
            cloudStorage = new CloudStorage();
            this.logFileName = this.salesLogsFolder + this.sinchLogsFolder + DateTime.UtcNow.ToString("hh:mm:ss.fff tt") + ".txt";
        }

        private async Task<string> GetAccessToken()
        {
            var requestData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            //string sinchAccessKey = Environment.GetEnvironmentVariable("SinchAccessKey");//await KeyVault.GetKeyVaultSecret("sinchAccessKey"); // 
            // string sinchAccessSecret = Environment.GetEnvironmentVariable("SinchAccessSecret");//await KeyVault.GetKeyVaultSecret("sinchAccessSecret"); //
            //string sinchAuthURL = Environment.GetEnvironmentVariable("SinchAuthURL");//await KeyVault.GetKeyVaultSecret("sinchProjectID");// 
            //string base64Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{sinchAccessKey}:{sinchAccessSecret}"));

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64Auth);
                var response = await httpClient.PostAsync(sinchAuthURL, requestData);
                var responseData = await response.Content.ReadAsStringAsync();

                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseData);

                string accessToken = tokenResponse.AccessToken;
                return accessToken;
            }
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
                //string sinchAccessKey = Environment.GetEnvironmentVariable("SinchAccessKey");//await KeyVault.GetKeyVaultSecret("sinchAccessKey"); // 
                //string sinchAccessSecret = Environment.GetEnvironmentVariable("SinchAccessSecret");//await KeyVault.GetKeyVaultSecret("sinchAccessSecret"); // // //
                //string sinchProjectID = Environment.GetEnvironmentVariable("SinchProjectID");//await KeyVault.GetKeyVaultSecret("sinchProjectID"); // 
                //string sinchAppID = Environment.GetEnvironmentVariable("SinchAppID"); //await KeyVault.GetKeyVaultSecret("sinchAppID");

                //string base64Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($@"{sinchAccessKey}:{sinchAccessSecret}"));
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
                var accessToken = await GetAccessToken();

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

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
