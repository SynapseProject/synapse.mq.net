using System;
using System.Collections.Generic;
using System.Text;

namespace Synapse.MQ
{
    public interface ISynapseEndpoint
    {
        void SendMessage(ISynapseMessage message);
        void ReceiveMessages(Func<ISynapseMessage, ISynapseEndpoint, ISynapseMessage> callback, ISynapseEndpoint replyOn = null);
    }
}
