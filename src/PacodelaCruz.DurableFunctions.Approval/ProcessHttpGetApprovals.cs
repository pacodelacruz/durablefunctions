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

namespace PacodelaCruz.DurableFunctions.Standard
{
    public static class ProcessHttpGetApprovals
    {
        /// <summary>
        /// Process approval responses via an HTTP GET with query params
        /// I'm using AuthorizationLevel.Anonymous just for demostration purposes, but you most probably want to authenticate and authorise the call. 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("ProcessHttpGetApprovals")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, methods: "get", Route = "approval")] HttpRequestMessage req, [OrchestrationClient] DurableOrchestrationClient orchestrationClient, TraceWriter log)
        {
            log.Info($"Received an Approval Respose");
            string name = req.RequestUri.ParseQueryString().GetValues("name")[0];
            string instanceId = req.RequestUri.ParseQueryString().GetValues("instanceid")[0];
            string response = req.RequestUri.ParseQueryString().GetValues("response")[0];
            log.Info($"name: '{name}', instanceId: '{instanceId}', response: '{response}'");
            bool isApproved = false;
            var status = await orchestrationClient.GetStatusAsync(instanceId);
            log.Info($"Orchestration status: {status}");
            if (status != null && (status.RuntimeStatus == OrchestrationRuntimeStatus.Running || status.RuntimeStatus == OrchestrationRuntimeStatus.Pending))
            {
                if (response.ToLower() == "approved")
                    isApproved = true;
                await orchestrationClient.RaiseEventAsync(instanceId, "ReceiveApprovalResponse", isApproved);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("Thanks for your selection! :)") };
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Whoops! Something went wrong! :(") };
            }
        }
    }
}