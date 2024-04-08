//using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;


namespace FunctionSinchapi
{


    /// <summary>
    /// Key Vault information
    /// </summary>
    public class KeyVault : EnvironmentConfiguration
    {

        private static async Task<string> GetNewKeyVaultAccessToken()
        {
            Microsoft.Identity.Client.AuthenticationResult authResult = null;
            string authority = String.Format(CultureInfo.InvariantCulture, salesAADInstance, salesTenant);
            List<String> scopes = new List<String>() { salesResourceID + "/.default" };
            authResult = await Microsoft.Identity.Client.ConfidentialClientApplicationBuilder.Create(salesKeyVaultClientId)
                                                                            .WithClientSecret(salesKeyVaultClientSecret)
                                                                            .WithAuthority(authority)
                                                                            .Build().AcquireTokenForClient(scopes).ExecuteAsync();
            return authResult.AccessToken;
        }

        public static async Task<string> GetKeyVaultSecret(string Key)
        {
            string keyVaultSecret = string.Empty;

            try
            {
                string bearerToken = await GetKeyVaultAccessToken();
                // string bearerToken = await GetNewKeyVaultAccessToken();

                if (!string.IsNullOrEmpty(bearerToken))
                {
                    string URL = string.Format(salesKeyVaultURL, Key);
                    Uri uri = new Uri(salesKeyVaultURL);
                    HttpRequestMessage content = new HttpRequestMessage(HttpMethod.Get, uri);
                    bearerToken = bearerToken.Remove(0, 7);
                    content.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                    using (var client = new HttpClient())
                    {
                        HttpResponseMessage resp = await client.SendAsync(content);

                        if (resp.StatusCode == HttpStatusCode.OK)
                        {
                            string response = await resp.Content.ReadAsStringAsync();

                            if (response.Length > 0)
                            {
                                JObject obj = JObject.Parse(response);

                                keyVaultSecret = Convert.ToString(obj["value"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return keyVaultSecret;
        }

        private static async Task<string> GetKeyVaultAccessToken()
        {
            try
            {
                string authority = String.Format(CultureInfo.InvariantCulture, salesAADInstance, salesTenant);
                ClientCredential clientCredential = new ClientCredential(salesKeyVaultClientId, salesKeyVaultClientSecret);

                AzureToken token = await GetBearerToken(authority, clientCredential, salesResourceID);
                return token.bearerToken;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static async Task<AzureToken> GetBearerToken(string authority, ClientCredential credentials, string resource)
        {
            AuthenticationResult result = null;
            int retryCount = 0;
            bool retry = false;

            AuthenticationContext context = new AuthenticationContext(authority, null);
            do
            {
                retry = false;
                try
                {
                    result = await context.AcquireTokenAsync(resource, credentials);
                }
                catch (AdalException ex)
                {
                    if (ex.ErrorCode == "temporarily_unavailable")
                    {
                        retry = true;
                        retryCount++;
                        await Task.Delay(3000);
                    }

                    if (retryCount == 3)
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    retry = true;
                    retryCount++;

                    await Task.Delay(3000);

                    if (retryCount == 3)
                    {
                        throw;
                    }
                }
            } while ((retry == true) && (retryCount < 3));

            if (result == null)
            {
                throw new AdalException("AzureServiceErr", "KeyVault access token acquiring failed");
            }

            return new AzureToken { bearerToken = result.AccessTokenType + " " + result.AccessToken, expiry = result.ExpiresOn };
        }

        private class AzureToken
        {
            public string bearerToken;
            public DateTimeOffset expiry;
        }
    }
}

