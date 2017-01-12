using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Synapse.MQ.ZeroMQ
{
    public class SynapseNode : ISynapseNode
    {
        public Func<ISynapseMessage, ISynapseEndpoint, ISynapseMessage> ProcessExecutePlanRequest { get; set; }
        public Func<ISynapseMessage, ISynapseMessage> ProcessPlanStatusReply { get; set; }
        public Func<ISynapseMessage, ISynapseMessage> ProcessAcks { get; set; }

        private String InboundUrl = @"tcp://localhost:5556";
        private String OutboundUrl = @"tcp://localhost:5557";

        private SynapseEndpoint Outbound = null;
        private SynapseEndpoint Inbound = null;

        private Thread requestPoller = null;
//        private Thread replyPoller = null;

        public SynapseNode()
        {
            init();   
        }

        public SynapseNode(String inboundUrl, String outboundUrl)
        {
            InboundUrl = inboundUrl;
            OutboundUrl = outboundUrl;
            init();
        }

        private void init()
        {
            ProcessExecutePlanRequest = null;
            ProcessPlanStatusReply = null;
            ProcessAcks = null;

            Outbound = new SynapseEndpoint("Node", OutboundUrl);
            Outbound.Connect();

            Inbound = new SynapseEndpoint("Node", InboundUrl);
            Inbound.Connect();
            requestPoller = new Thread(() => Inbound.ReceiveMessages(ProcessInbound, true, Outbound));
            requestPoller.Start();
//            replyPoller = new Thread(() => Inbound.ReceiveReplies(ProcessReplies, true, Outbound));
//            replyPoller.Start();
        }


        private SynapseMessage ProcessInbound(SynapseMessage message, SynapseEndpoint replyOn)
        {
            SynapseMessage reply = null;
            switch (message.Type)
            {
                case MessageType.EXECUTEPLAN:
                    if (ProcessExecutePlanRequest != null)
                        reply = (SynapseMessage)ProcessExecutePlanRequest(message, replyOn);
                    break;
                case MessageType.ACK:
                    if (ProcessAcks != null)
                        reply = (SynapseMessage)ProcessAcks(message);
                    break;
                case MessageType.PLANSTATUS_REPLY:
                    if (ProcessPlanStatusReply != null)
                        reply = (SynapseMessage)ProcessPlanStatusReply(message);
                    break;
                default:
                    throw new Exception("Unknown MessageType [" + message.Type + "] Received.");
            }

            return reply;
        }

        /*
                private String ProcessReplies(SynapseMessage message)
                {
                    switch (message.Type)
                    {
                        case MessageType.PLANSTATUS_REPLY:
                            ProcessPlanStatusReply(message);
                            break;
                        default:
                            throw new Exception("Unknown MessageType [" + message.Type + "] Received.");
                    }

                    return null;
                }

                public Guid SendPlanStatusRequest(SynapseMessage message)
                {
                    Outbound.SendMessage(message);
                    return message.Id;
                }

                public Guid SendStatusUpdateRequest(SynapseMessage message)
                {
                    Outbound.SendMessage(message);
                    return message.Id;
                }
        */

        public Guid SendMessage(ISynapseMessage message)
        {
            Outbound.SendMessage(message);
            return message.Id;
        }
    }
}
