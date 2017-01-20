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
            String[] pubSubUrl = { @"tcp://localhost:5559" };
            bool debugMode = false;

            if (args.Length > 0)
            {
                String mode = args[0].ToUpper();
                if (args.Length > 1) { inboundUrl = args[1].Split(','); }
                if (args.Length > 2) { outboundUrl = args[2].Split(','); }
                if (args.Length > 3) { pubSubUrl = args[3].Split(','); }
                if (args.Length > 4) { debugMode = bool.Parse(args[4]); }

                if (mode == "PROXY")
                {
                    ProxyType type = (ProxyType)Enum.Parse(typeof(ProxyType), args[3]);
                    SynapseProxy proxy = new SynapseProxy(inboundUrl, outboundUrl, null, type);
                    Thread proxyThread = new Thread(() => proxy.Start());
                    proxy.Debug = debugMode;
                    proxyThread.Start();

                    while (true) ;
                }
                else if (mode == "CONTROLLER")
                {
                    SynapseController controller = new SynapseController(inboundUrl, outboundUrl, pubSubUrl);
                    controller.ProcessAcks = ProcessAcksController;
                    controller.ProcessStatusUpdate = ProcessStatusUpdateRequest;
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
                        if (inputStr.ToUpper().StartsWith("CANCEL"))
                        {
                            message.Type = MessageType.CANCELPLAN;
                            message.Body = inputStr.Substring(7);
                            controller.PublishMessage(message);
                        }
                        else
                        {
                            message.Type = MessageType.EXECUTEPLAN;
                            message.Body = inputStr;
                            controller.SendMessage(message);
                        }

                    }
                }
                else if (mode == "NODE")
                {
                    SynapseNode node = new SynapseNode(inboundUrl, outboundUrl, pubSubUrl);
                    node.ProcessAcks = ProcessAcksNode;
                    node.ProcessExecutePlanRequest = ProcessExecutePlanRequest;
                    node.ProcessCancelPlanRequest = ProcessCancelPlanRequest;
                    node.Start();
                }
            }
            else
                Usage();
        }

        static void Usage()
        {
            Console.WriteLine("Usage : Synapse.MQ.Tester.exe MODE [INBOUND_URL(S)] [OUTBOUND_URL(S)] [PUBSUB_URL(S)] [DEBUG_FLAG]");
            Console.WriteLine("        Synapse.MQ.Tester.exe MODE [INBOUND_URL(S)] [OUTBOUND_URL(S)] [PROXY_MODE] [DEBUG_FLAG]");
            Console.WriteLine("        - MODE       : Tells Program How To Act (See Details Below)");
            Console.WriteLine("             = PROXY      : Used for Many to Many Messaging in ZeroMQ.  Forwards Messages on InboundUrl to OutboundUrl.");
            Console.WriteLine("             = CONTROLLER : Sends Plan Start, Receives Status Update, Send Plan Cancel.");
            Console.WriteLine("             = NODE       : Receives Plan Start, Sends Status Update, Receives Plan Cancel.");
            Console.WriteLine("        - URL(S)     : Comma Separated List of ZSocket Endpoints.");
            Console.WriteLine("        - PROXY_MODE : Tells the Proxy To Execute In Request/Reply (ReqRep) or Publish/Subscribe(PubSub) Mode.");
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
                status.SequenceNumber = i+1;
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
