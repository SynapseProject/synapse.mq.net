using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using Synapse.MQ;

namespace Synapse.MQ.ZeroMQ
{
    [Serializable, XmlRoot("SynapseMessage")]
    public class SynapseMessage : ISynapseMessage
    {
        [XmlElement]
        public Guid Id { get; set; }
        [XmlElement]
        public String TrackingId { get; set; }
        [XmlElement]
        public int SequenceNumber { get; set; }
        [XmlElement]
        public MessageType Type { get; set; }
        [XmlElement]
        public String Body { get; set; }

        [XmlElement]
        public DateTime CreationDate { get; set; }
        [XmlElement]
        public DateTime SentDate { get; set; }
        [XmlElement]
        public DateTime ReceivedDate { get; set; }

        public SynapseMessage()
        {
            Id = Guid.NewGuid();
            CreationDate = DateTime.Now;
        }

        public static SynapseMessage GetInstance(String xml)
        {
            return XmlUtils.Deserialize<SynapseMessage>(xml);
        }

        public string Serialize()
        {
            return XmlUtils.Serialize<SynapseMessage>(this, false);
        }

        public ISynapseMessage Deserialize(String xml)
        {
            return GetInstance(xml);
        }

        public override string ToString()
        {
            return XmlUtils.Serialize<SynapseMessage>(this, true);
        }

        public ISynapseMessage GetAck()
        {
            SynapseMessage ackMessage = new SynapseMessage();
            ackMessage.Id = this.Id;
            ackMessage.Type = MessageType.ACK;
            ackMessage.TrackingId = this.TrackingId;
            return ackMessage;
        }
    }
}
