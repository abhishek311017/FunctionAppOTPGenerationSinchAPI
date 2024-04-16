using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FunctionSinchapi
{

    public static class SinchVerification
    {
        [FunctionName("SinchVerification")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation(DateTime.Now.ToString("hh:mm:ss.fff tt") + ": SinchVerification HTTP trigger function processed a request");

            try
            {
                if ((req.Headers.TryGetValue("ApplicationID", out var applicationID) && applicationID == Environment.GetEnvironmentVariable("ApplicationID")) && req.Headers.TryGetValue("SaltKey", out var saltKey) && saltKey == Environment.GetEnvironmentVariable("SaltKeySinch"))
                {
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    dynamic data = JObject.Parse(requestBody);
                    string phoneNumber = data.phoneNumber;

                    var responseBodyObject = await new SinchApi().Send(phoneNumber);

                    return new OkObjectResult(responseBodyObject);
                }
                else
                {
                    throw new Exception(DateTime.Now.ToString("hh:mm:ss.fff tt") + ": Either ApplicationID or SaltKey is incorrect or empty");
                }
            }
            catch (Exception ex)
            {
                log.LogError(DateTime.Now.ToString("hh:mm:ss.fff tt") + $": Error in Sinch Veriffication: {ex.Message}");
                throw;
            }
        }
    }
}
