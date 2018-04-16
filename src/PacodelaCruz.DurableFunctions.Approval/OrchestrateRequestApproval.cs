using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading;
using PacodelaCruz.DurableFunctions.Models;

namespace PacodelaCruz.DurableFunctions.Approval
{
    public static class RequestApprovalOrchestration
    {
        /// <summary>
        /// Durable Orchestration
        /// Orchestrates a Request Approval Process using the Durable Functions Human Interaction Pattern
        /// The Approval Request can be sent via Email using SendGrid or via Slack. 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        [FunctionName("OrchestrateRequestApproval")]
        public static async Task<bool> Run([OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var isApproved = false;
            string meansOfApproval = Environment.GetEnvironmentVariable("Workflow:MeansOfApproval");
            ApprovalRequestMetadata approvalRequestMetadata = context.GetInput<ApprovalRequestMetadata>();
            approvalRequestMetadata.InstanceId = context.InstanceId;

            // Check whether the approval request is to be sent via Email or Slack based on App Settings
            if (meansOfApproval.Equals("email", StringComparison.OrdinalIgnoreCase))
            {
                await context.CallActivityAsync("SendApprovalRequestViaEmail", approvalRequestMetadata);
            }
            else
            {
                await context.CallActivityAsync("SendApprovalRequestViaSlack", approvalRequestMetadata);
            }

            // Wait for Response as an external event or a time out. 
            // The approver has a limit to approve otherwise the request will be rejected.
            using (var timeoutCts = new CancellationTokenSource())
            {
                int timeout;
                if (!int.TryParse(Environment.GetEnvironmentVariable("Workflow:Timeout"), out timeout))
                    timeout = 5;
                DateTime expiration = context.CurrentUtcDateTime.AddMinutes(timeout);
                Task timeoutTask = context.CreateTimer(expiration, timeoutCts.Token);

                // This event can come from a click on the Email sent via SendGrid or a selection on the message sent via Slack. 
                Task<bool> approvalResponse = context.WaitForExternalEvent<bool>("ReceiveApprovalResponse");
                Task winner = await Task.WhenAny(approvalResponse, timeoutTask);
                ApprovalResponseMetadata approvalResponseMetadata = new ApprovalResponseMetadata()
                {
                    ReferenceUrl = approvalRequestMetadata.ReferenceUrl
                };

                if (winner == approvalResponse)
                {
                    if (approvalResponse.Result)
                    {
                        approvalResponseMetadata.DestinationContainer = "approved";
                    }
                    else
                    {
                        approvalResponseMetadata.DestinationContainer = "rejected";
                    }
                }
                else
                {
                    approvalResponseMetadata.DestinationContainer = "rejected";
                }

                if (!timeoutTask.IsCompleted)
                {
                    // All pending timers must be completed or cancelled before the function exits.
                    timeoutCts.Cancel();
                }

                // Once the approval process has been finished, the Blob is to be moved to the corresponding container.
                await context.CallActivityAsync<string>("MoveBlob", approvalResponseMetadata);
                return isApproved;
            }
        }
    }
}
