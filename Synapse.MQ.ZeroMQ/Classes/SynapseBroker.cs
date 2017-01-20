using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Synapse.MQ;
using ZeroMQ;

namespace Synapse.MQ.ZeroMQ
{
    public enum BrokerType { NONE, BROADCAST, ROUNDROBIN, ORDERED, ADMIN }

    public class SynapseBroker
    {
        SynapseEndpoint Listener;
        SynapseEndpoint Sender;
        String[] InboundUrl;
        String[] OutboundUrl;

        public bool Debug { get; set; }

        public SynapseBroker(String[] inboundUrl, String[] outboundUrl, bool debug = false)
        {
            ZContext ctx = new ZContext();
            InboundUrl = inboundUrl;
            OutboundUrl = outboundUrl;

            Listener = new SynapseEndpoint("ProxyListener", InboundUrl, ZSocketType.SUB, ctx);
            Sender = new SynapseEndpoint("ProxySender", OutboundUrl, ZSocketType.PUB, ctx);
        }

        public void Start()
        {
            Listener.SubscribeAll();
            Listener.Bind();
            Sender.Bind();

            ZPollItem poll = ZPollItem.CreateReceiver();
            ZError error;
            ZMessage message;

            while (true)
            {
                if (Listener.Socket.PollIn(poll, out message, out error, TimeSpan.FromMilliseconds(64)))
                {
                    if (Debug)
                        ZeroMQUtils.WriteRawMessage(message);
                    ProcessMessage(message, Sender);
                }
                else
                {
                    if (error == ZError.ETERM)
                        return;
                    if (error != ZError.EAGAIN)
                        throw new ZException(error);
                }

                if (Sender.Socket.PollIn(poll, out message, out error, TimeSpan.FromMilliseconds(64)))
                {
                    Console.WriteLine("*** YOU SHOULD NEVER GET HERE ****");
                    if (Debug)
                        ZeroMQUtils.WriteRawMessage(message);
                    Console.WriteLine("*** YOU SHOULD NEVER GET HERE ****");
                    ProcessMessage(message, Listener);
                }
                else
                {
                    if (error == ZError.ETERM)
                        return;
                    if (error != ZError.EAGAIN)
                        throw new ZException(error);
                }
            }
        }

        private void ProcessMessage(ZMessage message, SynapseEndpoint endpoint)
        {
            if (message.Count >= 5)
            {
                BrokerType type = (BrokerType)Enum.Parse(typeof(BrokerType), message[0].ReadString());
                String groupName = message[1].ReadString();
                String queueName = message[2].ReadString();
                String sender = message[3].ReadString();
                String body = message[4].ReadString();      // TODO : Read As Bytes

                ZMessage outbound = new ZMessage();
                outbound.Add(new ZFrame(GetDestination(type, groupName, queueName)));
                outbound.Add(new ZFrame(sender));
                outbound.Add(new ZFrame(body));
                endpoint.Socket.Send(outbound);
            }
            else
            {
                Console.WriteLine("ERROR : Invalid Message Format Received.  Ignoring.");
            }
        }

        private String GetDestination(BrokerType type, String groupName, String queueName)
        {
            StringBuilder sb = new StringBuilder();

            if (type == BrokerType.ORDERED || type == BrokerType.ROUNDROBIN)
                sb.Append(Guid.NewGuid() + ".");
            sb.Append(queueName + ".");
            sb.Append(groupName + ".");
            sb.Append("Synapse");

            return sb.ToString();
        }
    }
}
