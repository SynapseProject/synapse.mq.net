using System;
using System.Collections.Generic;
using System.Text;

namespace Synapse.MQ
{
    public enum MessageType {
        NONE,
        EXECUTEPLAN,
        STATUS,
        PLANSTATUS_REQUEST,
        PLANSTATUS_REPLY,
        ACK
    }
}
