using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Threading.Tasks;

namespace FunctionSinchapi
{
    public class CloudStorage : EnvironmentConfiguration
    {
        /// <summary>
        /// azure storage account
        /// </summary>
        private readonly CloudStorageAccount storageAccount = null;

        /// <summary>
        /// azure container name
        /// </summary>
        private readonly CloudBlobContainer containerName = null;

        /// <summary>
        /// azure blob client
        /// </summary>
        private CloudBlockBlob blobClient = null;


        /// <summary>
        /// Cloud Storage
        /// </summary>
        public CloudStorage()
        {
            this.GetEnvironmentVariables();
            this.storageAccount = CloudStorageAccount.Parse(storageAccountName);
            this.containerName = storageAccount.CreateCloudBlobClient().GetContainerReference(salesBlobContainer);
        }

        /// <summary>
        /// Upload data to azure storage
        /// </summary>
        /// <param name="fileName">File Name </param>
        /// <param name="fileData">File Data</param>
        /// <param name="contentType">Content Type</param>
        /// <returns>returns nothing</returns>
        public async Task UploadFileToAzureBlob(string fileName, string fileData, string contentType)
        {
            try
            {
                blobClient = containerName.GetBlockBlobReference(fileName);
                blobClient.Properties.ContentType = contentType;
                await blobClient.UploadTextAsync(fileData);
            }
            catch (Exception ex)
            {
                exceptionMessage = fileName + ex.Message;
                logTracker.AppendLine(DateTime.Now.ToString("hh:mm:ss.fff tt") + ": Error in Uploading file to blob :" + exceptionMessage);
            }
        }
    }
}

