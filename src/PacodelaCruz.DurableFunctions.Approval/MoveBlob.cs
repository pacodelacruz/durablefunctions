using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using PacodelaCruz.DurableFunctions.Models;

namespace PacodelaCruz.DurableFunctions.Approval
{
    public static class MoveBlob
    {
        /// <summary>
        /// Moves a Blob from one container to other based on metadata
        /// </summary>
        /// <param name="responseMetadata"></param>
        /// <param name="log"></param>
        [FunctionName("MoveBlob")]
        public static void Run([ActivityTrigger] ApprovalResponseMetadata responseMetadata, TraceWriter log)
        {
            log.Info($"Moving Blob {responseMetadata.ReferenceUrl} to {responseMetadata.DestinationContainer}");
            try
            {
                CloudStorageAccount account = CloudStorageAccount.Parse(System.Environment.GetEnvironmentVariable("Blob:StorageConnection", EnvironmentVariableTarget.Process));
                var client = account.CreateCloudBlobClient();
                var sourceBlob = client.GetBlobReferenceFromServerAsync(new Uri(responseMetadata.ReferenceUrl)).Result;
                var destinationContainer = client.GetContainerReference(responseMetadata.DestinationContainer);
                var destinationBlob = destinationContainer.GetBlobReference(sourceBlob.Name);
                destinationBlob.StartCopyAsync(sourceBlob.Uri);
                Task.Delay(TimeSpan.FromSeconds(15)).Wait();
                sourceBlob.DeleteAsync();
                log.Info($"Blob '{responseMetadata.ReferenceUrl}' moved to container '{responseMetadata.DestinationContainer}'");
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                throw;
            }
        }
    }
}
