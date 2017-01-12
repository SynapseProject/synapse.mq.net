using System;
using System.Collections.Generic;
using System.Text;

namespace Synapse.MQ
{
    public interface ISynapseEndpoint
    {
        void SendMessage(ISynapseMessage message);
    }
}
