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
        public String Target { get; set; }
        [XmlElement]
        public String TargetGroup { get; set; }
        [XmlElement]
        public String SenderId { get; set; }
        [XmlElement]
        public bool AckRequested { get; set; }

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
            TargetGroup = String.Empty;
        }

        public static SynapseMessage GetInstance(String xml)
        {
            return XmlUtils.Deserialize<SynapseMessage>(xml);
        }

        public string Serialize()
        {
            return XmlUtils.Serialize<SynapseMessage>(this, false);
        }

        public void Deserialize(String xml)
        {
            SynapseMessage message =  GetInstance(xml);

            this.Body = message.Body;
            this.CreationDate = message.CreationDate;
            this.Id = message.Id;
            this.ReceivedDate = message.ReceivedDate;
            this.SentDate = message.SentDate;
            this.SequenceNumber = message.SequenceNumber;
            this.TrackingId = message.TrackingId;
            this.Type = message.Type;
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
            ackMessage.SequenceNumber = 1;
            ackMessage.SenderId = this.SenderId;
            ackMessage.Target = this.Target + ".ACK";
            ackMessage.TargetGroup = this.TargetGroup;
            return ackMessage;
        }

        public static SynapseMessage GetRegisterMessage(String groupId, String uniqueId, String queueName)
        {
            SynapseMessage message = new SynapseMessage();
            message.Type = MessageType.ADMIN;
            message.SenderId = uniqueId;
            message.Target = queueName;
            message.TargetGroup = groupId;
            message.AckRequested = true;

            return message;
        }

        public static SynapseMessage GetExecutePlanMessage(String body, String targetGroup = null, String trackingId = null, int seqNo = 0, bool requestAck = true)
        {
            SynapseMessage message = new SynapseMessage();

            message.Type = MessageType.EXECUTEPLAN;
            message.Target = MessageType.EXECUTEPLAN.ToString();
            if (targetGroup != null) { message.TargetGroup = targetGroup; }
            if (trackingId != null) { message.TrackingId = trackingId; }
            message.SequenceNumber = seqNo;
            message.Body = body;
            message.AckRequested = requestAck;

            return message;
        }

        public static SynapseMessage GetCancelPlanMessage(String body, String targetGroup = null, String trackingId = null, int seqNo = 0, bool requestAck = true)
        {
            SynapseMessage message = new SynapseMessage();

            message.Type = MessageType.CANCELPLAN;
            message.Target = MessageType.CANCELPLAN.ToString();
            if (targetGroup != null) { message.TargetGroup = targetGroup; }
            if (trackingId != null) { message.TrackingId = trackingId; }
            message.SequenceNumber = seqNo;
            message.Body = body;
            message.AckRequested = requestAck;

            return message;
        }

        public static SynapseMessage GetSendStatusMessage(String body, String targetGroup = null, String trackingId = null, int seqNo = 0, bool requestAck = true)
        {
            SynapseMessage message = new SynapseMessage();

            message.Type = MessageType.STATUS;
            message.Target = MessageType.STATUS.ToString();
            if (targetGroup != null) { message.TargetGroup = targetGroup; }
            if (trackingId != null) { message.TrackingId = trackingId; }
            message.SequenceNumber = seqNo;
            message.Body = body;
            message.AckRequested = requestAck;

            return message;
        }

    }
}
