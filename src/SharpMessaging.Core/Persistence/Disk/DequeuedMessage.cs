using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharpMessaging.Core.Persistence.Disk
{
    public class DequeuedMessage
    {
        private readonly Func<Task> _completionTask;
        private readonly LinkedList<Func<Task>> _abortTasks = new LinkedList<Func<Task>>();

        public DequeuedMessage(object message, Func<Task> completionTask, Func<Task> abortTask)
        {
            if (abortTask == null) throw new ArgumentNullException(nameof(abortTask));
            _completionTask = completionTask ?? throw new ArgumentNullException(nameof(completionTask));
            _abortTasks.AddLast(abortTask);
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public object Message { get; }

        public async Task Complete()
        {
            await _completionTask();
        }

        public async Task Abort()
        {
            foreach (var abortTask in _abortTasks)
            {
                try
                {
                    await abortTask();
                }
                catch
                {
                    //TODO: transport the exception using some kind of notification
                    //without breaking the entire system down.
                }
            }
        }

        internal void EnlistAbort(Func<Task> action)
        {
            _abortTasks.AddLast(action);
        }
    }
}