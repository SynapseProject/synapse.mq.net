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
            SynapseProxy controllerProxy = new SynapseProxy(@"tcp://*:5555", @"tcp://*:5556");
            Thread cProxyThread = new Thread(() => controllerProxy.Start());
            cProxyThread.Start();

            SynapseProxy nodeProxy = new SynapseProxy(@"tcp://*:5557", @"tcp://*:5558");
            Thread nProxyThread = new Thread(() => nodeProxy.Start());
            nProxyThread.Start();

            SynapseController controller = new SynapseController();
            controller.ProcessAcks = ProcessAcksController;
            controller.ProcessPlanStatus = ProcessPlanStatusRequest;
            controller.ProcessStatusUpdate = ProcessStatusUpdateRequest;

            SynapseNode node = new SynapseNode();
            node.ProcessAcks = ProcessAcksNode;
            node.ProcessExecutePlanRequest = ProcessExecutePlanRequest;
            node.ProcessPlanStatusReply = ProcessPlanStatusReply;


            SynapseMessage message = new SynapseMessage();
            message.Type = MessageType.EXECUTEPLAN;
            message.Body = "Test Message";
            message.SequenceNumber = 1;
            message.TrackingId = "0001";
//            controller.SendMessage(message);

            message = new SynapseMessage();
            message.Type = MessageType.STATUS;
            message.Body = "Status Update : Completed";
            message.SequenceNumber = 2;
            message.TrackingId = "0001";
//            node.SendMessage(message);

            message = new SynapseMessage();
            message.Type = MessageType.PLANSTATUS_REQUEST;
            message.Body = "Need Status For Plan";
            message.SequenceNumber = 3;
            message.TrackingId = "0003";
            node.SendMessage(message);

            while (true) { Thread.Sleep(500); }

        }

        public static SynapseMessage ProcessAcksController(SynapseMessage message)
        {
            Console.WriteLine("*** SynapseController : ProcessAcks ***");
            Console.WriteLine(message);
            Console.WriteLine("************************************************");

            return null;
        }

        public static SynapseMessage ProcessPlanStatusRequest(SynapseMessage message)
        {
            Console.WriteLine("*** SynapseController : ProcessPlanStatusRequest ***");
            Console.WriteLine(message);
            Console.WriteLine("************************************************");

            SynapseMessage reply = new SynapseMessage();
            reply.Type = MessageType.PLANSTATUS_REPLY;
            reply.Body = "Plan Status : In Progress";
            reply.SequenceNumber = 4;
            reply.TrackingId = "0003";
            //            Outbound.SendMessage(reply);

            return reply;
        }

        public static SynapseMessage ProcessStatusUpdateRequest(SynapseMessage message)
        {
            Console.WriteLine("*** SynapseController : ProcessStatusUpdateRequest ***");
            Console.WriteLine(message);
            Console.WriteLine("************************************************");

            return null;
        }

        public static SynapseMessage ProcessExecutePlanRequest(SynapseMessage message)
        {
            Console.WriteLine("*** SynapseNode : ProcessExecutePlanRequests ***");
            Console.WriteLine(message);
            Console.WriteLine("************************************************");

            return null;
        }

        public static SynapseMessage ProcessAcksNode(SynapseMessage message)
        {
            Console.WriteLine("*** SynapseNode : ProcessAcks ***");
            Console.WriteLine(message);
            Console.WriteLine("************************************************");

            return null;
        }

        public static SynapseMessage ProcessPlanStatusReply(SynapseMessage message)
        {
            Console.WriteLine("*** SynapseNode : ProcessPlanStatusReply ***");
            Console.WriteLine(message);
            Console.WriteLine("************************************************");

            return null;
        }
    }
}
