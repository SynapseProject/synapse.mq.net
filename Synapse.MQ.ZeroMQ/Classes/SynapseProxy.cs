using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ZeroMQ;

namespace Synapse.MQ.ZeroMQ
{
    public enum ProxyType { ReqRep, PubSub };

    public class SynapseProxy
    {
        SynapseEndpoint Listener;
        SynapseEndpoint Sender;
        public bool Debug { get; set; }
        public ProxyType Type { get; protected set; }

        public SynapseProxy(String[] listenOn, String[] sendOn, ZContext context = null, ProxyType type = ProxyType.ReqRep)
        {
            ZContext ctx = context;
            if (ctx == null)
                ctx = new ZContext();

            Type = type;
            if (Type == ProxyType.ReqRep)
            {
                Listener = new SynapseEndpoint("ProxyListener", listenOn, ZSocketType.ROUTER, ctx);
                Sender = new SynapseEndpoint("ProxySender", sendOn, ZSocketType.DEALER, ctx);
            }
            else
            {
                Listener = new SynapseEndpoint("ProxyListener", listenOn, ZSocketType.XSUB, ctx);
                Sender = new SynapseEndpoint("ProxySender", sendOn, ZSocketType.XPUB, ctx);
            }
        }

        private void init()
        {
            Debug = false;
            Type = ProxyType.ReqRep;
        }

        public void Start()
        {
            Listener.Bind();
            Sender.Bind();
            Console.WriteLine("Debug Mode : " + Debug);
            Console.WriteLine("Proxy Type : " + Type);

            ZPollItem poll = ZPollItem.CreateReceiver();
            ZError error;
            ZMessage message;

            while (true)
            {
                if (Listener.Socket.PollIn(poll, out message, out error, TimeSpan.FromMilliseconds(64)))
                {
                    if (Debug)
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
                    if (Debug)
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
// TODO : Replace Above Code With Lines Below Once Debugging Is Completed.
/*  
                        if (!ZContext.Proxy(Listener.Socket, Sender.Socket, out error))
                        {
                            //TODO : Unbind and Dispose of Sockets????
                            if (error == ZError.ETERM)
                                return;     // Interrupted
                            throw new ZException(error);
                        } 
*/
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
