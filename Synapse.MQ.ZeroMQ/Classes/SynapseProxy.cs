﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ZeroMQ;

namespace Synapse.MQ.ZeroMQ
{
    public class SynapseProxy
    {
        SynapseEndpoint Listener;
        SynapseEndpoint Sender;

        public SynapseProxy(String[] listenOn, String[] sendOn, ZContext context = null)
        {
            ZContext ctx = context;
            if (ctx == null)
                ctx = new ZContext();

            Listener = new SynapseEndpoint("ProxyListener", listenOn, ZSocketType.ROUTER, ctx);
            Sender = new SynapseEndpoint("ProxySender", sendOn, ZSocketType.DEALER, ctx);
        }

        public void Start()
        {
            Listener.Bind();
            Sender.Bind();

            ZPollItem poll = ZPollItem.CreateReceiver();
            ZError error;
            ZMessage message;

            while (true)
            {
                if (Listener.Socket.PollIn(poll, out message, out error, TimeSpan.FromMilliseconds(64)))
                {
                    WriteMessage(message);
                    Sender.Socket.Send(message);
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
                    WriteMessage(message);
                    Listener.Socket.Send(message);
                }
                else
                {
                    if (error == ZError.ETERM)
                        return;
                    if (error != ZError.EAGAIN)
                        throw new ZException(error);
                }
            }

            /*            if (!ZContext.Proxy(Listener.Socket, Sender.Socket, out error))
                        {
                            //TODO : Unbind and Dispose of Sockets????
                            if (error == ZError.ETERM)
                                return;     // Interrupted
                            throw new ZException(error);
                        } */
        }

        public static void WriteMessage(ZMessage message)
        {
            Console.Write(">> ");
            for (int i=0; i<message.Count; i++)
                Console.Write("[" + message[i].ReadString() + "]");
            Console.WriteLine();
        }
    }
}
