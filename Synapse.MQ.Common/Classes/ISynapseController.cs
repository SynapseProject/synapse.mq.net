using System;
using System.Collections.Generic;
using System.Text;

namespace Synapse.MQ
{
    public interface ISynapseController
    {
        // Controller Functions
        void Start();
        void Stop();

        // Inbound Message Processing Functions
        // Function implementations should return a SynapseMessage if a reply is to be sent, or NULL if not.
        Func<ISynapseMessage, ISynapseEndpoint, ISynapseMessage> ProcessStatusUpdate { get; set; }
        Func<ISynapseMessage, ISynapseEndpoint, ISynapseMessage> ProcessAcks { get; set; }

        // Outbound Actions
        Guid ExecutePlan(String body, String targetGroup = null, String trackingId = null, int seqNo = 0, bool requestAck = true, ISynapseEndpoint endpoint = null);
        Guid CancelPlan(String body, String targetGroup = null, String trackingId = null, int seqNo = 0, bool requestAck = false, ISynapseEndpoint endpoint = null);
    }
}
