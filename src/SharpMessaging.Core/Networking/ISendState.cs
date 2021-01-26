using System.Threading.Tasks;

namespace SharpMessaging.Core.Networking
{
    /// <summary>
    /// Used to send messages over IPC.
    /// </summary>
    public interface ISendState
    {
        Task Send(byte[] buffer, int offset, int length);
    }
}