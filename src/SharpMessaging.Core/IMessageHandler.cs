using System.Threading.Tasks;

namespace SharpMessaging.Core
{
    public interface IMessageHandler<in T>
    {
        Task Handle(MessageHandlerContext context, T message);
    }
}