﻿using SocketRemote.Protocol.Authentication;
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

        private IList<ActionExecutionResult> receiveReturnDatas(Socket socket, IList<int> MessageIds)
        {
            //接收返回的数据
            var byteBuffer = new byte[1024];
            var packetRec = new List<Byte>();
            int byteRec;
            do
            {
                byteRec = socket.Receive(byteBuffer, byteBuffer.Length, SocketFlags.None);
                packetRec.AddRange(byteBuffer);
            } while (byteRec > 0 && socket.Connected);
            //处理包
            return parseReturnDatas(packetRec.ToArray(), MessageIds);
        }

        private IList<ActionExecutionResult> parseReturnDatas(byte[] datas, IList<int> MessageIds)
        {
            int seek = 0;
            var stringData = Encoding.UTF8.GetChars(datas);
            var rtnList = new List<ActionExecutionResult>();
            while (seek < datas.Length)
            {
                seek = stringData.Skip(seek).ToArray().ToString().IndexOf("SSSR");
                if (seek == -1) return rtnList;
                //跳过四个字节的SSSR头
                seek += 4;
                //提取两个字节的包长度
                var length = Convert.ToInt32(BitConverter.ToInt16(datas.Skip(seek).Take(2).ToArray(), 0));
                //提取加密的数据并解密
                seek += 2;
                var plainBytes = _auth.Decrpyt(datas.Skip(seek).Take(length).ToArray());
                //提取1字节actionId、4字节messageId、1个字节的确认码和剩余内容
                var actionId = Convert.ToInt32(plainBytes.Take(1));
                var messageId = BitConverter.ToInt32(plainBytes.Skip(1).Take(4).ToArray(),0);
                var state = (ActionExecutionState)Convert.ToInt32(plainBytes.Skip(5).Take(1).ToArray());
                var content = plainBytes.Skip(6).ToArray();
                //组装返回类
                var result = new ActionExecutionResult() {
                     Message = content,
                     State = state
                };
                rtnList.Add(result);
                MessageIds.Remove(messageId);
            }
            return rtnList;
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

        public IList<ActionExecutionResult> SendCommand(IList<IRemoteAction> actions, TimeSpan timeout)
        {
            //先转换为随机消息码的组合
            var commandsObject = actions.Select(a => new
            {
                MessageId = new Random(a.ActionId + Convert.ToInt32(DateTime.Now.Ticks)).Next(),
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
            var cts = new CancellationTokenSource();
            IList<ActionExecutionResult> results = new List<ActionExecutionResult>();
            Task.Run(() =>
                results = receiveReturnDatas(_socket, ids.ToArray()), cts.Token
            );
            var startTime = DateTime.Now;
            //等待命令返回
            while (ids.Count() > 0 && (DateTime.Now - startTime < timeout)) ;
            cts.Cancel();
            return results;
        }


    }
}
