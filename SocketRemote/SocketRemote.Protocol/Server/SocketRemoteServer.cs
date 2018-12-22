using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using SocketRemote.Protocol.Authentication;
using SocketRemote.Protocol.RemoteActions;
using SocketRemote.Protocol.Server.Event;

namespace SocketRemote.Protocol.Server
{
    public class SocketRemoteServer
    {
        private IPEndPoint _addressSocket;
        private DistributionManager _distMan;
        private SRAuthentication _auth;
        private Socket _socket;
        private CancellationTokenSource _bgcts;
        private IList<RemoteActionReturnEventArgs> _returnMessages;

        public event EventHandler<StringEventArgs> ClientMessageReceived;
        public event EventHandler<StringEventArgs> ServerMessagePrepared;

        private void backgroundListenning()
        {
            _socket.Bind(_addressSocket);
            _socket.Listen(10);
            while (true)
            {
                var handler = _socket.Accept();
                Task.Run(() => connectionHandler(handler));
            }
        }

        /// <summary>
        /// 当新连接建立时的处理程序
        /// </summary>
        /// <param name="socket">与客户端通讯的Socket</param>
        private void connectionHandler(Socket socket)
        {
            var byteBuffer = new byte[1024];
            var packetRec = new List<Byte>();
            int byteRec;
            do
            {
                byteRec = socket.Receive(byteBuffer, byteBuffer.Length, SocketFlags.None);
                packetRec.AddRange(byteBuffer.Take(byteRec));
            } while (byteRec > 0 && !checkPacket(packetRec.ToArray()));
            var messageIds = parsePacket(packetRec.ToArray(), socket);
            //等待包返回
            while (messageIds.Count > 0)
            {
                try
                {
                    var message = _returnMessages.FirstOrDefault(m => messageIds.Contains(m.MessageId));
                    if (message != null)
                    {
                        //触发事件
                        ServerMessagePrepared?.Invoke(this, new StringEventArgs()
                        {
                            Content = $"IP:{socket.RemoteEndPoint.AddressFamily.ToString()}\tMessageId:{message.MessageId.ToString()}"
                        });
                        //发送
                        var sendingData = generateReturnData(message.Result, message.RemoteActionId, message.MessageId);
                        socket.Send(sendingData);
                        messageIds.Remove(messageIds.First(i => i == message.MessageId));
                    }
                }
                catch (Exception)
                {
                    continue;
                }


            }
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        private bool checkPacket(byte[] datas)
        {
            var stringData = Encoding.UTF8.GetChars(datas);
            var seek = new string(stringData).IndexOf("SSSR");
            if (seek == -1) return false;
            seek += 4;
            if (seek + 2 >= datas.Length) return false;
            var length = Convert.ToInt32(BitConverter.ToInt16(datas.Skip(seek).Take(2).ToArray(), 0));
            if (seek + length < datas.Length) return true;
            return false;
        }

        /// <summary>
        /// 分析接收到的数据，处理粘包、断包等问题
        /// </summary>
        /// <param name="data">接收到的数据</param>
        private IList<int> parsePacket(byte[] datas, Socket socket)
        {
            var messagesId = new List<int>();
            var stringData = Encoding.UTF8.GetString(datas);
            int seek = 0;
            while (seek < datas.Count())
            {
                //SSSR协议包头四个字节为字符串SSSR
                seek = new string(stringData.Skip(seek).ToArray()).IndexOf("SSSR");
                if (seek == -1)
                {
                    socket.Send(Encoding.UTF8.GetBytes("This is a SSSocketRemote protocol server"));
                    break;
                }
                seek += 4;
                //然后是两个字节表示包的长度
                var length = (datas[seek + 1] << 8) + datas[seek];
                seek += 2;
                //然后剩下length字节数据为命令长度
                if (!(seek + length <= datas.Count())) break;
                var packetData = datas.Skip(seek).Take(length).ToArray();
                //解密数据，交给命令处理器处理命令
                var decryptedText = _auth.Decrpyt(packetData);
                var messageId = parseInstruction(decryptedText);
                //触发服务器事件
                ClientMessageReceived?.Invoke(this, new StringEventArgs()
                {
                    Content = $"IP:{socket.RemoteEndPoint.AddressFamily.ToString()}\tMessageId:{messageId.ToString()}\tMessage:{new string(Encoding.UTF8.GetChars(decryptedText))}"
                });
                messagesId.Add(messageId);
                //下一步
                seek += length;
            }
            return messagesId;
        }

        /// <summary>
        /// 将指令发送到分发管理器送指令处理器
        /// </summary>
        /// <param name="data">指令数据</param>
        private int parseInstruction(byte[] data)
        {
            //第一个字节为命令id
            var actionId = Convert.ToInt32(data[0]);
            //接下来四个字节为随机消息码
            var messageId = BitConverter.ToInt32(data.Skip(1).Take(4).ToArray(), 0);
            //生成随机消息码
            //var messageId = new Random(actionId + Convert.ToInt32(DateTime.Now.Ticks)).Next();
            //剩下为命令内容
            var content = data.Skip(5).ToArray();
            var message = new RemoteActionMessage()
            {
                ActionId = actionId,
                MessageId = messageId,
                Content = content
            };
            _distMan.MessageQueue.Enqueue(message);
            return messageId;
        }

        private byte[] generateReturnData(ActionExecutionResult result, int actionId, int messageId)
        {
            var datas = new List<byte>();
            datas.AddRange(Encoding.UTF8.GetBytes("SSSR"));
            //添加一个字节的actionId、四个字节的messageId、一个字节的确认码和剩余内容
            var actionContentPlain = new List<byte>();
            actionContentPlain.Add(BitConverter.GetBytes(actionId)[0]);
            actionContentPlain.AddRange(BitConverter.GetBytes(messageId));
            actionContentPlain.Add(Convert.ToByte(result.State));
            actionContentPlain.AddRange(result.Message);
            //加密数据，提取长度
            var chiperDatas = _auth.Encrpyt(actionContentPlain.ToArray());
            //添加两个字节的长度
            datas.AddRange(BitConverter.GetBytes(chiperDatas.Length).Take(2));
            //添加加密内容
            datas.AddRange(chiperDatas);
            return datas.ToArray();
        }

        public SocketRemoteServer(string Host, int Port, byte[] SecretKey)
        {
            _addressSocket = new IPEndPoint(IPAddress.Parse(Host), Port);
            _socket = new Socket(_addressSocket.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _auth = new SRAuthentication(SecretKey.Take(32).ToArray(), SecretKey.Skip(32).Take(16).ToArray());
            _distMan = new DistributionManager(RemoteActionManager.GetAllRemoteActionInstances());
            _distMan.RemoteActionReturn += _distMan_RemoteActionReturn;
            _returnMessages = new List<RemoteActionReturnEventArgs>();
        }

        private void _distMan_RemoteActionReturn(object sender, RemoteActionReturnEventArgs e)
        {
            this._returnMessages.Add(e);
        }

        public void StartListenning()
        {
            _bgcts = new CancellationTokenSource();
            _distMan.Start();
            Task.Run(() => backgroundListenning(), _bgcts.Token);
        }

        public void EndListenning()
        {
            _bgcts.Cancel();
            _distMan.Stop();
        }

    }
}
