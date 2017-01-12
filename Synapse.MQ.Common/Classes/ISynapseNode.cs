using System;
using System.Collections.Generic;
using System.Text;

namespace Synapse.MQ
{
    public interface ISynapseNode
    {
        // Inbound Message Processing Functions
        // Function implementations should return a SynapseMessage if a reply is to be sent, or NULL if not.
        Func<ISynapseMessage, ISynapseEndpoint, ISynapseMessage> ProcessExecutePlanRequest { get; set; }
        Func<ISynapseMessage, ISynapseMessage> ProcessPlanStatusReply { get; set; }
        Func<ISynapseMessage, ISynapseMessage> ProcessAcks { get; set; }

        // Outbound Actions
//        Guid SendStatusUpdateRequest(SynapseMessage message);
//        Guid SendPlanStatusRequest(SynapseMessage message);
        Guid SendMessage(ISynapseMessage message);
    }
}
