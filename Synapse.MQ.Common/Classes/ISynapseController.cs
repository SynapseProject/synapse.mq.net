using System;
using System.Collections.Generic;
using System.Text;

namespace Synapse.MQ
{
    interface ISynapseController
    {
        // Inbound Message Processing Functions
        // Function implementations should return a SynapseMessage if a reply is to be sent, or NULL if not.
        Func<SynapseMessage, SynapseMessage> ProcessPlanStatus { get; set; }
        Func<SynapseMessage, SynapseMessage> ProcessStatusUpdate { get; set; }
        Func<SynapseMessage, SynapseMessage> ProcessAcks { get; set; }

        // Outbound Actions
//        Guid SendExecutePlanRequest(SynapseMessage message);
        Guid SendMessage(SynapseMessage message);
    }
}
