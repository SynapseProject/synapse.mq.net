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
            String[] inboundUrl = { @"tcp://localhost:5555" };
            String[] outboundUrl = { @"tcp://localhost:5556" };

            if (args.Length > 0)
            {
                String mode = args[0].ToUpper();
                if (args.Length > 1) { inboundUrl = args[1].Split(','); }
                if (args.Length > 2) { outboundUrl = args[2].Split(','); }

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
                        message.TrackingId = "NODE_" + ("" + i).PadLeft(8, '0');
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
            Console.WriteLine("Usage : Synapse.MQ.Tester.exe MODE [INBOUND_URL(S)] [OUTBOUND_URL(S)]");
            Console.WriteLine("        - PROXY      : Used for Many to Many Messaging in ZeroMQ.  Forwards Messages on InboundUrl to OutboundUrl.");
            Console.WriteLine("        - CONTROLLER : Sends Plan Start, Receives Status Update, Replies to Plan Status Requests.");
            Console.WriteLine("        - NODE       : Receives Plan Start, Sends Status Update, Requests Plan Status and Receives Plan Status Reply.");
            Console.WriteLine("        - URL(S)     : Comma Separated List of ZSocket Endpoints.");
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
            reply.Body = "Plan [" + message.Body + "]  Status : In Progress";
            reply.SequenceNumber = 1;
            reply.TrackingId = message.TrackingId;

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
                status.SequenceNumber = i+1;
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
