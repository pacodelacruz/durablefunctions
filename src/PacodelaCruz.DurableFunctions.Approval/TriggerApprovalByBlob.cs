using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Http;
using PacodelaCruz.DurableFunctions.Models;

namespace PacodelaCruz.DurableFunctions.Approval
{
    public static class TriggerApprovalByBlob
    {
        /// <summary>
        /// Function triggered by a Blob Storage file which starts a Durable Function Orchestration
        /// and sends the blob metadata as context
        /// </summary>
        /// <param name="requestBlob"></param>
        /// <param name="name"></param>
        /// <param name="orchestrationClient"></param>
        /// <param name="log"></param>
        [FunctionName("TriggerApprovalByBlob")]
        public static async void Run([BlobTrigger("requests/{name}", Connection = "Blob:StorageConnection")]Stream requestBlob, string name, [OrchestrationClient] DurableOrchestrationClient orchestrationClient, TraceWriter log)
        {
            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {requestBlob.Length} Bytes");
            string blobStorageBasePath = Environment.GetEnvironmentVariable("Blob:StorageBasePath", EnvironmentVariableTarget.Process);
            string requestor = "";
            string subject = "";
            // If the blob name containes a '+' sign, it identifies the first part of the blob name as the requestor and the remaining as the subject. Otherwise, the requestor is unknown and the subject is the full blobname. 
            if (name.Contains("+"))
            {
                requestor = Uri.UnescapeDataString(name.Substring(0, name.LastIndexOf("+")));
                subject = name.Substring(name.LastIndexOf("+") + 1);
            }
            else
            {
                requestor = "unknown";
                subject = name;
            }

            ApprovalRequestMetadata requestMetadata = new ApprovalRequestMetadata()
            {
                ApprovalType = "Cat",
                ReferenceUrl = $"{blobStorageBasePath}requests/{name}",
                Subject = subject,
                Requestor = requestor
            };

            string instanceId = await orchestrationClient.StartNewAsync("OrchestrateRequestApproval", requestMetadata);
            log.Info($"Durable Function Ochestration started: {instanceId}");
        }
    }
}
