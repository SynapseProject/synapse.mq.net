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

        private String[] InboundUrl = { @"tcp://localhost:5556" };
        private String[] OutboundUrl = { @"tcp://localhost:5555" };

        private SynapseEndpoint Inbound = null;
        private SynapseEndpoint Outbound = null;

        private Thread requestPoller = null;

        public SynapseController()
        {
            init();
        }

        public SynapseController(String[] inboundUrl, String[] outboundUrl)
        {
            InboundUrl = inboundUrl;
            OutboundUrl = outboundUrl;

            init();
        }

        private void init()
        {
            ProcessStatusUpdate = null;
            ProcessAcks = null;
        }

        public void Start()
        {
            Outbound = new SynapseEndpoint("Controller", OutboundUrl, ZSocketType.PUB);
            Outbound.Connect();

            Inbound = new SynapseEndpoint("Controller", InboundUrl, ZSocketType.SUB);
            Inbound.Connect();

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
    }
}
