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
using System.Linq;
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
            while (true)
            {
                var client = new SocketRemoteClient(host, int.Parse(port), Encoding.UTF8.GetBytes(keyString));
                //加载可用的RemoteAction
                var actionsType = RemoteActionManager.GetAllRemoteActions();
                var actionsInstance = RemoteActionManager.GetAllRemoteActionInstances();
                Console.WriteLine("Available RA:");
                Console.WriteLine(string.Join("\n", actionsInstance.Select(a => a.ActionId.ToString() + ":" + a.GetType().Name)));
                Console.Write("Select:");
                var selected = int.Parse(Console.ReadLine());
                //初始化RA
                var ra = actionsInstance.FirstOrDefault(t => t.ActionId == selected);
                Console.WriteLine("\"Property=Value\" To set action properties");
                Console.WriteLine("\"Send\" To send action");
                var command = Console.ReadLine();
                while (command.ToLower().Trim() != "send")
                {
                    if (!command.Contains("="))
                        continue;
                    var prop = command.Split('=')[0];
                    var value = string.Join("=", command.Split('=').Skip(1).ToArray());
                    ra.CommandProperties.Add(prop, value);
                    command = Console.ReadLine();
                }
                client.Connect();
                var res = client.SendCommand(new IRemoteAction[] { ra }, new TimeSpan(0, 0, 30)).GetEnumerator();
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
