using System;
using System.Collections.Generic;
using System.Text;

namespace Synapse.MQ
{
    public enum MessageType {
        NONE,
        EXECUTEPLAN,
        STATUS,
        CANCELPLAN,
        ACK,
        ADMIN,
        REPLY
    }
}
