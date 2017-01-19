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
        public Func<ISynapseMessage, ISynapseEndpoint, ISynapseMessage> ProcessStatusUpdate { get; set; }
        public Func<ISynapseMessage, ISynapseEndpoint, ISynapseMessage> ProcessAcks { get; set; }

        private String[] InboundUrl = { @"tcp://localhost:5558" };
        private String[] OutboundUrl = { @"tcp://localhost:5555" };
        private String[] PublishUrl = { @"tcp://localhost:5559" };

        private SynapseEndpoint Inbound = null;
        private SynapseEndpoint Outbound = null;
        private SynapseEndpoint Publisher = null;

        private Thread requestPoller = null;

        public SynapseController()
        {
            init();
        }

        public SynapseController(String[] inboundUrl, String[] outboundUrl, String[] publishUrl)
        {
            InboundUrl = inboundUrl;
            OutboundUrl = outboundUrl;
            PublishUrl = publishUrl;

            init();
        }

        private void init()
        {
            ProcessStatusUpdate = null;
            ProcessAcks = null;
        }

        public void Start()
        {
            Outbound = new SynapseEndpoint("Controller", OutboundUrl);
            Outbound.Connect();

            Inbound = new SynapseEndpoint("Controller", InboundUrl);
            Inbound.Connect();

            Publisher = new SynapseEndpoint("Controller", PublishUrl, ZSocketType.PUB);
            Publisher.Connect();

            requestPoller = new Thread(() => Inbound.ReceiveMessages(ProcessInbound, true, Outbound));
            requestPoller.Start();
        }

        private ISynapseMessage ProcessInbound(ISynapseMessage message, ISynapseEndpoint replyOn)
        {
            SynapseMessage reply = null;
            switch (message.Type)
            {
                case MessageType.STATUS:
                    if (this.ProcessStatusUpdate != null)
                        reply = (SynapseMessage)ProcessStatusUpdate(message, replyOn);
                    break;
                case MessageType.ACK:
                    if (this.ProcessAcks != null)
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

        public Guid PublishMessage(ISynapseMessage message)
        {
//            Publish.PublishMessage(message);
            Publisher.SendMessage(message);
            return message.Id;
        }
    }
}
