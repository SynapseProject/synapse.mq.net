using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ZeroMQ;

namespace Synapse.MQ.ZeroMQ
{
    class ZeroMQUtils
    {
        public static void WriteRawMessage(ZMessage message, bool showHex = false)
        {
            Console.WriteLine("***** " + DateTime.Now + " - Total Frames : " + message.Count + " *****");
            for(int i=0; i<message.Count; i++)
            {
                ZFrame frame = message[i].Duplicate();
                Console.Write("Frame [" + i + "] : Length [" + frame.Length + "] : [");

                byte[] bytes = new byte[frame.Length];
                frame.Read(bytes, 0, (int)frame.Length);
                String frameStr = System.Text.Encoding.Default.GetString(bytes);

                Console.WriteLine(frameStr + "]");

                if (showHex)
                    Console.WriteLine("      [" + BitConverter.ToString(bytes) + "]");
            }
            Console.WriteLine("*********************************************");
        }
    }
}
