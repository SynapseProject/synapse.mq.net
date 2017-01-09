using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using ZeroMQ;

namespace Synapse.MQ.ZeroMQ
{
    public class SynapseEndpoint
    {
        public ZContext Context { get; }
        public ZSocket Socket { get; }
        public ZSocketType SocketType { get; }
        public String Endpoint { get; }

        public SynapseEndpoint(String endpoint, ZContext context = null, ZSocketType socketType = ZSocketType.DEALER)
        {
            Endpoint = endpoint;
            Context = context;
            SocketType = socketType;
            if (Context == null)
                Context = new ZContext();
            Socket = new ZSocket(Context, SocketType);
        }

        public void Bind()
        {
            Socket.Bind(Endpoint);
            Console.WriteLine(SocketType + " Socket Bound To " + Endpoint);
        }

        public void Unbind()
        {
            Socket.Unbind(Endpoint);
        }

        public void Connect()
        {
            Socket.Connect(Endpoint);
            Console.WriteLine(SocketType + " Socket Connected To " + Endpoint);
        }

        public void Disconnect()
        {
            Socket.Disconnect(Endpoint);
        }


        public void SendMessage(SynapseMessage message)
        {
            ZError error;
            using (ZMessage outgoing = new ZMessage())
            {
                if (message.ReplyTo == null)
                    outgoing.Add(new ZFrame(Socket.Identity));
                else
                    outgoing.Add(new ZFrame(Encoding.UTF8.GetBytes(message.ReplyTo)));
                outgoing.Add(new ZFrame(message.ToString()));
                Console.WriteLine("<<< [" + message.Id + "][" + message.TrackingId + "][" + message.Type + "] " + message.Body);
                if (!Socket.Send(outgoing, out error))
                {
                    if (error == ZError.ETERM)
                        return;
                    throw new ZException(error);
                }

            }
        }

        public void ReceiveMessages(Func<SynapseMessage, SynapseEndpoint, SynapseMessage> callback, Boolean sendAck = true, SynapseEndpoint replyOn = null)
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
                    SynapseMessage message = SynapseMessage.FromString(xml);

                    //TODO : Debug - Remove Me
                    Console.WriteLine(">>> [" + message.Id + "][" + message.TrackingId + "][" + message.Type + "] " + message.Body);

                    //TODO : Build Ack Message
                    if (sendAck)
                    {
                        replyUsing.SendMessage(SynapseMessage.GetAck(message));
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



        public void ReceiveReplies(Func<SynapseMessage, String> callback)
        {
            ZError error;
            ZMessage incoming;
            ZPollItem poll = ZPollItem.CreateReceiver();

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

                    SynapseMessage message = SynapseMessage.FromString(xml);

                    if (callback != null)
                        callback(message);
                }

            }
        }


    }
}
