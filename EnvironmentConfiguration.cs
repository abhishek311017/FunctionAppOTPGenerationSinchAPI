using System;
using System.Text;

namespace FunctionSinchapi
{
    public class EnvironmentConfiguration
    {
        internal static string salesKeyVaultClientId;
        internal static string salesKeyVaultClientSecret;
        internal static string salesKeyVaultURL;
        internal static string salesResourceID;
        internal static string salesTenant;
        internal static string salesAADInstance;
        internal string storageAccountName;
        internal string salesBlobContainer;
        internal string exceptionMessage;
        internal string salesLogsFolder;
        internal string sinchLogsFolder;
        internal string sinchAppURL;
        internal StringBuilder logTracker;

        public EnvironmentConfiguration()
        {
            if (logTracker == null)
                logTracker = new StringBuilder();
        }

        public void GetEnvironmentVariables()
        {
            salesKeyVaultClientId = Environment.GetEnvironmentVariable("SalesKeyVaultClientId");
            salesKeyVaultClientSecret = Environment.GetEnvironmentVariable("SalesKeyVaultClientSecret");
            salesKeyVaultURL = Environment.GetEnvironmentVariable("SalesKeyVaultURL");
            salesResourceID = Environment.GetEnvironmentVariable("SalesResourceID");
            salesTenant = Environment.GetEnvironmentVariable("SalesTenant");
            salesAADInstance = Environment.GetEnvironmentVariable("SalesAADInstance");
            salesBlobContainer = Environment.GetEnvironmentVariable("SalesBlobContainer");
            storageAccountName = Environment.GetEnvironmentVariable("SalesStorageAccountName");
            salesLogsFolder = Environment.GetEnvironmentVariable("SalesLogsFolder");
            sinchLogsFolder = Environment.GetEnvironmentVariable("SinchLogsFolder");
            sinchAppURL = Environment.GetEnvironmentVariable("SinchAppURL");
        }
    }
}
