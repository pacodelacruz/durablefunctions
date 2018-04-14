using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacodelaCruz.DurableFunctions.Models
{
    public class ApprovalResponseMetadata
    {
        public string ReferenceUrl { get; set; }
        public string DestinationContainer { get; set; }
    }
}
