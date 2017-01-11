using System;
using System.Collections.Generic;
using System.Text;

namespace Synapse.MQ
{
    interface ISynapseNode
    {
        // Inbound Message Processing Functions
        // Function implementations should return a SynapseMessage if a reply is to be sent, or NULL if not.
        Func<SynapseMessage, SynapseMessage> ProcessExecutePlanRequest { get; set; }
        Func<SynapseMessage, SynapseMessage> ProcessPlanStatusReply { get; set; }
        Func<SynapseMessage, SynapseMessage> ProcessAcks { get; set; }

        // Outbound Actions
//        Guid SendStatusUpdateRequest(SynapseMessage message);
//        Guid SendPlanStatusRequest(SynapseMessage message);
        Guid SendMessage(SynapseMessage message);
    }
}
