using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using ZeroMQ;

namespace Synapse.MQ.ZeroMQ
{
    public class SynapseController : ISynapseController
    {
        private String OutboundUrl = @"tcp://localhost:5555";
        private String InboundUrl = @"tcp://localhost:5558";

        private SynapseEndpoint Outbound = null;
        private SynapseEndpoint Inbound = null;

        private Thread requestPoller = null;
        private Thread replyPoller = null;

        public SynapseController()
        {
            Outbound = new SynapseEndpoint(OutboundUrl);
            Outbound.Connect();

            Inbound = new SynapseEndpoint(InboundUrl);
            Inbound.Connect();

            requestPoller = new Thread(() => Inbound.ReceiveMessages(ProcessInbound));
            requestPoller.Start();
            replyPoller = new Thread(() => Inbound.ReceiveReplies(ProcessReplies));
            replyPoller.Start();
        }

        private SynapseMessage ProcessInbound(SynapseMessage message, SynapseEndpoint replyOn)
        {
            Console.WriteLine("*** SystemController : ProcessInbound Message Received");
            switch (message.Type)
            {
                case MessageType.PLANSTATUS_REQUEST:
                    ProcessPlanStatusRequest(message);
                    break;
                case MessageType.STATUS:
                    ProcessStatusUpdateRequest(message);
                    break;
                case MessageType.ACK:
                    ProcessAcks(message);
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
                default:
                    throw new Exception("Unknown MessageType [" + message.Type + "] Received.");
            }

            return null;
        }

        public void ProcessAcks(SynapseMessage message)
        {
            Console.WriteLine("*** SynapseController : ProcessAcks ***");
            Console.WriteLine("*** [" + message.Id + "][" + message.TrackingId + "][" + message.Type + "] " + message.Body);
        }

        public void ProcessPlanStatusRequest(SynapseMessage message)
        {
            Console.WriteLine("*** SynapseController : ProcessPlanStatusRequest ***");
            Console.WriteLine("*** [" + message.Id + "][" + message.TrackingId + "][" + message.Type + "] " + message.Body);
        }

        public void ProcessStatusUpdateRequest(SynapseMessage message)
        {
            Console.WriteLine("*** SynapseController : ProcessStatusUpdateRequest ***");
            Console.WriteLine("*** [" + message.Id + "][" + message.TrackingId + "][" + message.Type + "] " + message.Body);
        }

        public Guid SendExecutePlanRequest(SynapseMessage message)
        {
            Outbound.SendMessage(message);
            return message.Id;
        }
    }
}
