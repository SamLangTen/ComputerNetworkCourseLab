using System;
using System.Collections.Generic;
using System.Text;

namespace SocketRemote.Protocol.Server
{
    public class RemoteActionMessage
    {
        public int MessageId { get; set; }
        public int ActionId { get; set; }
        public byte[]  Content { get; set; }
    }
}
