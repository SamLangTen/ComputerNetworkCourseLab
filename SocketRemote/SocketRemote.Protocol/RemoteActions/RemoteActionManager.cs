using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Linq;
namespace SocketRemote.Protocol.RemoteActions
{
    public class RemoteActionManager
    {
        public static IList<Type> GetAllRemoteActions()
        {
            var types = new List<Type>();
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IRemoteAction)))).ToList().ForEach(t=>types.AddRange(t));
            return types;
        }

        public static IList<IRemoteAction> GetAllRemoteActionInstances()
        {
            return GetAllRemoteActions().Select(t => (IRemoteAction)Activator.CreateInstance(t)).ToList();
        }
    }
}
