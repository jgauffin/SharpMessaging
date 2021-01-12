using System.Threading.Tasks;

namespace SharpMessaging.Core.Networking
{
    public interface IMessageHandlerInvoker
    {
        Task HandleAsync(object message);
    }
}