// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace PacodelaCruz.DurableFunctions.Approval
{
    public static class ProcessSlackApprovals
    {
        /// <summary>
        /// Routes all Slack Interactive Button Responses to the corresponding handler
        /// I'm using AuthorizationLevel.Anonymous just for demostration purposes, but you most probably want to authenticate and authorise the call. 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("ProcessSlackApprovals")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, methods: "post", Route = "slackapproval")] HttpRequestMessage req, [OrchestrationClient] DurableOrchestrationClient orchestrationClient, TraceWriter log)
        {
            var formData = await req.Content.ReadAsFormDataAsync();
            string payload = formData.Get("payload");
            dynamic response = JsonConvert.DeserializeObject(payload);
            string callbackId = response.callback_id;
            string[] callbackIdParts = callbackId.Split('#');
            string approvalType = callbackIdParts[0];
            log.Info($"Received a Slack Response with callbackid {callbackId}");

            string instanceId = callbackIdParts[1];
            string from = Uri.UnescapeDataString(callbackIdParts[2]);
            string name = callbackIdParts[3];
            bool isApproved = false;
            log.Info($"instaceId:'{instanceId}', from:'{from}', name:'{name}', response:'{response.actions[0].value}'");
            var status = await orchestrationClient.GetStatusAsync(instanceId);
            log.Info($"Orchestration status: '{status}'");
            if (status.RuntimeStatus == OrchestrationRuntimeStatus.Running || status.RuntimeStatus == OrchestrationRuntimeStatus.Pending)
            {
                string selection = response.actions[0].value;
                string catEmoji = "";
                if (selection == "Approve")
                {
                    isApproved = true;
                    catEmoji = ":heart_eyes_cat:";
                }
                else
                {
                    catEmoji = ":smirk_cat:";
                }
                await orchestrationClient.RaiseEventAsync(instanceId, "ReceiveApprovalResponse", isApproved);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($"Thanks for your selection! Your selection for *'{name}'* was *'{selection}'* {catEmoji}") };
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($"The approval request has expired! :crying_cat_face:") };
            }
        }
    }
}