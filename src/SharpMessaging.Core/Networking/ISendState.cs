using System.Threading.Tasks;

namespace SharpMessaging.Core.Networking
{
    public interface ISendState
    {
        Task Send(byte[] buffer, int offset, int length);
    }
}