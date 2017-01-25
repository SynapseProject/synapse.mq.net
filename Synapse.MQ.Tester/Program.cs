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
            String name = Guid.NewGuid().ToString();
            String group = String.Empty;
            String[] inboundUrl = { @"tcp://localhost:5556" };
            String[] outboundUrl = { @"tcp://localhost:5555" };
            bool debugMode = false;

            if (args.Length > 0)
            {
                String mode = args[0].ToUpper();
                if (args.Length > 1) { name = args[1]; }
                if (args.Length > 2) { group = args[2]; }
                if (args.Length > 3) { inboundUrl = args[3].Split(','); }
                if (args.Length > 4) { outboundUrl = args[4].Split(','); }
                if (args.Length > 5) { debugMode = bool.Parse(args[5]); }

                if (mode == "BROKER")
                {
                    SynapseBroker broker = new SynapseBroker(inboundUrl, outboundUrl, debugMode);
                    broker.Start();
                }
                else if (mode == "CONTROLLER")
                {
                    SynapseController controller = new SynapseController(inboundUrl, outboundUrl);
                    controller.ProcessAcks = ProcessAcksController;
                    controller.ProcessStatusUpdate = ProcessStatusUpdateRequest;
                    controller.Id = name;
                    controller.GroupId = group;
                    controller.Debug = debugMode;
                    controller.Start();

                    int i = 0;
                    String inputStr = String.Empty;
                    while (true)
                    {
                        inputStr = Console.ReadLine().Trim();

                        i++;
                        SynapseMessage message = new SynapseMessage();
                        message.SequenceNumber = i;
                        message.TrackingId = "CONTROLLER_" + ("" + i).PadLeft(8, '0');
                        message.TargetGroup = controller.GroupId;
                        message.SenderId = controller.Id;

                        if (inputStr.ToUpper().StartsWith("CANCEL"))
                        {
                            message.Type = MessageType.CANCELPLAN;
                            message.Target = "CANCELPLAN";
                            message.Body = inputStr.Substring(7);
                            message.AckRequested = false;
                            controller.SendMessage(message);
                        }
                        else if (inputStr.ToUpper().StartsWith("EXIT"))
                        {
                            controller.Unregister();
                            Environment.Exit(0);
                        }
                        else
                        {
                            message.Type = MessageType.EXECUTEPLAN;
                            message.Target = "EXECUTEPLAN";
                            message.Body = inputStr;
                            message.AckRequested = true;
                            controller.SendMessage(message);
                        }

                    }
                }
                else if (mode == "NODE")
                {
                    SynapseNode node = new SynapseNode(inboundUrl, outboundUrl);
                    node.ProcessAcks = ProcessAcksNode;
                    node.ProcessExecutePlanRequest = ProcessExecutePlanRequest;
                    node.ProcessCancelPlanRequest = ProcessCancelPlanRequest;
                    node.Id = name;
                    node.GroupId = group;
                    node.Debug = debugMode;
                    node.Start();

                    int i = 0;
                    String inputStr = String.Empty;
                    while (true)
                    {
                        inputStr = Console.ReadLine().Trim();
                        i++;
                        if (inputStr.ToUpper().StartsWith("EXIT"))
                        {
                            node.Unregister();
                            Environment.Exit(0);
                        }

                    }
                }
            }
            else
                Usage();
        }

        static void Usage()
        {
            Console.WriteLine("Usage : Synapse.MQ.Tester.exe MODE [NAME] [GROUP] [INBOUND_URL(S)] [OUTBOUND_URL(S)]  [DEBUG_FLAG]");
            Console.WriteLine("        - MODE       : Tells Program How To Act (See Details Below)");
            Console.WriteLine("             = BROKER     : Used for Many to Many Messaging in ZeroMQ.  Forwards Messages on InboundUrl to OutboundUrl.");
            Console.WriteLine("             = CONTROLLER : Sends Plan Start, Receives Status Update, Send Plan Cancel.");
            Console.WriteLine("             = NODE       : Receives Plan Start, Sends Status Update, Receives Plan Cancel.");
            Console.WriteLine("        - NAME       : Unique Name Identifying Synapse Object");
            Console.WriteLine("        - GROUP      : Group Id For Directed Messaging.");
            Console.WriteLine("        - URL(S)     : Comma Separated List of ZSocket Endpoints.");
            Console.WriteLine("        - DEBUG_FLAG : Puts Synapse Object Into Debug Mode (True/False)");
        }

        public static ISynapseMessage ProcessAcksController(ISynapseMessage message, ISynapseEndpoint endpoint)
        {
            Console.WriteLine("*** SynapseController : ProcessAcks ***");
            Console.WriteLine(message);
            Console.WriteLine("************************************************");

            return null;
        }
        public static ISynapseMessage ProcessStatusUpdateRequest(ISynapseMessage message, ISynapseEndpoint endpoint)
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
                status.TargetGroup = message.TargetGroup;
                status.Target = "STATUS";
                status.SenderId = "ProcessExecutePlanRequest-SendStatus";
                status.SequenceNumber = i+1;
                status.AckRequested = true;
                status.Body = message.Body.Substring(0, (i+1)).ToUpper();

                if (endpoint != null)
                    endpoint.SendMessage(status);
            }

            return null;
        }

        public static ISynapseMessage ProcessAcksNode(ISynapseMessage message, ISynapseEndpoint endpoint)
        {
            Console.WriteLine("*** SynapseNode : ProcessAcks ***");
            Console.WriteLine(message);
            Console.WriteLine("************************************************");

            return null;
        }

        public static ISynapseMessage ProcessCancelPlanRequest(ISynapseMessage message, ISynapseEndpoint endpoint)
        {
            Console.WriteLine("*** SynapseNode : ProcessCancelPlanRequest ***");
            Console.WriteLine(message);
            Console.WriteLine("************************************************");

            return null;
        }
    }
}
