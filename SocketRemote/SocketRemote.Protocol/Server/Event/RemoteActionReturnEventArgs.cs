using System;
using System.Collections.Generic;
using System.Text;
using SocketRemote.Protocol.RemoteActions;
namespace SocketRemote.Protocol.Server
{
    public class RemoteActionReturnEventArgs: EventArgs
    {
        public ActionExecutionResult Result { get; set; }
        public int RemoteActionId { get; set; }
        public int MessageId { get; set; }
    }
}
