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
        public Func<ISynapseMessage, ISynapseMessage> ProcessPlanStatus { get; set; }
        public Func<ISynapseMessage, ISynapseMessage> ProcessStatusUpdate { get; set; }
        public Func<ISynapseMessage, ISynapseMessage> ProcessAcks { get; set; }

        private String[] InboundUrl = { @"tcp://localhost:5558" };
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
            ProcessPlanStatus = null;
            ProcessStatusUpdate = null;
            ProcessAcks = null;

            Outbound = new SynapseEndpoint("Controller", OutboundUrl);
            Outbound.Connect();

            Inbound = new SynapseEndpoint("Controller", InboundUrl);
            Inbound.Connect();

            requestPoller = new Thread(() => Inbound.ReceiveMessages(ProcessInbound, true, Outbound));
            requestPoller.Start();
        }

        private ISynapseMessage ProcessInbound(ISynapseMessage message, ISynapseEndpoint replyOn)
        {
            SynapseMessage reply = null;
            switch (message.Type)
            {
                case MessageType.PLANSTATUS_REQUEST:
                    if (this.ProcessPlanStatus != null)
                        reply = (SynapseMessage)ProcessPlanStatus(message);
                    break;
                case MessageType.STATUS:
                    if (this.ProcessStatusUpdate != null)
                        reply = (SynapseMessage)ProcessStatusUpdate(message);
                    break;
                case MessageType.ACK:
                    if (this.ProcessAcks != null)
                        reply = (SynapseMessage)ProcessAcks(message);
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
