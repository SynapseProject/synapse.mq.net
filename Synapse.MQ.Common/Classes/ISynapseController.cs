using System;
using System.Collections.Generic;
using System.Text;

namespace Synapse.MQ
{
    public interface ISynapseController
    {
        // Inbound Message Processing Functions
        // Function implementations should return a SynapseMessage if a reply is to be sent, or NULL if not.
        Func<ISynapseMessage, ISynapseEndpoint, ISynapseMessage> ProcessStatusUpdate { get; set; }
        Func<ISynapseMessage, ISynapseEndpoint, ISynapseMessage> ProcessAcks { get; set; }

        // Outbound Actions
        Guid SendMessage(ISynapseMessage message);
        Guid PublishMessage(ISynapseMessage message);
    }
}
