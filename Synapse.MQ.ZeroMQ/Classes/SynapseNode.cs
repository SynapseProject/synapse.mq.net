using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using ZeroMQ;

namespace Synapse.MQ.ZeroMQ
{
    public class SynapseNode : ISynapseNode
    {
        public Func<ISynapseMessage, ISynapseEndpoint, ISynapseMessage> ProcessExecutePlanRequest { get; set; }
        public Func<ISynapseMessage, ISynapseEndpoint, ISynapseMessage> ProcessCancelPlanRequest { get; set; }
        public Func<ISynapseMessage, ISynapseEndpoint, ISynapseMessage> ProcessAcks { get; set; }

        private String[] InboundUrl = { @"tcp://localhost:5556" };
        private String[] OutboundUrl = { @"tcp://localhost:5555" };

        private SynapseEndpoint Inbound = null;
        private SynapseEndpoint Outbound = null;

        private Thread requestPoller = null;

        public SynapseNode()
        {
            init();
        }

        public SynapseNode(String[] inboundUrl, String[] outboundUrl)
        {
            InboundUrl = inboundUrl;
            OutboundUrl = outboundUrl;

            init();
        }

        private void init()
        {
            ProcessExecutePlanRequest = null;
            ProcessCancelPlanRequest = null;
            ProcessAcks = null;
        }

        public void Start()
        {
            Outbound = new SynapseEndpoint("Node", OutboundUrl, ZSocketType.PUB);
            Outbound.Connect();

            Inbound = new SynapseEndpoint("Node", InboundUrl, ZSocketType.SUB);
            Inbound.Connect();

            requestPoller = new Thread(() => Inbound.ReceiveMessages(ProcessInbound, true, Outbound));
            requestPoller.Start();
        }


        private ISynapseMessage ProcessInbound(ISynapseMessage message, ISynapseEndpoint replyOn)
        {
            SynapseMessage reply = null;
            switch (message.Type)
            {
                case MessageType.EXECUTEPLAN:
                    if (ProcessExecutePlanRequest != null)
                        reply = (SynapseMessage)ProcessExecutePlanRequest(message, replyOn);
                    break;
                case MessageType.CANCELPLAN:
                    if (ProcessCancelPlanRequest != null)
                        reply = (SynapseMessage)ProcessCancelPlanRequest(message, replyOn);
                    break;
                case MessageType.ACK:
                    if (ProcessAcks != null)
                        reply = (SynapseMessage)ProcessAcks(message, replyOn);
                    break;
                default:
                    throw new Exception("Unknown MessageType [" + message.Type + "] Received.");
            }

            return reply;
        }

        public Guid SendMessage(ISynapseMessage message)
        {
            Outbound.SendMessage(message);
            return message.Id;
        }
    }
}
