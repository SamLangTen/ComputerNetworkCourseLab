using System;
using System.Collections.Generic;
using System.Text;

namespace SocketRemote.Protocol.Server.Event
{
    public class StringEventArgs:System.EventArgs
    {
        public string Content { get; set; }
    }
}
