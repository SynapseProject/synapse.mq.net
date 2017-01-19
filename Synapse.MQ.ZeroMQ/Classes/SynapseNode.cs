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
        private String[] OutboundUrl = { @"tcp://localhost:5557" };
        private String[] SubscribeUrl = { @"tcp://localhost:5560" }; 

        private SynapseEndpoint Inbound = null;
        private SynapseEndpoint Outbound = null;
        private SynapseEndpoint Subscriber = null;

        private Thread requestPoller = null;
        private Thread subscribePoller = null;

        public SynapseNode()
        {
            init();
        }

        public SynapseNode(String[] inboundUrl, String[] outboundUrl, String[] subscribeUrl)
        {
            InboundUrl = inboundUrl;
            OutboundUrl = outboundUrl;
            SubscribeUrl = subscribeUrl;

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
            Outbound = new SynapseEndpoint("Node", OutboundUrl);
            Outbound.Connect();

            Inbound = new SynapseEndpoint("Node", InboundUrl);
            Inbound.Connect();

            Subscriber = new SynapseEndpoint("Node", SubscribeUrl, ZSocketType.SUB);
            Subscriber.Connect();

            requestPoller = new Thread(() => Inbound.ReceiveMessages(ProcessInbound, true, Outbound));
            requestPoller.Start();

            subscribePoller = new Thread(() => Subscriber.SubscribeToMessages(ProcessCancelPlanRequest, false, Outbound));
            subscribePoller.Start();
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
