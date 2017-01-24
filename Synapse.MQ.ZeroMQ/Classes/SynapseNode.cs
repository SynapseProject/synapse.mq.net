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
        public String Id { get; set; }
        public String GroupId { get; set; }

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
            init();
            InboundUrl = inboundUrl;
            OutboundUrl = outboundUrl;
        }

        public SynapseNode(String id, String groupId, String[] inboundUrl, String[] outboundUrl)
        {
            init();
            InboundUrl = inboundUrl;
            OutboundUrl = outboundUrl;
            Id = id;
            GroupId = groupId;
        }

        private void init()
        {
            ProcessExecutePlanRequest = null;
            ProcessCancelPlanRequest = null;
            ProcessAcks = null;

            Id = Guid.NewGuid().ToString();
            GroupId = String.Empty;
        }

        public void Start()
        {
            Outbound = new SynapseEndpoint("Node-Outbound", OutboundUrl, ZSocketType.PUB);
            Outbound.Connect();

            Inbound = new SynapseEndpoint("Node-Inbound", InboundUrl, ZSocketType.SUB);
            Subscribe();
            Inbound.Connect();

            requestPoller = new Thread(() => Inbound.ReceiveMessages(ProcessInbound, Outbound));
            requestPoller.Start();

            Register();
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
            }

            return reply;
        }

        public Guid SendMessage(ISynapseMessage message)
        {
            Outbound.SendMessage(message);
            return message.Id;
        }

        public void Register()
        {
            SynapseMessage message = SynapseEndpoint.GetRegisterMessage(GroupId, Id, "REGISTER_NODE");
            this.SendMessage(message);
        }
        public void Unregister()
        {
            SynapseMessage message = SynapseEndpoint.GetRegisterMessage(GroupId, Id, "UNREGISTER_NODE");
            this.SendMessage(message);
        }

        private String[] GetQueueNames()
        {
            List<String> queues = new List<string>();

            queues.Add(Id + "." + GroupId + "." + "EXECUTEPLAN.SYNAPSE");   // Execute Plan Request
            queues.Add(GroupId + "." + "CANCELPLAN.SYNAPSE");               // Cancel Plan Request
            queues.Add(Id + "." + GroupId + "." + "STATUS.ACK.SYNAPSE");    // ACK messages from status update messages.
            queues.Add(Id + "." + GroupId + "." + "ADMIN.ACK.SYNAPSE");     // ACK messages from admin requests.

            return queues.ToArray();
        }

        public void Subscribe()
        {
            String[] queues = GetQueueNames();
            foreach (String queue in queues)
                Inbound.Subscribe(queue);
        }

        public void Unsubscribe()
        {
            String[] queues = GetQueueNames();
            foreach (String queue in queues)
                Inbound.Unsubscribe(queue);
        }
    }
}
