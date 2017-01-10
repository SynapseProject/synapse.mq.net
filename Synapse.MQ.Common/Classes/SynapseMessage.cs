using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using Synapse.MQ;

namespace Synapse.MQ
{
    public enum MessageType { NONE, EXECUTE, STATUS, PLANSTATUS_REQUEST, PLANSTATUS_REPLY, ACK, REQUEST, REPLY }

    [Serializable, XmlRoot("SynapseMessage")]
    public class SynapseMessage
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

        public string ToXml()
        {
            return XmlUtils.Serialize<SynapseMessage>(this, true);
        }

        public static SynapseMessage FromXml(String xml)
        {
            return XmlUtils.Deserialize<SynapseMessage>(xml);
        }

        public static SynapseMessage GetAck(SynapseMessage message)
        {
            SynapseMessage ackMessage = new SynapseMessage();
            ackMessage.Id = message.Id;
            ackMessage.Type = MessageType.ACK;
            ackMessage.TrackingId = message.TrackingId;
            return ackMessage;
        }
    }
}
