using SocketRemote.Protocol.RemoteActions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
namespace SocketRemote.Protocol.Server
{
    public class DistributionManager
    {
        private IList<IRemoteAction> _remoteActions;
        private CancellationTokenSource _cts;
        private void _backgroundProcess()
        {
            while(true)
            {
                if (MessageQueue.Count == 0) continue;
                var message = MessageQueue.Dequeue();
                var action = _remoteActions.FirstOrDefault(r => r.ActionId == message.ActionId);
                if (action != null)
                    Task.Run(() =>
                    {
                        var result = action.Execute(message.Content);
                        this.RemoteActionReturn?.Invoke(this, new RemoteActionReturnEventArgs() { Result = result, MessageId = message.MessageId, RemoteActionId = message.ActionId });
                    });
            }
        }

        public DistributionManager(IList<IRemoteAction> actions)
        {
            _remoteActions = actions;
            MessageQueue = new Queue<RemoteActionMessage>();
        }

        public Queue<RemoteActionMessage> MessageQueue { get; set; }
        public event EventHandler<RemoteActionReturnEventArgs> RemoteActionReturn;
        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => _backgroundProcess(), _cts.Token);
        }

        public void Stop()
        {
            _cts?.Cancel();
        }
    }
}
