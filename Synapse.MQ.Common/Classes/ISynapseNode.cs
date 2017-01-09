using System;
using System.Collections.Generic;
using System.Text;

namespace Synapse.MQ
{
    interface ISynapseNode
    {
        // Inbound Actions
        void ProcessExecutePlanRequest(SynapseMessage message);
        void ProcessAcks(SynapseMessage message);
        void ProcessPlanStatusReply(SynapseMessage message);

        // Outbound Actions
        Guid SendStatusUpdateRequest(SynapseMessage message);
        Guid SendPlanStatusRequest(SynapseMessage message);
    }
}
