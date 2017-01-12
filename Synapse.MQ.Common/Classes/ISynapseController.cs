using System;
using System.Collections.Generic;
using System.Text;

namespace Synapse.MQ
{
    public interface ISynapseController
    {
        // Inbound Message Processing Functions
        // Function implementations should return a SynapseMessage if a reply is to be sent, or NULL if not.
        Func<ISynapseMessage, ISynapseMessage> ProcessPlanStatus { get; set; }
        Func<ISynapseMessage, ISynapseMessage> ProcessStatusUpdate { get; set; }
        Func<ISynapseMessage, ISynapseMessage> ProcessAcks { get; set; }

        // Outbound Actions
//        Guid SendExecutePlanRequest(SynapseMessage message);
        Guid SendMessage(ISynapseMessage message);
    }
}
