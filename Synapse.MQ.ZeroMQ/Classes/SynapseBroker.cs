using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using Synapse.MQ;
using ZeroMQ;

namespace Synapse.MQ.ZeroMQ
{
    public enum BrokerType { NONE, BROADCAST, ROUNDROBIN, ORDERED, ADMIN, REPLY }
    enum ClientType { CONTROLLER, NODE }

    public class SynapseBroker
    {
        SynapseEndpoint Listener;
        SynapseEndpoint Sender;
        String[] InboundUrl;
        String[] OutboundUrl;

        public bool Debug { get; set; }

        private Dictionary<String, Dictionary<String, List<String>>> clients = new Dictionary<string, Dictionary<String, List<String>>>();

        public SynapseBroker(String[] inboundUrl, String[] outboundUrl, bool debug = false)
        {
            ZContext ctx = new ZContext();
            InboundUrl = inboundUrl;
            OutboundUrl = outboundUrl;
            Debug = debug;

            clients.Add(ClientType.CONTROLLER.ToString(), new Dictionary<string, List<string>>());
            clients.Add(ClientType.NODE.ToString(), new Dictionary<string, List<string>>());

            Listener = new SynapseEndpoint("BrokerInbound", InboundUrl, ZSocketType.SUB, ctx);
            Sender = new SynapseEndpoint("BrokerOutbound", OutboundUrl, ZSocketType.PUB, ctx);
        }

        public void Start()
        {
            Console.WriteLine("Debug Mode : " + Debug);
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
                    ZMessage tempMessage = message;
                    if (Debug)
                        ZeroMQUtils.WriteRawMessage(message);
                    new Thread(() => ProcessMessage(tempMessage, Sender)).Start();
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
                    new Thread(() => ProcessMessage(message, Listener)).Start();
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
            String body = message[0].ReadString();
            SynapseMessage sMessage = SynapseMessage.GetInstance(body);

            if (sMessage.Type == MessageType.ADMIN)
            {
                String key = sMessage.TargetGroup;
                String value = sMessage.SenderId + "." + sMessage.TargetGroup;

                if (sMessage.Target == "REGISTER_CONTROLLER")
                {
                    AddClient(ClientType.CONTROLLER, sMessage.TargetGroup, sMessage.SenderId);
                    Console.WriteLine("*** Controller [" + sMessage.SenderId + "] In Group [" + sMessage.TargetGroup + "] Has Been Registered.");
                }
                else if (sMessage.Target == "REGISTER_NODE")
                {
                    AddClient(ClientType.NODE, sMessage.TargetGroup, sMessage.SenderId);
                    Console.WriteLine("*** Node [" + sMessage.SenderId + "] In Group [" + sMessage.TargetGroup + "] Has Been Registered.");
                }
                else if (sMessage.Target == "UNREGISTER_CONTROLLER")
                {
                    RemoveClient(ClientType.CONTROLLER, sMessage.TargetGroup, sMessage.SenderId);
                    Console.WriteLine("*** Controller [" + sMessage.SenderId + "] In Group [" + sMessage.TargetGroup + "] Has Been Unregistered.");
                }
                else if (sMessage.Target == "UNREGISTER_NODE")
                {
                    RemoveClient(ClientType.NODE, sMessage.TargetGroup, sMessage.SenderId);
                    Console.WriteLine("*** Node [" + sMessage.SenderId + "] In Group [" + sMessage.TargetGroup + "] Has Been Unregistered.");
                }

                if (sMessage.AckRequested == false)
                    return;

                sMessage = (SynapseMessage)sMessage.GetAck();
                sMessage.Target = "ADMIN.ACK";
            }

            ZMessage outbound = new ZMessage();
            outbound.Add(new ZFrame(GetDestination(sMessage)));
            outbound.Add(new ZFrame(sMessage.Serialize()));
            if (Debug == true)
                ZeroMQUtils.WriteRawMessage(outbound);

            endpoint.Socket.Send(outbound);

        }

        private String GetDestination(SynapseMessage message)
        {
            StringBuilder sb = new StringBuilder();

            if (message.Type == MessageType.ADMIN)
                sb.Append(message.SenderId + "." + message.TargetGroup);
            else
            {
                String key = message.TargetGroup;
                String sendTo = null;
                switch (message.Target)
                {
                    case "EXECUTEPLAN":
                    case "STATUS.ACK":
                        sendTo = GetClient(ClientType.NODE, BrokerType.ORDERED, message.TargetGroup);
                        break;
                    case "CANCELPLAN":
                        sendTo = message.TargetGroup;
                        break;
                    case "STATUS":
                    case "EXECUTEPLAN.ACK":
                        sendTo = GetClient(ClientType.CONTROLLER, BrokerType.ORDERED, message.TargetGroup);
                        break;
                    default:
                        sendTo = message.SenderId + "." + message.TargetGroup;
                        break;
                }
                sb.Append(sendTo + ".");
            }
            sb.Append(message.Target + ".");
            sb.Append("SYNAPSE");

            return sb.ToString();
        }

        private void AddClient(ClientType clientType, String groupId, String clientId)
        {
            Dictionary<String, List<String>> type = clients[clientType.ToString()];

            if (!type.ContainsKey(groupId))
                type.Add(groupId, new List<string>());

            if (!type[groupId].Contains(clientId))
                type[groupId].Add(clientId);
        }

        private void RemoveClient(ClientType clientType, String groupId, String clientId)
        {
            Dictionary<String, List<String>> type = clients[clientType.ToString()];

            if (type.ContainsKey(groupId))
                if (type[groupId].Contains(clientId))
                    type[groupId].Remove(clientId);
        }

        private List<String> GetClients(ClientType clientType, String groupId)
        {
            Dictionary<String, List<String>> type = clients[clientType.ToString()];

            if (type.ContainsKey(groupId))
                return type[groupId];
            else
                return null;
        }

        private String GetClient(ClientType clientType, BrokerType brokerType, String groupId)
        {
            String clientId = null;
            List<String> clients = GetClients(clientType, groupId);

            if (clients.Count > 0)
            {
                clientId = clients.ElementAt<String>(0) + "." + groupId;
            }

            return clientId;
        }
    }
}
