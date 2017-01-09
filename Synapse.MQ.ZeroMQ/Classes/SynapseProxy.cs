using System;
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

        public SynapseProxy(String listenOn = "tcp://*:5555", String sendOn = "tcp://*:5556", ZContext context = null)
        {
            ZContext ctx = context;
            if (ctx == null)
                ctx = new ZContext();

            Listener = new SynapseEndpoint(listenOn, ctx, ZSocketType.ROUTER);
            Sender = new SynapseEndpoint(sendOn, ctx, ZSocketType.DEALER);
        }

        public void Start()
        {
            Listener.Bind();
            Sender.Bind();

            ZError error;
            if (!ZContext.Proxy(Listener.Socket, Sender.Socket, out error))
            {
                //TODO : Unbind and Dispose of Sockets????
                if (error == ZError.ETERM)
                    return;     // Interrupted
                throw new ZException(error);
            }
        }
    }
}
