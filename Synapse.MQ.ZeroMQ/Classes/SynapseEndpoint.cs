using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;

using ZeroMQ;

namespace Synapse.MQ.ZeroMQ
{
    public class SynapseEndpoint : ISynapseEndpoint
    {
        public String Name { get; set; }
        public ZContext Context { get; }
        public ZSocket Socket { get; }
        public ZSocketType SocketType { get; }
        public List<String> Endpoints { get; }
        public String SubscribeTo { get; set; }

        public SynapseEndpoint(String name, String[] endpoints, ZSocketType socketType = ZSocketType.DEALER, ZContext context = null)
        {
            Name = name;
            Endpoints = new List<String>();
            foreach (String endpoint in endpoints)
                if (!String.IsNullOrWhiteSpace(endpoint))
                    Endpoints.Add(endpoint.Trim());
            Context = context;
            SocketType = socketType;
            if (Context == null)
                Context = new ZContext();
            Socket = new ZSocket(Context, SocketType);
        }

        internal void Bind()
        {
            foreach (String endpoint in Endpoints)
            {
                Socket.Bind(endpoint);
                if (String.IsNullOrWhiteSpace(SubscribeTo))
                    Socket.SubscribeAll();
                else
                    Socket.Subscribe(SubscribeTo);
                Console.WriteLine(SocketType + " Socket Bound To " + endpoint);
            }
        }

        internal void Unbind()
        {
            foreach (String endpoint in Endpoints)
                Socket.Unbind(endpoint);
        }

        internal void Connect()
        {
            foreach (String endpoint in Endpoints)
            {
                Socket.Connect(endpoint);
                if (String.IsNullOrWhiteSpace(SubscribeTo))
                    Socket.SubscribeAll();
                else
                    Socket.Subscribe(SubscribeTo);
                Console.WriteLine(SocketType + " Socket Connected To " + endpoint);
            }
        }

        internal void Disconnect()
        {
            foreach (String endpoint in Endpoints)
                Socket.Disconnect(endpoint);
        }

        public void SendMessage(ISynapseMessage message)
        {
            ZError error;
            using (ZMessage outgoing = new ZMessage())
            {
                message.SentDate = DateTime.Now;
                outgoing.Add(new ZFrame(message.Serialize()));
                Console.WriteLine("<<< [" + this.Name + "][" + message.Id + "][" + message.TrackingId + "][" + message.Type + "] " + message.Body);
                if (!Socket.Send(outgoing, out error))
                {
                    if (error == ZError.ETERM)
                        return;
                    throw new ZException(error);
                }

            }
        }

        public void ReceiveMessages(Func<ISynapseMessage, ISynapseEndpoint, ISynapseMessage> callback, Boolean sendAck = false, ISynapseEndpoint replyOn = null)
        {
            ZError error;
            ZMessage request;
            ISynapseEndpoint replyUsing = this;

            if (replyOn != null)
                replyUsing = replyOn;

            while (true)
            {
                if (null == (request = Socket.ReceiveMessage(out error)))
                {
                    if (error == ZError.ETERM)
                        return;
                    throw new ZException(error);
                }

                new Thread(() => ProcessMessage(request, callback, sendAck, replyUsing)).Start();

            }
        }

        internal void ProcessMessage(ZMessage request, Func<ISynapseMessage, ISynapseEndpoint, ISynapseMessage> callback, Boolean sendAck, ISynapseEndpoint replyUsing)
        {
            int frameCount = request.Count;

            String xml = request[frameCount - 1].ReadString();

            SynapseMessage message = SynapseMessage.GetInstance(xml);
            message.ReceivedDate = DateTime.Now;

            //TODO : Debug - Remove Me
            Console.WriteLine(">>> [" + this.Name + "][" + message.Id + "][" + message.TrackingId + "][" + message.Type + "] " + message.Body);

            if (sendAck && message.Type != MessageType.ACK)
            {
                replyUsing.SendMessage(message.GetAck());
            }

            if (callback != null)
            {
                ISynapseMessage reply = callback(message, replyUsing);
                if (reply != null)
                    replyUsing.SendMessage(reply);
            }
        }
    }
}
