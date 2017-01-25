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
        public String Id { get; set; }
        public String GroupId { get; set; }
        public bool Debug { get; set; }

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
            init();
            InboundUrl = inboundUrl;
            OutboundUrl = outboundUrl;
        }

        private void init()
        {
            ProcessStatusUpdate = null;
            ProcessAcks = null;
            Id = Guid.NewGuid().ToString();
            GroupId = String.Empty;

            Outbound = new SynapseEndpoint("Controller", OutboundUrl, ZSocketType.PUB);
            Outbound.Debug = Debug;
            Outbound.Connect();

            Inbound = new SynapseEndpoint("Controller", InboundUrl, ZSocketType.SUB);
            Inbound.Debug = Debug;
            Inbound.Connect();

            requestPoller = new Thread(() => Inbound.ReceiveMessages(ProcessInbound, Outbound));
            requestPoller.Start();

        }

        public void Start()
        {
            Subscribe();
            Register();
        }

        public void Stop()
        {
            Unregister();
            Unsubscribe();
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
            SynapseMessage message = SynapseEndpoint.GetRegisterMessage(GroupId, Id, "REGISTER_CONTROLLER");
            this.SendMessage(message);
        }

        public void Unregister()
        {
            SynapseMessage message = SynapseEndpoint.GetRegisterMessage(GroupId, Id, "UNREGISTER_CONTROLLER");
            this.SendMessage(message);
        }

        private String[] GetQueueNames()
        {
            List<String> queues = new List<string>();

            queues.Add(Id + "." + GroupId + "." + "STATUS.SYNAPSE");            // Status update messages.
            queues.Add(Id + "." + GroupId + "." + "EXECUTEPLAN.ACK.SYNAPSE");   // ACK messages from ExecutePlan requests.
            queues.Add(Id + "." + GroupId + "." + "ADMIN.ACK.SYNAPSE");         // ACK messages from admin requests.

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
