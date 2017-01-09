using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using Synapse.MQ;
using Synapse.MQ.ZeroMQ;

namespace Synapse.MQ.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            SynapseMessage message = new SynapseMessage();
            message.Type = MessageType.EXECUTE;
            message.Body = "Test Message";
            message.SequenceNumber = 1;
            message.TrackingId = "0001";

            SynapseProxy controllerProxy = new SynapseProxy(@"tcp://*:5555", @"tcp://*:5556");
            Thread cProxyThread = new Thread(() => controllerProxy.Start());
            cProxyThread.Start();

            SynapseProxy nodeProxy = new SynapseProxy(@"tcp://*:5557", @"tcp://*:5558");
            Thread nProxyThread = new Thread(() => nodeProxy.Start());
            nProxyThread.Start();

            SynapseNode node = new SynapseNode();

            SynapseController controller = new SynapseController();
            controller.SendExecutePlanRequest(message);

            while (true) { Thread.Sleep(500); }

        }

        public static SynapseMessage processRequest(SynapseMessage message, SynapseEndpoint replyTo)
        {
            Console.WriteLine("*** Processing Request ***");
            Console.WriteLine("*** [" + message.Id + "][" + message.TrackingId + "][" + message.Type + "] " + message.Body);

            SynapseMessage reply = new SynapseMessage();
            reply.TrackingId = message.TrackingId;
            reply.Body = message.Body.ToUpper();
            reply.SequenceNumber = message.SequenceNumber + 1;

            return reply;
        }


    }
}
