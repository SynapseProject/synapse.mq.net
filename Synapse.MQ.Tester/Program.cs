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

            SynapseNode node = new SynapseNode();

            SynapseController controller = new SynapseController();

            SynapseMessage message = new SynapseMessage();
            message.Type = MessageType.EXECUTE;
            message.Body = "Test Message";
            message.SequenceNumber = 1;
            message.TrackingId = "0001";
            controller.SendExecutePlanRequest(message);

            message = new SynapseMessage();
            message.Type = MessageType.STATUS;
            message.Body = "Status Update : Completed";
            message.SequenceNumber = 2;
            message.TrackingId = "0001";
            node.SendStatusUpdateRequest(message);

            message = new SynapseMessage();
            message.Type = MessageType.PLANSTATUS_REQUEST;
            message.Body = "Need Status For Plan";
            message.SequenceNumber = 3;
            message.TrackingId = "0003";
            node.SendPlanStatusRequest(message);

            while (true) { Thread.Sleep(500); }

        }
    }
}
