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
            String inboundUrl = @"tcp://localhost:5555";
            String outboundUrl = @"tcp://localhost:5556";



            if (args.Length > 0)
            {
                String mode = args[0].ToUpper();
                if (args.Length > 1) { inboundUrl = args[1]; }
                if (args.Length > 2) { outboundUrl = args[2]; }

                if (mode == "PROXY")
                {
                    SynapseProxy proxy = new SynapseProxy(inboundUrl, outboundUrl);
                    Thread proxyThread = new Thread(() => proxy.Start());
                    proxyThread.Start();

                    while (true) ;
                }
                else if (mode == "CONTROLLER")
                {
                    SynapseController controller = new SynapseController(inboundUrl, outboundUrl);
                    controller.ProcessAcks = ProcessAcksController;
                    controller.ProcessPlanStatus = ProcessPlanStatusRequest;
                    controller.ProcessStatusUpdate = ProcessStatusUpdateRequest;

                    int i = 0;
                    String inputStr = String.Empty;
                    while (true)
                    {
                        inputStr = Console.ReadLine().Trim();

                        i++;
                        SynapseMessage message = new SynapseMessage();
                        message.SequenceNumber = i;
                        message.TrackingId = "CONTROLLER_" + ("" + i).PadLeft(8, '0');
                        message.Type = MessageType.EXECUTEPLAN;
                        message.Body = inputStr;

                        controller.SendMessage(message);
                    }
                }
                else if (mode == "NODE")
                {
                    SynapseNode node = new SynapseNode(inboundUrl, outboundUrl);
                    node.ProcessAcks = ProcessAcksNode;
                    node.ProcessExecutePlanRequest = ProcessExecutePlanRequest;
                    node.ProcessPlanStatusReply = ProcessPlanStatusReply;

                    int i = 0;
                    String inputStr = String.Empty;
                    while (true)
                    {
                        inputStr = Console.ReadLine().Trim();

                        i++;
                        SynapseMessage message = new SynapseMessage();
                        message.SequenceNumber = i;
                        message.TrackingId = "NODE" + ("" + i).PadLeft(8, '0');
                        message.Type = MessageType.PLANSTATUS_REQUEST;
                        message.Body = inputStr;

                        node.SendMessage(message);
                    }
                }
            }
            else
                Usage();
        }

        static void Usage()
        {
            Console.WriteLine("Usage : Synapse.MQ.Tester.exe MODE [INBOUND_URL] [OUTBOUND_URL]");
            Console.WriteLine("        - PROXY      : Used for Many to Many Messaging in ZeroMQ.  Forwards Messages on InboundUrl to OutboundUrl.");
            Console.WriteLine("        - CONTROLLER : Sends Plan Start, Receives Status Update, Replies to Plan Status Requests.");
            Console.WriteLine("        - NODE       : Receives Plan Start, Sends Status Update, Requests Plan Status and Receives Plan Status Reply.");
        }

        static void TestLocal(string[] args)
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

        public static ISynapseMessage ProcessAcksController(ISynapseMessage message)
        {
            Console.WriteLine("*** SynapseController : ProcessAcks ***");
            Console.WriteLine(message);
            Console.WriteLine("************************************************");

            return null;
        }

        public static ISynapseMessage ProcessPlanStatusRequest(ISynapseMessage message)
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

        public static ISynapseMessage ProcessStatusUpdateRequest(ISynapseMessage message)
        {
            Console.WriteLine("*** SynapseController : ProcessStatusUpdateRequest ***");
            Console.WriteLine(message);
            Console.WriteLine("************************************************");

            return null;
        }

        public static ISynapseMessage ProcessExecutePlanRequest(ISynapseMessage message, ISynapseEndpoint endpoint)
        {
            Console.WriteLine("*** SynapseNode : ProcessExecutePlanRequests ***");
            Console.WriteLine(message);
            Console.WriteLine("************************************************");

            for (int i=0; i<message.Body.Length; i++)
            {
                Thread.Sleep(3000);
                SynapseMessage status = new SynapseMessage();
                status.Type = MessageType.STATUS;
                status.TrackingId = message.TrackingId;
                status.SequenceNumber = i;
                status.Body = message.Body.Substring(0, (i+1)).ToUpper();

                if (endpoint != null)
                    endpoint.SendMessage(status);
            }

            return null;
        }

        public static ISynapseMessage ProcessAcksNode(ISynapseMessage message)
        {
            Console.WriteLine("*** SynapseNode : ProcessAcks ***");
            Console.WriteLine(message);
            Console.WriteLine("************************************************");

            return null;
        }

        public static ISynapseMessage ProcessPlanStatusReply(ISynapseMessage message)
        {
            Console.WriteLine("*** SynapseNode : ProcessPlanStatusReply ***");
            Console.WriteLine(message);
            Console.WriteLine("************************************************");

            return null;
        }
    }
}
