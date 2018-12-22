using System;
using System.Text;
using SocketRemote.Protocol;
using SocketRemote.Protocol.Client;
using SocketRemote.Protocol.Server;
using SocketRemote.Protocol.RemoteActions.Actions;
using SocketRemote.Protocol.RemoteActions;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

namespace SocketRemote.CommonClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var key = Encoding.UTF8.GetBytes("12345678876543211234567887654321");
            var server = new SocketRemoteServer("127.0.0.1", 8540, key);
            server.StartListenning();
            var client = new SocketRemoteClient("127.0.0.1", 8540, key);
            client.Connect();
            var command = new FileSystemRemoteAction();
            command.CommandProperties.Add("Action", "ls");
            command.CommandProperties.Add("Filename", @"D:\");
            var results = (List<ActionExecutionResult>)await client.SendCommand(new IRemoteAction[] { command }, new TimeSpan(0, 0, 30));
            results.ForEach(a => {
                Console.WriteLine($"{ a.State.ToString()} {a.MessageId}");
                var msg = Encoding.UTF8.GetChars(a.Message);
                Console.WriteLine(msg);
            });
            Console.ReadLine();

        }
    }
}
