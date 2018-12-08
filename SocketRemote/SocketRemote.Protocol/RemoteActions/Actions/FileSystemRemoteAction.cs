using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
namespace SocketRemote.Protocol.RemoteActions.Actions
{
    public class FileSystemRemoteAction : IRemoteAction
    {
        public int ActionId => 1;

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// 可用的命令：
        ///     Action(ls) - 执行的文件系统操作
        ///     Filename   - 执行操作的文件系统路径 
        /// </remarks>
        public Dictionary<string, string> CommandProperties { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public FileSystemRemoteAction()
        {
            CommandProperties = new Dictionary<string, string>();
        }

        #region "Server"

        private string listDirectory(string path) => string.Join("\n", Directory.GetFiles(path));

        public ActionExecutionResult Execute(byte[] command)
        {
            var textCommand = Encoding.UTF8.GetChars(command).ToString();
            var textResultMessage = "";
            var state = ActionExecutionState.Success;
            /*
             command[0] = 
             "0" -> List Directory
            */
            switch (textCommand[0])
            {
                case '0':
                    textResultMessage = textCommand.Substring(1);
                    break;
                default:
                    textResultMessage = "Unregnoized Command";
                    state = ActionExecutionState.Failed;
                    break;
            }
            return new ActionExecutionResult() { State = state, Message = Encoding.UTF8.GetBytes(textResultMessage) };
        }

        #endregion

        #region "Client"

        public string GetServerCommand()
        {
            var command = "";
            var action = CommandProperties.FirstOrDefault(kvp => kvp.Key == "Action");
            if (action.Value == "ls") command = "0";
            var filename = CommandProperties.FirstOrDefault(kvp => kvp.Key == "Filename");
            command += filename.Value;
            return command;

        }

        #endregion




    }
}
