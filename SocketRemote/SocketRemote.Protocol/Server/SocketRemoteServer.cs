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
                packetRec.AddRange(byteBuffer);
            } while (byteRec > 0);
            var messageIds = parsePacket(packetRec.ToArray(), socket);
            //等待包返回
            while (messageIds.Count > 0)
            {
                var message = _returnMessages.FirstOrDefault(m => messageIds.Contains(m.MessageId));
                var sendingData = generateReturnData(message.Result, message.RemoteActionId);
                socket.Send(sendingData);
            }
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
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
                seek = stringData.Skip(seek).ToArray().ToString().IndexOf("SSSR");
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
                if (!(seek + length < datas.Count())) break;
                var packetData = datas.Skip(seek).Take(length).ToArray();
                //解密数据，交给命令处理器处理命令
                var decryptedText = _auth.Decrpyt(packetData);
                var messageId = parseInstruction(decryptedText);
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
            //生成随机消息码
            var messageId = new Random(actionId + Convert.ToInt32(DateTime.Now.Ticks)).Next();
            //剩下为命令内容
            var content = data.Skip(1).ToArray();
            var message = new RemoteActionMessage()
            {
                ActionId = actionId,
                MessageId = messageId,
                Content = content
            };
            _distMan.MessageQueue.Enqueue(message);
            return messageId;
        }

        private byte[] generateReturnData(ActionExecutionResult result, int actionId)
        {
            var datas = new List<byte>();
            datas.AddRange(Encoding.UTF8.GetBytes("SSSR"));
            //添加一个字节的actionId、一个字节的确认码和剩余内容
            var actionContentPlain = new List<byte>();
            actionContentPlain.Add(BitConverter.GetBytes(actionId)[0]);
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
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _auth = new SRAuthentication(SecretKey.Take(16).ToArray(), SecretKey.Skip(16).Take(16).ToArray());
            _distMan = new DistributionManager(null);
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
            Task.Run(() => backgroundListenning(), _bgcts.Token);
        }

        public void EndListenning()
        {
            _bgcts.Cancel();
        }

    }
}
