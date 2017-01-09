using System;
using System.Collections.Generic;
using System.Text;

namespace Synapse.MQ
{
    interface ISynapseController
    {
        // Inbound Actions
        void ProcessAcks(SynapseMessage message);
        void ProcessStatusUpdateRequest(SynapseMessage message);
        void ProcessPlanStatusRequest(SynapseMessage message);

        // Outbound Actions
        Guid SendExecutePlanRequest(SynapseMessage message);

    }
}
