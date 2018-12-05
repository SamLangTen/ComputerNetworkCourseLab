using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SocketRemote.Protocol.RemoteActions.Actions
{
    public class FileSystemRemoteAction : IRemoteAction
    {
        public int ActionId => 1;

        #region "Server"

        private string listDirectory(string path) => string.Join("\n", Directory.GetFiles(path));

        public ActionExecutionResult Execute(byte[] command)
        {
            /*
             command[0] = 
             "0" -> List Directory
            */
            switch (command[0])
            {
                case '0':
                    return new ActionExecutionResult() { State = ActionExecutionState.Success, Message = listDirectory(command.Substring(1)) };
                default:
                    return new ActionExecutionResult() { State = ActionExecutionState.Failed, Message = "Unregnoized Command" };
            }
        }


        #endregion

        #region "Client"

        public string GetServerCommand()
        {
            throw new NotImplementedException();
        }

        #endregion




    }
}
