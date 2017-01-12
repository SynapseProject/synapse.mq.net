﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using ZeroMQ;

namespace Synapse.MQ.ZeroMQ
{
    public class SynapseEndpoint : ISynapseEndpoint
    {
        public String Name { get; set; }
        public ZContext Context { get; }
        public ZSocket Socket { get; }
        public ZSocketType SocketType { get; }
        public String Endpoint { get; }

        public SynapseEndpoint(String name, String endpoint, ZSocketType socketType = ZSocketType.DEALER, ZContext context = null)
        {
            Name = name;
            Endpoint = endpoint;
            Context = context;
            SocketType = socketType;
            if (Context == null)
                Context = new ZContext();
            Socket = new ZSocket(Context, SocketType);
        }

        internal void Bind()
        {
            Socket.Bind(Endpoint);
            Console.WriteLine(SocketType + " Socket Bound To " + Endpoint);
        }

        internal void Unbind()
        {
            Socket.Unbind(Endpoint);
        }

        internal void Connect()
        {
            Socket.Connect(Endpoint);
            Console.WriteLine(SocketType + " Socket Connected To " + Endpoint);
        }

        internal void Disconnect()
        {
            Socket.Disconnect(Endpoint);
        }

        public void SendMessage(ISynapseMessage message)
        {
            SendMessage(message, null);
        }

        internal void SendMessage(ISynapseMessage message, String identity = null)
        {
            ZError error;
            using (ZMessage outgoing = new ZMessage())
            {
                if (String.IsNullOrWhiteSpace(identity))
                    outgoing.Add(new ZFrame(Socket.Identity));
                else
                    outgoing.Add(new ZFrame(Encoding.UTF8.GetBytes(identity)));

                message.SentDate = DateTime.Now;
                outgoing.Add(new ZFrame(message.Serialize()));
                Console.WriteLine("<<< [" + this.Name + "][" + this.Endpoint + "][" + message.Id + "][" + message.TrackingId + "][" + message.Type + "] " + message.Body);
                if (!Socket.Send(outgoing, out error))
                {
                    if (error == ZError.ETERM)
                        return;
                    throw new ZException(error);
                }

            }
        }

        internal void ReceiveMessages(Func<SynapseMessage, SynapseEndpoint, SynapseMessage> callback, Boolean sendAck = false, SynapseEndpoint replyOn = null)
        {
            ZError error;
            ZMessage request;
            SynapseEndpoint replyUsing = this;

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

                using (request)
                {
                    string identity = request[1].ReadString();
                    String xml = request[2].ReadString();

                    //TODO : Build Me
                    SynapseMessage message = SynapseMessage.GetInstance(xml);
                    message.ReceivedDate = DateTime.Now;

                    //TODO : Debug - Remove Me
                    Console.WriteLine(">>> [" + this.Name + "][" + this.Endpoint + "][" + message.Id + "][" + message.TrackingId + "][" + message.Type + "] " + message.Body);

                    if (sendAck && message.Type != MessageType.ACK)
                    {
                        replyUsing.SendMessage(message.GetAck());
                    }

                    if (callback != null)
                    {
                        SynapseMessage reply = callback(message, replyUsing);
                        if (reply != null)
                            replyUsing.SendMessage(reply);
                    }
                }
            }
        }



        internal void ReceiveReplies(Func<SynapseMessage, String> callback, Boolean sendAck = false, SynapseEndpoint replyOn = null)
        {
            ZError error;
            ZMessage incoming;
            ZPollItem poll = ZPollItem.CreateReceiver();
            SynapseEndpoint replyUsing = this;

            if (replyOn != null)
                replyUsing = replyOn;

            while (true)
            {
                if (!Socket.PollIn(poll, out incoming, out error, TimeSpan.FromMilliseconds(10)))
                {
                    if (error == ZError.EAGAIN)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                    if (error == ZError.ETERM)
                        return;
                    throw new ZException(error);
                }
                using (incoming)
                {
                    String xml = incoming[0].ReadString();

                    SynapseMessage message = SynapseMessage.GetInstance(xml);

                    Console.WriteLine(">>> [" + this.Name + "][" + this.Endpoint + "][" + message.Id + "][" + message.TrackingId + "][" + message.Type + "] " + message.Body);

                    if (sendAck && message.Type != MessageType.ACK)
                    {
                        replyUsing.SendMessage(message.GetAck());
                    }

                    if (callback != null)
                        callback(message);
                }

            }
        }


    }
}
