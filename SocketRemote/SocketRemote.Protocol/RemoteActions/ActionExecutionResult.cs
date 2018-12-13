using System;
using System.Collections.Generic;
using System.Text;

namespace SocketRemote.Protocol.RemoteActions
{
    public class ActionExecutionResult
    {
        public ActionExecutionState State { get; set; }
        public byte[] Message { get; set; }
        public int MessageId { get; set; }
    }

    public enum ActionExecutionState
    {
        Success,
        Failed
    }
}
