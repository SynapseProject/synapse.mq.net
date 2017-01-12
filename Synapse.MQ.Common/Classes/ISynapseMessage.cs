using System;
using System.Collections.Generic;
using System.Text;

namespace Synapse.MQ
{
    public interface ISynapseMessage
    {
        // Properties
        Guid Id { get; set; }
        String TrackingId { get; set; }
        int SequenceNumber { get; set; }
        MessageType Type { get; set; }
        String Body { get; set; }

        DateTime CreationDate { get; set; }
        DateTime SentDate { get; set; }
        DateTime ReceivedDate { get; set; }

        // Methods
        string Serialize();
        ISynapseMessage Deserialize(String str);
        ISynapseMessage GetAck();
    }
}
