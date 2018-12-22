using SocketRemote.Protocol.Server;
using System;
using System.Text;

namespace SocketRemote.CommonServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Password(longer than 48Bytes):");
            var keyString = Console.ReadLine();
            Console.Write("Listenning Host:");
            var host = Console.ReadLine();
            Console.Write("Port:");
            var port = Console.ReadLine();
            var server = new SocketRemoteServer(host, int.Parse(port), Encoding.UTF8.GetBytes(keyString));
            server.ClientMessageReceived += Server_ClientMessageReceived;
            server.ServerMessagePrepared += Server_ServerMessagePrepared;
            server.StartListenning();
            Console.ReadKey();
            server.EndListenning();
        }

        private static void Server_ServerMessagePrepared(object sender, Protocol.Server.Event.StringEventArgs e)
        {
            Console.WriteLine(e.Content);
        }

        private static void Server_ClientMessageReceived(object sender, Protocol.Server.Event.StringEventArgs e)
        {
            Console.WriteLine(e.Content);
        }
    }
}
