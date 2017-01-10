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
        private String InboundUrl = @"tcp://localhost:5556";
        private String OutboundUrl = @"tcp://localhost:5557";

        private SynapseEndpoint Outbound = null;
        private SynapseEndpoint Inbound = null;

        private Thread requestPoller = null;
        private Thread replyPoller = null;

        public SynapseNode()
        {
            Outbound = new SynapseEndpoint("Node", OutboundUrl);
            Outbound.Connect();

            Inbound = new SynapseEndpoint("Node", InboundUrl);
            Inbound.Connect();
            requestPoller = new Thread(() => Inbound.ReceiveMessages(ProcessInbound, true, Outbound));
            requestPoller.Start();
            replyPoller = new Thread(() => Inbound.ReceiveReplies(ProcessReplies, true, Outbound));
            replyPoller.Start();
            
        }


        private SynapseMessage ProcessInbound(SynapseMessage message, SynapseEndpoint replyOn)
        {
            switch (message.Type)
            {
                case MessageType.EXECUTE:
                    ProcessExecutePlanRequest(message);
                    break;
                case MessageType.ACK:
                    ProcessAcks(message);
                    break;
                case MessageType.PLANSTATUS_REPLY:
                    ProcessPlanStatusReply(message);
                    break;
                default:
                    throw new Exception("Unknown MessageType [" + message.Type + "] Received.");
            }

            return null;
        }

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

        public void ProcessExecutePlanRequest(SynapseMessage message)
        {
//            Console.WriteLine("*** SynapseNode : ProcessExecutePlanRequests ***");
//            Console.WriteLine(message);
//            Console.WriteLine("************************************************");
        }

        public void ProcessAcks(SynapseMessage message)
        {
//            Console.WriteLine("*** SynapseNode : ProcessAcks ***");
//            Console.WriteLine(message);
//            Console.WriteLine("************************************************");
        }

        public void ProcessPlanStatusReply(SynapseMessage message)
        {
//            Console.WriteLine("*** SynapseNode : ProcessPlanStatusReply ***");
//            Console.WriteLine(message);
//            Console.WriteLine("************************************************");
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
    }
}
