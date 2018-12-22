using SocketRemote.Protocol.Authentication;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;
using SocketRemote.Protocol.RemoteActions;
using System.Threading.Tasks;

namespace SocketRemote.Protocol.Client
{
    public class SocketRemoteClient
    {

        private IPEndPoint _addressSocket;
        private SRAuthentication _auth;
        private Socket _socket;

        private IEnumerable<ActionExecutionResult> receiveReturnDatas(Socket socket)
        {
            //接收返回的数据
            var byteBuffer = new byte[1024];
            var packetRec = new List<Byte>();
            int byteRec;
            do
            {
                byteRec = socket.Receive(byteBuffer, byteBuffer.Length, SocketFlags.None);
                packetRec.AddRange(byteBuffer.Take(byteRec));

            } while (byteRec > 0 && socket.Connected && !checkPacket(packetRec.ToArray()));
            //处理包
            return parseReturnDatas(packetRec.ToArray());
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

        private IEnumerable<ActionExecutionResult> parseReturnDatas(byte[] datas)
        {
            int seek = 0;
            var stringData = Encoding.UTF8.GetChars(datas);
            var rtnList = new List<ActionExecutionResult>();
            while (seek < datas.Length)
            {
                seek = new string(stringData.Skip(seek).ToArray()).IndexOf("SSSR");
                if (seek == -1) yield break;
                //跳过四个字节的SSSR头
                seek += 4;
                //提取两个字节的包长度
                var length = Convert.ToInt32(BitConverter.ToInt16(datas.Skip(seek).Take(2).ToArray(), 0));
                //提取加密的数据并解密
                seek += 2;
                var plainBytes = _auth.Decrpyt(datas.Skip(seek).Take(length).ToArray());
                //提取1字节actionId、4字节messageId、1个字节的确认码和剩余内容
                var actionId = Convert.ToInt32(plainBytes.Take(1).First());
                var messageId = BitConverter.ToInt32(plainBytes.Skip(1).Take(4).ToArray(), 0);
                var state = (ActionExecutionState)Convert.ToInt32(plainBytes.Skip(5).Take(1).First());
                var content = plainBytes.Skip(6).ToArray();
                //组装返回类
                var result = new ActionExecutionResult()
                {
                    Message = content,
                    State = state,
                    MessageId = messageId
                };
                yield return result;
            }
        }

        public SocketRemoteClient(string host, int port, byte[] SecretKeys)
        {
            var ip = Dns.GetHostEntry(host);
            _addressSocket = new IPEndPoint(ip.AddressList.FirstOrDefault(), port);
            _auth = new SRAuthentication(SecretKeys.Take(16).ToArray(), SecretKeys.Skip(16).Take(16).ToArray());
            _socket = new Socket(_addressSocket.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Connect() => _socket?.Connect(_addressSocket);

        public bool IsConnected { get => _socket.Connected; }

        public void Disconnect() => _socket?.Disconnect(true);

        public IEnumerable<ActionExecutionResult> SendCommand(IList<IRemoteAction> actions, TimeSpan timeout)
        {
            //先转换为随机消息码的组合
            var commandsObject = actions.Select(a => new
            {
                MessageId = new Random(a.ActionId + (int)(DateTime.Now.Ticks)).Next(),
                Action = a
            });
            //提取出待转换的命令，加入1字节的ActionId和4字节的随机消息码
            var commandsByte = commandsObject.Select(a => (new byte[] { BitConverter.GetBytes(a.Action.ActionId)[0] }).Concat(BitConverter.GetBytes(a.MessageId).Concat(Encoding.UTF8.GetBytes(a.Action.GetServerCommand())).ToArray()));
            //将数据加密
            var encryptedByte = commandsByte.Select(b => _auth.Encrpyt(b.ToArray()));
            //将提取数据长度，拼接SSSR报文头
            var header = Encoding.UTF8.GetBytes("SSSR");
            var commandToSend = encryptedByte.Select(b => header.Concat(BitConverter.GetBytes(b.Length).Take(2)).Concat(b).ToArray());
            //发送命令
            commandToSend.ToList().ForEach(c =>
            {
                _socket.Send(c);
            });
            //后台等待命令返回
            var ids = commandsObject.Select(a => a.MessageId);
            return receiveReturnDatas(_socket);
        }


    }
}
