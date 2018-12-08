using System;
using System.Collections.Generic;
using System.Text;

namespace SocketRemote.Protocol.RemoteActions
{
    public interface IRemoteAction
    {
        int ActionId { get; }
        ActionExecutionResult Execute(byte[] command);
        string GetServerCommand();
        Dictionary<string,string> CommandProperties { get; set; }

    }
}
