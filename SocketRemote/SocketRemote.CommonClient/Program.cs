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
        static void Main(string[] args)
        {
            Console.Write("Password(longer than 48Bytes):");
            var keyString = Console.ReadLine();
            Console.Write("Connect Host:");
            var host = Console.ReadLine();
            Console.Write("Port:");
            var port = Console.ReadLine();
            while(true)
            {
                var client = new SocketRemoteClient("127.0.0.1", 8540, Encoding.UTF8.GetBytes(keyString));
                Console.Write("Filename:");
                var filename = Console.ReadLine();
                var command = new FileSystemRemoteAction();
                command.CommandProperties.Add("Action", "ls");
                command.CommandProperties.Add("Filename", filename);
                client.Connect();
                var res = client.SendCommand(new IRemoteAction[] { command }, new TimeSpan(0, 0, 30)).GetEnumerator();
                while (res.MoveNext())
                {
                    var a = res.Current;
                    Console.WriteLine($"{ a.State.ToString()} {a.MessageId}");
                    var msg = Encoding.UTF8.GetChars(a.Message);
                    Console.WriteLine(msg);
                }
                client.Disconnect();
            }

        }
    }
}
