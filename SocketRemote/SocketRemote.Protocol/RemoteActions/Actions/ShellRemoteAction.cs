using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using SocketRemote.Protocol.RemoteActions;
using System.IO;

namespace SocketRemote.Protocol.RemoteActions.Actions
{
    public class ShellRemoteAction : IRemoteAction
    {
        public int ActionId => 2;

        public Dictionary<string, string> CommandProperties { get; set; } = new Dictionary<string, string>();

        public ActionExecutionResult Execute(byte[] command)
        {
            try
            {
                var commandString = new string(Encoding.UTF8.GetChars(command));
                var cmd = commandString.Split('\n')[0];
                var arg = commandString.Split('\n')[1];
                var process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = cmd;
                process.StartInfo.Arguments = arg;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();
                process.WaitForExit(2000);
                process.Kill();
                var output = process.StandardOutput.ReadToEnd();
                return new ActionExecutionResult()
                {
                    Message = Encoding.UTF8.GetBytes(output),
                    State = ActionExecutionState.Success
                };
            }
            catch (Exception ex)
            {
                return new ActionExecutionResult()
                {
                    Message = Encoding.UTF8.GetBytes(ex.Message),
                    State = ActionExecutionState.Failed
                };
            }
        }

        public string GetServerCommand()
        {
            var command = CommandProperties.FirstOrDefault(kvp => kvp.Key == "Filename");
            var arg = CommandProperties.FirstOrDefault(kvp => kvp.Key == "Arguments");
            return command.Value + "\n" + arg.Value;
        }
    }
}
