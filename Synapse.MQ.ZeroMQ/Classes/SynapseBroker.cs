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

    public class SynapseBroker
    {
        SynapseEndpoint Listener;
        SynapseEndpoint Sender;
        String[] InboundUrl;
        String[] OutboundUrl;

        public bool Debug { get; set; }

        private Dictionary<String, String> registeredControllers = new Dictionary<string, string>();
        private Dictionary<String, String> registeredNodes = new Dictionary<string, string>();

        public SynapseBroker(String[] inboundUrl, String[] outboundUrl, bool debug = false)
        {
            ZContext ctx = new ZContext();
            InboundUrl = inboundUrl;
            OutboundUrl = outboundUrl;
            Debug = debug;

            Listener = new SynapseEndpoint("BrokerListener", InboundUrl, ZSocketType.SUB, ctx);
            Sender = new SynapseEndpoint("BrokerSender", OutboundUrl, ZSocketType.PUB, ctx);
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
                    registeredControllers.Add(key, value);
                    Console.WriteLine("*** Controller [" + sMessage.SenderId + "] In Group [" + sMessage.TargetGroup + "] Has Been Registered.");
                }
                else if (sMessage.Target == "REGISTER_NODE")
                {
                    registeredNodes.Add(key, value);
                    Console.WriteLine("*** Node [" + sMessage.SenderId + "] In Group [" + sMessage.TargetGroup + "] Has Been Registered.");
                }
                else if (sMessage.Target == "UNREGISTER_CONTROLLER")
                {
                    registeredControllers.Remove(key);
                    Console.WriteLine("*** Controller [" + sMessage.SenderId + "] In Group [" + sMessage.TargetGroup + "] Has Been Unregistered.");
                }
                else if (sMessage.Target == "UNREGISTER_NODE")
                {
                    registeredNodes.Remove(key);
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
            {
                Console.WriteLine("<<< Sending Wire Message : " + DateTime.Now);
                ZeroMQUtils.WriteRawMessage(outbound);
            }

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
                        registeredNodes.TryGetValue(key, out sendTo);
                        break;
                    case "CANCELPLAN":
                        sendTo = message.TargetGroup;
                        break;
                    case "STATUS":
                        registeredControllers.TryGetValue(key, out sendTo);
                        break;
                    default:
                        sendTo = message.SenderId + "." + message.TargetGroup;
                        break;
                }
                sb.Append(sendTo + ".");
            }

            sb.Append(message.Target + ".");
            sb.Append("SYNAPSE");

            Console.WriteLine("+++++ Destination : " + sb.ToString() + " +++++");

            return sb.ToString();
        }
    }
}
