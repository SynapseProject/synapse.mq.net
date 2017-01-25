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

        String Target { get; set; }
        String TargetGroup { get; set; }
        String SenderId { get; set; }
        bool AckRequested { get; set; }

        DateTime CreationDate { get; set; }
        DateTime SentDate { get; set; }
        DateTime ReceivedDate { get; set; }

        // Methods
        String Serialize();
        void Deserialize(String str);
        ISynapseMessage GetAck();
    }
}
