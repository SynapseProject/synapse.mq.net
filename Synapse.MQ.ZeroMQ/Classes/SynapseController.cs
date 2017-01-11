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
        public Func<SynapseMessage, SynapseMessage> ProcessPlanStatus { get; set; }
        public Func<SynapseMessage, SynapseMessage> ProcessStatusUpdate { get; set; }
        public Func<SynapseMessage, SynapseMessage> ProcessAcks { get; set; }

        private String OutboundUrl = @"tcp://localhost:5555";
        private String InboundUrl = @"tcp://localhost:5558";

        private SynapseEndpoint Outbound = null;
        private SynapseEndpoint Inbound = null;

        private Thread requestPoller = null;
//        private Thread replyPoller = null;

        public SynapseController()
        {
            init();
        }

        public SynapseController(String inboundUrl, String outboundUrl)
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
//            replyPoller = new Thread(() => Inbound.ReceiveReplies(ProcessReplies, true, Outbound));
//            replyPoller.Start();
        }

        private SynapseMessage ProcessInbound(SynapseMessage message, SynapseEndpoint replyOn)
        {
            SynapseMessage reply = null;
            switch (message.Type)
            {
                case MessageType.PLANSTATUS_REQUEST:
                    if (this.ProcessPlanStatus != null)
                        reply = ProcessPlanStatus(message);
                    break;
                case MessageType.STATUS:
                    if (this.ProcessStatusUpdate != null)
                        reply = ProcessStatusUpdate(message);
                    break;
                case MessageType.ACK:
                    if (this.ProcessAcks != null)
                        reply = ProcessAcks(message);
                    break;
                default:
                    throw new Exception("Unknown MessageType [" + message.Type + "] Received.");
            }

            return reply;
        }

/*        public Guid SendExecutePlanRequest(SynapseMessage message)
        {
            Outbound.SendMessage(message);
            return message.Id;
        }
*/
        public Guid SendMessage(SynapseMessage message)
        {
            Outbound.SendMessage(message);
            return message.Id;
        }
    }
}
